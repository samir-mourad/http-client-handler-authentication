using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpClientHandlerAuthentication.HttpClientHandler
{
    public class AuthenticationHandler : DelegatingHandler
    {
        private const string TOKEN_ENDPOINT = "openid-connect/token";

        private readonly ClientCredentials _clientCredentials;
        private readonly HttpClient _httpClient;
        private readonly AccessTokenCacheManager _accessTokenCacheManager;

        public AuthenticationHandler(AccessTokenCacheManager accessTokenCacheManager, ClientCredentials clientCredentials, HttpClient httpClient)
        {
            _accessTokenCacheManager = accessTokenCacheManager;
            _clientCredentials = clientCredentials;
            _httpClient = httpClient;

            if (_httpClient.BaseAddress == null)
            {
                throw new AuthenticationHandlerException($"{nameof(HttpClient.BaseAddress)} o endereço do host/ApplicationId está incorreto");
            }

            if (_httpClient.BaseAddress?.AbsoluteUri.EndsWith("/") == false)
            {
                _httpClient.BaseAddress = new Uri(_httpClient.BaseAddress.AbsoluteUri + "/");
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await GetToken();

            request.Headers.Authorization = new AuthenticationHeaderValue(token.Scheme, token.AccessToken);

            return await base.SendAsync(request, cancellationToken);
        }

        private async Task<TokenResponse> GetToken()
        {
            var token = _accessTokenCacheManager.GetToken(_clientCredentials.ClientId);

            if (token == null)
            {
                token = await GetNewToken(_clientCredentials);
                _accessTokenCacheManager.AddOrUpdateToken(_clientCredentials.ClientId, token);
            }

            return token;
        }

        private async Task<TokenResponse> GetNewToken(ClientCredentials credentials)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, TOKEN_ENDPOINT);

            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", credentials.ClientId),
                new KeyValuePair<string, string>("client_secret", credentials.ClientSecret)
            });

            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var tokenResponse = await response.DeserializeAsync<TokenResponse>();
                return tokenResponse;
            }

            var errorMessage = await GetErrorMessageAsync(response);
            throw new AuthenticationHandlerException(errorMessage);
        }

        private async Task<string> GetErrorMessageAsync(HttpResponseMessage response)
        {
            var errorMessage = $"Ocorreu um erro ao tentar obter o access token do servidor de identidade {response.RequestMessage.RequestUri}";

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var errorResponse = await response.DeserializeAsync<TokenErrorResponse>();
                errorMessage = $"{errorMessage} Detalhe do erro: {errorResponse.Error}";
            }
            else
            {
                errorMessage = $"{errorMessage} Status code: {(int)response.StatusCode} - {response.StatusCode}";
            }

            return errorMessage;
        }
    }
}
