﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Browser;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using PingPong.Core;

namespace PingPong.OAuth
{
    public abstract class OAuthBase
    {
        static OAuthBase()
        {
            if (Application.Current.IsRunningOutOfBrowser)
            {
                WebRequest.RegisterPrefix("http://", WebRequestCreator.ClientHttp);
                WebRequest.RegisterPrefix("https://", WebRequestCreator.ClientHttp);
            }
            else
            {
                WebRequest.RegisterPrefix("http://", WebRequestCreator.BrowserHttp);
                WebRequest.RegisterPrefix("https://", WebRequestCreator.BrowserHttp);
            }
        }

        private static readonly Random _random = new Random();

        public string ConsumerKey { get; private set; }
        public string ConsumerSecret { get; private set; }

        public OAuthBase(string consumerKey, string consumerSecret)
        {
            Enforce.NotNullOrEmpty(consumerKey, "key");
            Enforce.NotNullOrEmpty(consumerSecret, "consumerSecret");

            ConsumerKey = consumerKey;
            ConsumerSecret = consumerSecret;
        }

        private string GenerateSignature(Uri uri, MethodType methodType, Token token, IEnumerable<KeyValuePair<string, object>> parameters)
        {
            var hmacKeyBase = ConsumerSecret.UrlEncode() + "&" + ((token == null) ? "" : token.Secret).UrlEncode();
            using (var hmacsha1 = new HMACSHA1(Encoding.UTF8.GetBytes(hmacKeyBase)))
            {
                var stringParameter = parameters.OrderBy(p => p.Key)
                    .ThenBy(p => p.Value)
                    .ToQueryParameter();
                var signatureBase = methodType.ToString().ToUpper() +
                                    "&" + uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped).UrlEncode() +
                                    "&" + stringParameter.UrlEncode();

                var hash = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(signatureBase));
                return Convert.ToBase64String(hash).UrlEncode();
            }
        }

        protected string BuildAuthorizationHeader(IEnumerable<KeyValuePair<string, object>> parameters)
        {
            Enforce.NotNull(parameters, "parameters");

            return "OAuth " + string.Join(",", parameters.Select(p => string.Format("{0}=\"{1}\"", p.Key, p.Value.ToString())));
        }

        protected IDictionary<string,object> ConstructBasicParameters(string url, MethodType methodType, Token token, params KeyValuePair<string,object>[] optionalParameters)
        {
            Enforce.NotNull(url, "url");
            Enforce.NotNull(optionalParameters, "optionalParameters");

            var parameters = new Dictionary<string, object>
            {
                { "oauth_consumer_key", ConsumerKey },
                { "oauth_nonce", _random.Next() },
                { "oauth_timestamp", DateTime.UtcNow.ToUnixTime() },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_version", "1.0" }
            };
            if (token != null)
                parameters.Add("oauth_token", token.Key);

            var signature = GenerateSignature(new Uri(url), methodType, token, parameters.Concat(optionalParameters));
            parameters.Add("oauth_signature", signature);

            return parameters;
        }
    }

    public enum MethodType
    {
        Post,
        Get,
    }
}