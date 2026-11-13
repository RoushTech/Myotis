namespace RoushTech.Myotis.Tests.Integration;

using System.Net.Http.Json;
using RoushTech.Myotis.Website.Models;

public static class HttpClientExtensions
{
    public static async Task<T> GetAsync<T>(this HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    public static async Task<T> PostAsync<T>(this HttpClient client, string path, object body)
    {
        var response = await client.PostAsJsonAsync(path, body);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    public static Task<MailboxResponse> CreateMailboxAsync(this HttpClient admin, string name) =>
        admin.PostAsync<MailboxResponse>("/api/v0/mailboxes", new { name });

    public static Task<CreatedCredentialResponse> CreateCredentialAsync(this HttpClient admin, Guid mailboxId, string username) =>
        admin.PostAsync<CreatedCredentialResponse>($"/api/v0/mailboxes/{mailboxId}/credentials", new { username });
}
