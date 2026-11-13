namespace RoushTech.Myotis.Tests.Integration;

using MimeKit;
using MimeKit.Utils;

public static class Mail
{
    public static readonly byte[] Pdf = [37, 80, 68, 70, 45, 49, 46, 52];

    public static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

    public static MimeMessage Invoice()
    {
        var message = SmtpClients.Message("Your invoice INV-20481 is ready", "jordan@example.org");
        var body = new BodyBuilder { TextBody = "Invoice INV-20481 is attached." };
        var logo = body.LinkedResources.Add("logo.png", Png);
        logo.ContentId = MimeUtils.GenerateMessageId();
        body.HtmlBody = $"""<p>Invoice <b>INV-20481</b></p><img src="cid:{logo.ContentId}"><img src="https://tracker.example/open.gif"><script>alert(1)</script>""";
        body.Attachments.Add("INV-20481.pdf", Pdf);
        message.Body = body.ToMessageBody();
        return message;
    }
}
