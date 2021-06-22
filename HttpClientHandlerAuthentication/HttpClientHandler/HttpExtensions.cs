using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace HttpClientHandlerAuthentication.HttpClientHandler
{
    public static class HttpExtensions
    {
        public static async Task<T> DeserializeAsync<T>(this HttpResponseMessage response)
        {
            var serializer = new JsonSerializer();

            var stream = await response.Content.ReadAsStreamAsync();

            using var sr = new StreamReader(stream);
            using var jsonTextReader = new JsonTextReader(sr);

            return serializer.Deserialize<T>(jsonTextReader);
        }
    }
}
