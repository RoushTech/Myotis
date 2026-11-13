namespace RoushTech.Myotis.Tests.Integration;

using System.Net.Http.Json;

public static class HttpClientExtensions
{
    public static async Task<T> GetAsync<T>(this HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
