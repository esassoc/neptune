using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Neptune.Common.Services;

public abstract class BaseAPIService<T>
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger<T> Logger;
    protected readonly string ServiceName;

    protected BaseAPIService(HttpClient httpClient, ILogger<T> logger, string serviceName)
    {
        HttpClient = httpClient;
        Logger = logger;
        ServiceName = serviceName;
    }

    protected async Task<TV> GetJsonImpl<TV>(string uri, JsonSerializerOptions jsonSerializerOptions)
    {
        return await HttpClient.GetFromJsonAsync<TV>(uri, jsonSerializerOptions);
    }

    protected async Task<HttpResponseMessage> PostJsonImpl<TV>(string uri, TV value, JsonSerializerOptions jsonSerializerOptions)
    {
        var postResponse = await HttpClient.PostAsJsonAsync(uri, value, jsonSerializerOptions);
        postResponse.EnsureSuccessStatusCode();
        return postResponse;
    }

    protected async Task<HttpResponseMessage> PostFormContent(string uri, List<KeyValuePair<string, string>> nameValueCollection)
    {
        var postResponse = await HttpClient.PostAsync(uri, new FormUrlEncodedContent(nameValueCollection));
        postResponse.EnsureSuccessStatusCode();
        return postResponse;
    }

    protected async Task<List<TV>> GetIEnumerableImpl<TV>(string uri, JsonSerializerOptions jsonSerializerOptions)
    {
        using var response = await HttpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var list = new List<TV>();
        await using var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

        await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<TV>(responseStream,
                           new JsonSerializerOptions { PropertyNameCaseInsensitive = true, DefaultBufferSize = 512 }))
        {
            list.Add(item);
        }

        return list;
    }


}