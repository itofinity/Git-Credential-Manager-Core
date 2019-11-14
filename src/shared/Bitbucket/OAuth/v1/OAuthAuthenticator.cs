/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Atlassian
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using DotNetAuth.OAuth1a;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Git.CredentialManager;
using RestSharp;
using OpenSSL.PrivateKeyDecoder;

namespace Bitbucket.OAuth.v1
{
    /// <summary>
    /// </summary>
    public class OAuthAuthenticator : IOAuthAuthenticator
    {
        /// <summary>
        /// The maximum wait time for a network request before timing out
        /// </summary>
        public const int RequestTimeout = 15 * 1000; // 15 second limit

        internal static readonly Regex RefreshTokenRegex = new Regex(@"\s*""refresh_token""\s*:\s*""([^""]+)""\s*", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal static readonly Regex AccessTokenTokenRegex = new Regex(@"\s*""access_token""\s*:\s*""([^""]+)""\s*", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);


        private BbSOauth1AProvider _bbSOAuth1AProvider;
        private Dictionary<string, string> _oAuthSession = new Dictionary<string, string>();
        private OAuth10aStateManager _oAuth10AStateManager;
        private ApplicationCredentials _oAuth10ACredentials;

        private CommandContext _context;

        public OAuthAuthenticator(CommandContext context, string consumerKey, string consumerSecret)
        {
            _context = context;
            ConsumerKey = consumerKey;
            ConsumerSecret = consumerSecret;
            _oAuth10AStateManager = new OAuth10aStateManager((k, v) => _oAuthSession[k] = v, k => (string)_oAuthSession[k]);
            _oAuth10ACredentials = new ApplicationCredentials
            {
                ConsumerKey = ConsumerKey,
                ConsumerSecret = GetConsumerSecret()
            };
        }

        public string AuthorizeUrl { get { return "plugins/servlet/oauth/authorize"; } }

        public string CallbackUrl { get { return "http://localhost:34106/"; } }

        public string ConsumerKey { get; }

        public string ConsumerSecret { get; }

        public string TokenUrl { get { return "plugins/servlet/oauth/request-token"; } }

        public async Task<AuthenticationResult> AcquireTokenAsync(Uri targetUri, IEnumerable<string> scopes, ICredential credentials)
        {
            var result = await GetAuthAsync(targetUri, scopes, CancellationToken.None);

            if (!result.IsSuccess)
            {
                _context.Trace.WriteLine($"oauth authentication failed");
                return new AuthenticationResult(AuthenticationResultType.Failure);
            }

            // HACK HACk HACK
            return new AuthenticationResult(AuthenticationResultType.Success, result.Token,
                result.RemoteUsername);
        }

        /// <summary>
        /// Gets the OAuth access token
        /// </summary>
        /// <returns>The access token</returns>
        /// <exception cref="SourceTree.Exceptions.OAuthException">
        /// Thrown when OAuth fails for whatever reason
        /// </exception>
        public async Task<AuthenticationResult> GetAuthAsync(Uri targetUri, IEnumerable<string> scopes, CancellationToken cancellationToken)
        {
            var userSlug = await Authorize(targetUri, scopes, cancellationToken);

            return await GetAccessToken(targetUri, userSlug);
        }

        public async Task<AuthenticationResult> Authenticate(string restRootUrl, Uri targetUri, GitCredential credentials, IEnumerable<string> scopes)
        {
            var result = await GetAuthAsync(targetUri, scopes, CancellationToken.None);

            if (!result.IsSuccess)
            {
                _context.Trace.WriteLine($"oauth authentication failed");
                return new AuthenticationResult(AuthenticationResultType.Failure);
            }

            // HACK HACk HACK
            return new AuthenticationResult(AuthenticationResultType.Success, result.Token,
                result.RemoteUsername);
        }

        /// <summary>
        /// Uses a refresh_token to get a new access_token
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="refreshToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> RefreshAuthAsync(Uri targetUri, string refreshToken, CancellationToken cancellationToken)
        {
            return await RefreshAccessToken(targetUri, refreshToken);
        }

        /// <summary>
        /// Run the OAuth dance to get a new request_token
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="scope"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<string> Authorize(Uri targetUri, IEnumerable<string> scopes, CancellationToken cancellationToken)
        {
            var authorizationUri = GetAuthorizationUri(targetUri, scopes);


            // Open the browser to prompt the user to authorize the token request
            Process.Start(authorizationUri.AbsoluteUri);

            string rawUrlData;
            try
            {
                // Start a temporary server to handle the callback request and await for the reply.
                rawUrlData = await SimpleServer.WaitForURLAsync(CallbackUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                string message;
                if (ex.InnerException != null && ex.InnerException.GetType().IsAssignableFrom(typeof(TimeoutException)))
                {
                    message = "Timeout awaiting response from Host service.";
                }
                else
                {
                    message = "Unable to receive callback from OAuth service provider";
                }

                throw new Exception(message, ex);
            }

            try
            {
                var processUserResponse = OAuth1aProcess.ProcessUserResponse(_bbSOAuth1AProvider, _oAuth10ACredentials,
                    new Uri(rawUrlData), _oAuth10AStateManager);
                processUserResponse.Wait();
                _oAuthSession["access_token"] = processUserResponse.Result.AllParameters["oauth_token"];
                _oAuthSession["accessTokenSecret"] = processUserResponse.Result.AllParameters["oauth_token_secret"];
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            // TDOD mke any REST request and then get the X-AUSERNAME header from the response
            // then use that as the usrslug in the token request
            var provider = _bbSOAuth1AProvider;
            var jiraCredentials = _oAuth10ACredentials;
            var accessToken = _oAuthSession["access_token"] as string;
            var accessTokenSecret = _oAuthSession["accessTokenSecret"] as string;


            var http = new Http { Url = new Uri(targetUri.AbsoluteUri + @"/rest/api/1.0/application-properties") };

            http.ApplyAccessTokenToHeader(provider, jiraCredentials, accessToken, accessTokenSecret, "GET");
            var response = http.Get();


            //var processUserResponse2 = OAuth1aProcess.ProcessUserResponse(_bbSOAuth1AProvider, _oAuth10ACredentials,
            //    uri, _oAuth10AStateManager);
            //processUserResponse2.Wait();
            //_oAuthSession["access_token"] = processUserResponse2.Result.AllParameters["oauth_token"];
            //_oAuthSession["accessTokenSecret"] = processUserResponse2.Result.AllParameters["oauth_token_secret"];

            var userslug = response.Headers.FirstOrDefault(h =>
                h.Name.Equals("X-AUSERNAME", StringComparison.InvariantCultureIgnoreCase));

            //var patRequestBody = $"{{\"name\": \"Git-Credential-Manager-{DateTime.Today.ToShortDateString()}\",\"permissions\": [\"REPO_ADMIN\",\"PROJECT_READ\"]}}";
            //var http2 = new Http { Url = new Uri(targetUri.ToString() + $"/rest/access-tokens/1.0/users/{userslug.Value}"), RequestBody = patRequestBody, RequestContentType = "application/json"};

            //http2.ApplyAccessTokenToHeader(provider, jiraCredentials, accessToken, accessTokenSecret, "PUT");
            //var response2 = http2.Put();

            //var json = JObject.Parse(response2.Content);
            //var pat = json["token"];
            ////Parse the callback url
            //Dictionary<string, string> qs = GetQueryParameters(uri.Query);

            //// look for a request_token code in the parameters
            //string authCode = GetAuthenticationCode(qs);

            //if (string.IsNullOrWhiteSpace(authCode))
            //{
            //    var error_desc = GetErrorDescription(qs);
            //    throw new Exception("Request for an OAuth request_token was denied" + error_desc);
            //}

            return userslug.Value;
        }

        private string GetAuthenticationCode(Dictionary<string, string> qs)
        {
            if (qs is null)
                return null;

            return qs.Keys.Where(k => k.EndsWith("code", StringComparison.OrdinalIgnoreCase))
                          .Select(k => qs[k])
                          .FirstOrDefault();
        }

        private string GetErrorDescription(Dictionary<string, string> qs)
        {
            if (qs is null)
                return null;

            return qs["error_description"];
        }

        /// <summary>
        /// Use a request_token to get an access_token
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="authCode"></param>
        /// <returns></returns>
        private async Task<AuthenticationResult> GetAccessToken(Uri targetUri, string userSlug)
        {
            //if (targetUri is null)
            //    throw new ArgumentNullException(nameof(targetUri));
            //if (authCode is null)
            //    throw new ArgumentNullException(nameof(authCode));

            //var options = new NetworkRequestOptions(true)
            //{
            //    Timeout = TimeSpan.FromMilliseconds(RequestTimeout),
            //};
            //var grantUri = GetGrantUrl(targetUri, authCode);
            //var requestUri = targetUri.CreateWith(grantUri);
            //var content = GetGrantRequestContent(authCode);

            //using (var response = await Network.HttpPostAsync(requestUri, content, options))
            //{
            //    Trace.WriteLine($"server responded with {response.StatusCode}.");

            //    switch (response.StatusCode)
            //    {
            //        case HttpStatusCode.OK:
            //        case HttpStatusCode.Created:
            //            {
            //                // The request was successful, look for the tokens in the response.
            //                string responseText = response.Content.AsString;
            //                var token = FindAccessToken(responseText);
            //                var refreshToken = FindRefreshToken(responseText);
            //                return GetAuthenticationResult(token, refreshToken);
            //            }

            //        case HttpStatusCode.Unauthorized:
            //            {
            //                // Do something.
            //                return new AuthenticationResult(AuthenticationResultType.Failure);
            //            }

            //        default:
            //            Trace.WriteLine("authentication failed");
            //            var error = response.Content.AsString;
            //            return new AuthenticationResult(AuthenticationResultType.Failure);
            //    }
            //}

            var accessToken = _oAuthSession["access_token"] as string;
            var accessTokenSecret = _oAuthSession["accessTokenSecret"] as string;

            var patRequestBody = $"{{\"name\": \"Git-Credential-Manager-{DateTime.Today.ToShortDateString()}\",\"permissions\": [\"REPO_ADMIN\",\"PROJECT_READ\"]}}";

            // HACK why is this QueryUri when the previous call was actualuri?
            var http2 = new Http { Url = new Uri(targetUri.AbsoluteUri + $"/rest/access-tokens/1.0/users/{userSlug}"), RequestBody = patRequestBody, RequestContentType = "application/json" };

            http2.ApplyAccessTokenToHeader(_bbSOAuth1AProvider, _oAuth10ACredentials, accessToken, accessTokenSecret, "PUT");
            var response2 = http2.Put();

            var json = JObject.Parse(response2.Content);
            var pat = json["token"];

            // TODO username
            return new AuthenticationResult(AuthenticationResultType.Success, new GitCredential("", pat.Value<string>()), userSlug);
        }

        /// <summary>
        /// Use a refresh_token to get a new access_token
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="currentRefreshToken"></param>
        /// <returns></returns>
        private async Task<AuthenticationResult> RefreshAccessToken(Uri targetUri, string currentRefreshToken)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (currentRefreshToken is null)
                throw new ArgumentNullException(nameof(currentRefreshToken));

            var refreshUri = GetRefreshUri(targetUri);

            var content = GetRefreshRequestContent(currentRefreshToken);

            using (var request = new HttpRequestMessage(HttpMethod.Post, refreshUri))
            {
                // set content
                request.Content = content;

                // Set the auth header
                request.Headers.Authorization = new AuthenticationHeaderValue(Constants.Http.WwwAuthenticateBearerScheme, currentRefreshToken);
                
                request.Headers.Add("Accept", "*/*");
            
                using (var response = await HttpClient.SendAsync(request))
                {
                    _context.Trace.WriteLine($"server responded with {response.StatusCode}.");

                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                        case HttpStatusCode.Created:
                            {
                                // The request was successful, look for the tokens in the response.
                                string responseText = await response.Content.ReadAsStringAsync();
                                var token = FindAccessToken(responseText);
                                var refreshToken = FindRefreshToken(responseText);
                                return GetAuthenticationResult(token, refreshToken);
                            }

                        case HttpStatusCode.Unauthorized:
                            {
                                // Do something.
                                return new AuthenticationResult(AuthenticationResultType.Failure);
                            }

                        default:
                            _context.Trace.WriteLine("authentication failed");
                            var error = await response.Content.ReadAsStringAsync();
                            return new AuthenticationResult(AuthenticationResultType.Failure);
                    }
                }
            }
        }

        private Uri GetAuthorizationUri(Uri targetUri, IEnumerable<string> scopes)
        {

            _bbSOAuth1AProvider = new BbSOauth1AProvider(targetUri.AbsoluteUri);
            var authorizationUri = OAuth1aProcess.GetAuthorizationUri(_bbSOAuth1AProvider, _oAuth10ACredentials,
                CallbackUrl, _oAuth10AStateManager);
            authorizationUri.Wait();
            return authorizationUri.Result;

            /*


            var xxxx = GetAuthorizationUrl("POST", new Uri(targetUri, AuthorizeUrl).AbsoluteUri, null);

            const string AuthorizationUrl = "{0}?response_type=code&client_id={1}&state=authenticated&scope={2}&redirect_uri={3}";

            var authorityUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                             AuthorizationUrl,
                                             AuthorizeUrl,
                                             ConsumerKey,
                                             scope.ToString(),
                                             CallbackUrl);

            return new Uri(targetUri, authorityUrl);
            */
        }

        private string GetConsumerSecret()
        {
            if (File.Exists(ConsumerSecret.Replace(@"\\\\", @"\")))
            {

                string privateKeyText = File.ReadAllText(ConsumerSecret);

                IOpenSSLPrivateKeyDecoder decoder = new OpenSSLPrivateKeyDecoder();
                RSACryptoServiceProvider cryptoServiceProvider = decoder.Decode(privateKeyText);
                var xml = cryptoServiceProvider.ToXmlString(true);

                var privateKey = new RSACryptoServiceProvider();
                privateKey.FromXmlString(xml);

                return xml;
/*
                StreamReader sr = File.OpenText(ConsumerSecret);
                var bbsPrivateKey = sr.ReadToEnd().Trim();
                sr.Close();

                var consumerSecret = bbsPrivateKey
                    .Replace("-----BEGIN PRIVATE KEY-----", "")
                    .Replace("-----BEGIN RSA PRIVATE KEY-----", "")
                    .Replace("-----BEGIN OPENSSH PRIVATE KEY-----", "")
                    .Replace("-----END PRIVATE KEY-----", "")
                    .Replace("-----END RSA PRIVATE KEY-----", "")
                    .Replace("-----END OPENSSH PRIVATE KEY-----", "")
                    .Replace("\r\n", "").Replace("\n", "");

                RSACryptoServiceProvider keyInfo = opensslkey.DecodePrivateKeyInfo(Convert.FromBase64String(consumerSecret));
                return keyInfo.ToXmlString(true);
*/
            }
            else
            {
                return ConsumerSecret;
            }
        }

        private Uri GetRefreshUri(Uri targetUri)
        {
            var baseUrl = targetUri.AbsoluteUri;
            return new Uri(new Uri(baseUrl), TokenUrl);
        }

        private Uri GetGrantUrl(Uri targetUri, string authCode)
        {
            var tokenUrl = $"{TokenUrl}?grant_type=authorization_code&code={authCode}&client_id={ConsumerKey}&client_secret={ConsumerSecret}&state=authenticated";
            return new Uri(new Uri(targetUri.ToString()), tokenUrl);
        }

        private MultipartFormDataContent GetGrantRequestContent(string authCode)
        {
            var content = new MultipartFormDataContent
            {
                { new StringContent("authorization_code"), "grant_type" },
                { new StringContent(authCode), "code" },
                { new StringContent(ConsumerKey), "client_id" },
                { new StringContent(ConsumerSecret), "client_secret" },
                { new StringContent("authenticated"), "state" },
                { new StringContent(CallbackUrl), "redirect_uri" }
            };
            return content;
        }

        private Dictionary<string, string> GetQueryParameters(string rawUrlData)
        {
            return rawUrlData.Replace("/?", string.Empty).Split('&')
                             .ToDictionary(c => c.Split('=')[0],
                                           c => Uri.UnescapeDataString(c.Split('=')[1]));
        }

        private MultipartFormDataContent GetRefreshRequestContent(string currentRefreshToken)
        {
            var content = new MultipartFormDataContent
            {
                { new StringContent("refresh_token"), "grant_type" },
                { new StringContent(currentRefreshToken), "refresh_token" },
                { new StringContent(ConsumerKey), "client_id" },
                { new StringContent(ConsumerSecret), "client_secret" }
            };
            return content;
        }

        private GitCredential FindAccessToken(string responseText)
        {
            Match tokenMatch;
            if ((tokenMatch = AccessTokenTokenRegex.Match(responseText)).Success
                && tokenMatch.Groups.Count > 1)
            {
                string tokenText = tokenMatch.Groups[1].Value;
                // TODO username
                return new GitCredential("",tokenText);
            }

            return null;
        }

        private GitCredential FindRefreshToken(string responseText)
        {
            Match refreshTokenMatch;
            if ((refreshTokenMatch = RefreshTokenRegex.Match(responseText)).Success
                && refreshTokenMatch.Groups.Count > 1)
            {
                string refreshTokenText = refreshTokenMatch.Groups[1].Value;
                // TODO username
                return new GitCredential("", refreshTokenText);
            }

            return null;
        }

        private AuthenticationResult GetAuthenticationResult(GitCredential token, GitCredential refreshToken)
        {
            // Bitbucket should always return both.
            if (token == null || refreshToken == null)
            {
                _context.Trace.WriteLine("authentication failure");
                return new AuthenticationResult(AuthenticationResultType.Failure);
            }
            else
            {
                _context.Trace.WriteLine("authentication success: new personal access token created.");
                return new AuthenticationResult(AuthenticationResultType.Success, token, refreshToken);
            }
        }

        private HttpClient _httpClient;
        private HttpClient HttpClient
        {
            get
            {
                if (_httpClient is null)
                {
                    _httpClient = _context.HttpClientFactory.CreateClient();

                    // Set the common headers and timeout
                    _httpClient.Timeout = TimeSpan.FromMilliseconds(RequestTimeout);
                    _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(BitbucketConstants.BitbucketApiAcceptsHeaderValue));
                }

                return _httpClient;
            }
        }
    }
}
