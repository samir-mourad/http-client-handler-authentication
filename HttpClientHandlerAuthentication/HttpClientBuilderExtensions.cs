using HttpClientHandlerAuthentication.HttpClientHandler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Net.Http;

namespace HttpClientHandlerAuthentication
{
    public static class HttpClientBuilderExtensions
    {
        public static IHttpClientBuilder AddAuthentication(this IHttpClientBuilder builder, ClientCredentials credentials, string identityAuthority)
        {
            builder.Services.TryAddSingleton<AccessTokenCacheManager>();
            builder.AddHttpMessageHandler(provider => CreateDelegatingHandler(provider, credentials, identityAuthority));

            return builder;
        }

        private static AuthenticationHandler CreateDelegatingHandler(IServiceProvider provider, ClientCredentials credentials, string identityAuthority)
        {
            var httpClient = CreateHttpClient(provider, identityAuthority);
            var accessTokenCacheManager = provider.GetRequiredService<AccessTokenCacheManager>();

            return new AuthenticationHandler(accessTokenCacheManager, credentials, httpClient);
        }

        private static HttpClient CreateHttpClient(IServiceProvider provider, string identityAuthority)
        {
            var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(identityAuthority);

            return httpClient;
        }
    }
}
