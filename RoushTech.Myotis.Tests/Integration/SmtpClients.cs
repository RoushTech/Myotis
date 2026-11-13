namespace RoushTech.Myotis.Tests.Integration;

using System.Net.Sockets;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

public static class SmtpClients
{
    public static async Task<SmtpClient> ConnectAsync(MyotisApp app)
    {
        _ = app.Services;
        var client = new SmtpClient();
        // The listener starts in the background with the host, so give it a moment on the first connection.
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await client.ConnectAsync("127.0.0.1", app.SmtpPort, SecureSocketOptions.None);
                return client;
            }
            catch (SocketException) when (attempt < 50)
            {
                await Task.Delay(100);
            }
        }
    }

    public static async Task SendAsync(MyotisApp app, string username, string password, MimeMessage message)
    {
        using var client = await ConnectAsync(app);
        await client.AuthenticateAsync(username, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    public static MimeMessage Message(string subject, string to = "someone@example.test")
    {
        var message = new MimeMessage { Subject = subject };
        message.From.Add(MailboxAddress.Parse("no-reply@billing.example"));
        message.To.Add(MailboxAddress.Parse(to));
        message.Body = new TextPart("plain") { Text = "Hello from the test suite." };
        return message;
    }

    /// <summary>Speaks SMTP by hand so it can ignore the advertised SIZE limit the way a careless sender would.</summary>
    public static async Task<string> SendIgnoringSizeAsync(MyotisApp app, string username, string password, int bodyBytes)
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync("127.0.0.1", app.SmtpPort);
        await using var stream = tcp.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII);
        async Task<string> ReadReplyAsync()
        {
            string? line;
            var reply = new StringBuilder();
            do
            {
                line = await reader.ReadLineAsync() ?? "";
                reply.AppendLine(line);
            }
            while (line.Length > 3 && line[3] == '-');
            return reply.ToString();
        }

        async Task<string> CommandAsync(string command)
        {
            await stream.WriteAsync(Encoding.ASCII.GetBytes(command + "\r\n"));
            return await ReadReplyAsync();
        }

        await ReadReplyAsync();
        await CommandAsync("EHLO test");
        await CommandAsync("AUTH PLAIN " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"\0{username}\0{password}")));
        await CommandAsync("MAIL FROM:<no-reply@billing.example>");
        await CommandAsync("RCPT TO:<someone@example.test>");
        await CommandAsync("DATA");
        var line = Encoding.ASCII.GetBytes(new string('x', 998) + "\r\n");
        try
        {
            await stream.WriteAsync(Encoding.ASCII.GetBytes("Subject: too big\r\n\r\n"));
            for (var sent = 0; sent < bodyBytes; sent += line.Length)
            {
                await stream.WriteAsync(line);
            }

            await stream.WriteAsync(Encoding.ASCII.GetBytes("\r\n.\r\n"));
        }
        catch (IOException)
        {
            // The server hangs up mid-message; its reply is already waiting.
        }

        return await ReadReplyAsync();
    }
}
