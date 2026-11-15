namespace RoushTech.Myotis.Messages;

using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using RoushTech.Myotis.Data;
using RoushTech.Myotis.Data.Models;

public record MessageSummary(Guid Id, Guid MailboxId, DateTime ReceivedAt, string Subject, string From, string To, string Cc, int Size, int AttachmentCount, bool IsRead);

public record AttachmentInfo(int Index, string FileName, string ContentType, long Size);

public record MessageDetail(
    MessageSummary Summary,
    string EnvelopeFrom,
    IReadOnlyList<string> EnvelopeTo,
    DateTime? SentAt,
    IReadOnlyList<KeyValuePair<string, string>> Headers,
    string? Text,
    bool HasHtml,
    int RemoteImageCount,
    IReadOnlyList<AttachmentInfo> Attachments,
    string? Html,
    IReadOnlyList<string> Links);

public record AttachmentContent(string FileName, byte[] Content);

public partial class MessageReader(MyotisDbContext db)
{
    private const int ListLimit = 500;

    public Task<List<MessageSummary>> ListAsync(Mailbox mailbox, string? search, CancellationToken cancellationToken) =>
        ListAsync(mailbox, search, since: null, cancellationToken);

    public Task<List<MessageSummary>> ListAsync(Mailbox mailbox, string? search, DateTime? since, CancellationToken cancellationToken)
    {
        var messages = db.Messages.Where(m => m.MailboxId == mailbox.Id);
        if (since is { } after)
        {
            var utc = after.ToUniversalTime();
            messages = messages.Where(m => m.ReceivedAt > utc);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            messages = messages.Where(m =>
                EF.Functions.Like(m.Subject, pattern, "\\") || EF.Functions.Like(m.From, pattern, "\\") || EF.Functions.Like(m.To, pattern, "\\"));
        }

        return messages.OrderByDescending(m => m.ReceivedAt).Take(ListLimit).Select(Summary).ToListAsync(cancellationToken);
    }

    public async Task<MessageDetail> DetailAsync(Message message, CancellationToken cancellationToken)
    {
        var parsed = await ParseAsync(message, cancellationToken);
        var html = parsed.HtmlBody;
        return new MessageDetail(
            ToSummary(message),
            message.EnvelopeFrom,
            message.EnvelopeTo,
            message.SentAt,
            [.. parsed.Headers.Select(h => new KeyValuePair<string, string>(h.Field, h.Value))],
            parsed.TextBody,
            html is not null,
            html is null ? 0 : RemoteImage().Matches(html).Count,
            [.. Attachments(parsed).Select((part, index) => new AttachmentInfo(index, FileName(part, index), part.ContentType.MimeType, Measure(part)))],
            html,
            Links(html, parsed.TextBody));
    }

    // Inline cid: images become data: URIs, because the sandboxed frame can't fetch anything else from Myotis.
    public async Task<string?> HtmlAsync(Message message, CancellationToken cancellationToken)
    {
        var parsed = await ParseAsync(message, cancellationToken);
        var html = parsed.HtmlBody;
        if (html is null)
        {
            return null;
        }

        var inline = parsed.BodyParts.OfType<MimePart>().Where(p => p.ContentId is not null).ToDictionary(p => p.ContentId!, StringComparer.OrdinalIgnoreCase);
        return OpenLinksInNewWindow(ContentIdReference().Replace(html, match =>
        {
            if (!inline.TryGetValue(match.Groups["id"].Value, out var part))
            {
                return match.Value;
            }

            using var stream = new MemoryStream();
            part.Content?.DecodeTo(stream);
            return $"data:{part.ContentType.MimeType};base64,{Convert.ToBase64String(stream.ToArray())}";
        }));
    }

    // A link that loads in the scriptless frame, or tries to replace the page around it, leaves the reader stuck.
    private static string OpenLinksInNewWindow(string html)
    {
        var untargeted = LinkTag().Replace(html, tag => TargetOrQuotedValue().Replace(tag.Value, part => part.Groups["target"].Success ? "" : part.Value));
        // Anything ahead of the doctype drops the message into quirks mode.
        var afterPrologue = DocumentPrologue().Match(untargeted).Length;
        return untargeted.Insert(afterPrologue, """<base target="_blank">""");
    }

