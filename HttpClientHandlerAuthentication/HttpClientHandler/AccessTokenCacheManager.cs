using System;
using System.Collections.Concurrent;

namespace HttpClientHandlerAuthentication.HttpClientHandler
{
    public class AccessTokenCacheManager
    {
        private readonly ConcurrentDictionary<string, AccessTokenCacheEntry>
            _cache = new ConcurrentDictionary<string, AccessTokenCacheEntry>();

        public void AddOrUpdateToken(string clientId, TokenResponse accessToken)
        {
            var newToken = new AccessTokenCacheEntry(accessToken);

            _cache.TryRemove(clientId, out _);
            _cache.TryAdd(clientId, newToken);
        }

        public TokenResponse GetToken(string clientId)
        {
            _cache.TryGetValue(clientId, out var tokenCacheEntry);

            return tokenCacheEntry?.IsValid ?? false
                 ? tokenCacheEntry.Token
                 : null;
        }

        private class AccessTokenCacheEntry
        {
            public TokenResponse Token { get; }
            public bool IsValid => DateTime.UtcNow < RefreshAfterDate;

            private DateTime RefreshAfterDate { get; }

            public AccessTokenCacheEntry(TokenResponse token)
            {
                Token = token;
                RefreshAfterDate = DateTime.UtcNow + TimeSpan.FromSeconds(token.ExpirationInSeconds / 2.0);
            }
        }
    }
}
