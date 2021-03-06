﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using PingPong.Core;

namespace PingPong.OAuth
{
    /// <summary>OAuth authorization client.</summary>
    public class OAuthAuthorizer : OAuthBase
    {
        public OAuthAuthorizer(string consumerKey, string consumerSecret)
            : base(consumerKey, consumerSecret)
        {
        }

        private IObservable<TokenResponse<T>> GetTokenResponse<T>(string url, IEnumerable<KeyValuePair<string, object>> parameters, Func<string, string, T> tokenFactory) where T : Token
        {
            var req = WebRequest.CreateHttp(url);
            req.Headers[HttpRequestHeader.Authorization] = BuildAuthorizationHeader(parameters);
            req.Method = MethodType.Post.ToString().ToUpper();
            req.ContentType = "application/x-www-form-urlencoded";

            return req.GetResponseAsObservable()
                .GetResponseLines()
                .Select(tokenBase =>
                {
                    var splitted = tokenBase.Split('&').Select(s => s.Split('=')).ToDictionary(s => s.First(), s => s.Last());
                    var token = tokenFactory(splitted["oauth_token"], splitted["oauth_token_secret"]);
                    var extraData = splitted.Where(kvp => kvp.Key != "oauth_token" && kvp.Key != "oauth_token_secret")
                        .ToLookup(kvp => kvp.Key, kvp => kvp.Value);
                    return new TokenResponse<T>(token, extraData);
                });
        }

        public string BuildAuthorizeUrl(string authUrl, RequestToken requestToken)
        {
            Enforce.NotNull(authUrl, "authUrl");
            Enforce.NotNull(requestToken, "accessToken");

            return authUrl + "?oauth_token=" + requestToken.Key;
        }

        /// <summary>Asynchronously gets request tokens.</summary>
        /// <param name="otherParameters">need parameters except consumer_key,timestamp,nonce,signature,signature_method,version</param>
        public IObservable<TokenResponse<RequestToken>> GetRequestToken(string requestTokenUrl, params KeyValuePair<string, object>[] otherParameters)
        {
            Enforce.NotNull(requestTokenUrl, "requestTokenUrl");
            Enforce.NotNull(otherParameters, "otherParameters");

            var parameters = ConstructBasicParameters(requestTokenUrl, MethodType.Post, null, otherParameters);
            foreach (var p in otherParameters)
                parameters.Add(p);

            return GetTokenResponse(requestTokenUrl, parameters, (key, secret) => new RequestToken(key, secret));
        }

        /// <summary>Asynchronously gets the access token.</summary>
        public IObservable<TokenResponse<AccessToken>> GetAccessToken(string accessTokenUrl, RequestToken requestToken, string verifier)
        {
            Enforce.NotNull(accessTokenUrl, "accessTokenUrl");
            Enforce.NotNull(requestToken, "requestToken");
            Enforce.NotNull(verifier, "verifier");

            var verifierParam = new KeyValuePair<string, object>("oauth_verifier", verifier);
            var parameters = ConstructBasicParameters(accessTokenUrl, MethodType.Post, requestToken, verifierParam);
            parameters.Add(verifierParam);
            return GetTokenResponse(accessTokenUrl, parameters, (key, secret) => new AccessToken(key, secret));
        }
    }
}