namespace RoushTech.Myotis.Tests.Integration;

using System.Net.Sockets;
using System.Text;

// Speaks SMTP by hand for what MailKit won't do: send a PROXY header, or keep going after failures.
public sealed class RawSmtp : IAsyncDisposable
{
    private readonly TcpClient tcp = new();
    private NetworkStream? stream;
    private StreamReader? reader;

    public static async Task<RawSmtp> ConnectAsync(MyotisApp app, string? proxySource = null)
    {
        _ = app.Services;
        var smtp = new RawSmtp();
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await smtp.tcp.ConnectAsync("127.0.0.1", app.SmtpPort);
                break;
            }
            catch (SocketException) when (attempt < 50)
            {
                await Task.Delay(100);
            }
        }

        smtp.stream = smtp.tcp.GetStream();
        smtp.reader = new StreamReader(smtp.stream, Encoding.ASCII);
        await smtp.ReadReplyAsync();
        if (proxySource is not null)
        {
            await smtp.WriteAsync($"PROXY TCP4 {proxySource} 10.0.0.2 40000 25");
        }

        await smtp.CommandAsync("EHLO test");
        return smtp;
    }

    public Task<string> SignInAsync(string username, string password) =>
        CommandAsync("AUTH PLAIN " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"\0{username}\0{password}")));

    public async Task<string> CommandAsync(string command)
    {
        await WriteAsync(command);
        return await ReadReplyAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (stream is not null)
        {
            await stream.DisposeAsync();
        }

        tcp.Dispose();
    }

    private async Task WriteAsync(string line) => await stream!.WriteAsync(Encoding.ASCII.GetBytes(line + "\r\n"));

    private async Task<string> ReadReplyAsync()
    {
        var reply = new StringBuilder();
        string line;
        do
        {
            line = await reader!.ReadLineAsync() ?? "";
            reply.AppendLine(line);
        }
        while (line.Length > 3 && line[3] == '-');
        return reply.ToString();
    }
}