    public async Task<byte[]> RawAsync(Message message, CancellationToken cancellationToken) =>
        await db.MessageContents.Where(c => c.MessageId == message.Id).Select(c => c.Raw).SingleAsync(cancellationToken);

    public async Task<AttachmentContent?> AttachmentAsync(Message message, int index, CancellationToken cancellationToken)
    {
        var part = Attachments(await ParseAsync(message, cancellationToken)).ElementAtOrDefault(index);
        if (part is null)
        {
            return null;
        }

        using var stream = new MemoryStream();
        part.Content?.DecodeTo(stream);
        return new AttachmentContent(FileName(part, index), stream.ToArray());
    }

    public async Task SetReadAsync(Message message, bool isRead, CancellationToken cancellationToken)
    {
        message.IsRead = isRead;
        await db.SaveChangesAsync(cancellationToken);
    }

    public static MessageSummary ToSummary(Message m) =>
        new(m.Id, m.MailboxId, m.ReceivedAt, m.Subject, m.From, m.To, m.Cc, m.Size, m.AttachmentCount, m.IsRead);

    private static readonly System.Linq.Expressions.Expression<Func<Message, MessageSummary>> Summary =
        m => new MessageSummary(m.Id, m.MailboxId, m.ReceivedAt, m.Subject, m.From, m.To, m.Cc, m.Size, m.AttachmentCount, m.IsRead);

    // Agents follow the reset or invite link an app just sent, so every link in the HTML and text comes back in order.
    private static List<string> Links(string? html, string? text)
    {
        var fromHtml = html is null ? [] : HtmlLink().Matches(html).Select(m => System.Net.WebUtility.HtmlDecode(m.Groups["url"].Value));
        var fromText = text is null ? [] : TextLink().Matches(text).Select(m => m.Value.TrimEnd('.', ',', ')', '>'));
        return [.. fromHtml.Concat(fromText).Distinct()];
    }

    private async Task<MimeMessage> ParseAsync(Message message, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(await RawAsync(message, cancellationToken), writable: false);
        return await MimeMessage.LoadAsync(stream, cancellationToken);
    }

    private static List<MimePart> Attachments(MimeMessage message) => [.. message.Attachments.OfType<MimePart>()];

    private static string FileName(MimePart part, int index) => part.FileName ?? $"attachment-{index + 1}";

    private static long Measure(MimePart part)
    {
        using var stream = new MemoryStream();
        part.Content?.DecodeTo(stream);
        return stream.Length;
    }

    [GeneratedRegex("""<img\b[^>]*\bsrc\s*=\s*["']?\s*(https?:)?//""", RegexOptions.IgnoreCase)]
    private static partial Regex RemoteImage();

    [GeneratedRegex("""<a\b[^>]*\bhref\s*=\s*["'](?<url>https?://[^"']+)["']""", RegexOptions.IgnoreCase)]
    private static partial Regex HtmlLink();

    [GeneratedRegex(@"https?://[^\s<>""']+", RegexOptions.IgnoreCase)]
    private static partial Regex TextLink();

    [GeneratedRegex("""cid:(?<id>[^"'\s)>]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex ContentIdReference();

    [GeneratedRegex("""<(?:a|area)\b(?:[^>"']|"[^"]*"|'[^']*')*>""", RegexOptions.IgnoreCase)]
    private static partial Regex LinkTag();

    [GeneratedRegex("""(?<target>\s+target\s*=\s*(?:"[^"]*"|'[^']*'|[^\s>]+))|"[^"]*"|'[^']*'""", RegexOptions.IgnoreCase)]
    private static partial Regex TargetOrQuotedValue();

    [GeneratedRegex("""^(?:\s|<!--.*?-->|<\?[^>]*>|<!doctype[^>]*>)*""", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex DocumentPrologue();
}
