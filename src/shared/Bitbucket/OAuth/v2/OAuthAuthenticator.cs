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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using System.Runtime.InteropServices;

namespace Bitbucket.OAuth.v2
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

        private CommandContext _context;
        public OAuthAuthenticator(CommandContext context)
        { 
            this._context = context;
        }

        public string AuthorizeUrlPath { get { return "/site/oauth2/authorize"; } }

        public string CallbackUri { get { return "http://localhost:34106/"; } }

        public string ConsumerKey { get { return "HJdmKXV87DsmC9zSWB"; } }

        public string ConsumerSecret { get { return "wwWw47VB9ZHwMsD4Q4rAveHkbxNrMp3n"; } }

        public string TokenUri { get { return "/site/oauth2/access_token"; } }

        public async Task<AuthenticationResult> AcquireTokenAsync(Uri targetUri, IEnumerable<string> scopes, ICredential credentials)
        {
            return await GetAuthAsync(targetUri, scopes, CancellationToken.None);
        }
        public async Task<AuthenticationResult> Authenticate(string restRootUrl, Uri targetUri, GitCredential credentials, IEnumerable<string> scopes)
        {
            return await GetAuthAsync(targetUri, scopes, CancellationToken.None);
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
            var authToken = await Authorize(targetUri, scopes, cancellationToken);

            return await GetAccessToken(targetUri, authToken);
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
            var authorizationUri = GetAuthorizationUri(scopes);

            // Open the browser to prompt the user to authorize the token request
            OpenBrowser(authorizationUri.AbsoluteUri);

            string rawUrlData;
            try
            {
                // Start a temporary server to handle the callback request and await for the reply.
                rawUrlData = await SimpleServer.WaitForURLAsync(CallbackUri, cancellationToken);
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

            //Parse the callback url
            Dictionary<string, string> qs = GetQueryParameters(rawUrlData);

            // look for a request_token code in the parameters
            string authCode = GetAuthenticationCode(qs);

            if (string.IsNullOrWhiteSpace(authCode))
            {
                var error_desc = GetErrorDescription(qs);
                throw new Exception("Request for an OAuth request_token was denied" + error_desc);
            }

            return authCode;
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
        private async Task<AuthenticationResult> GetAccessToken(Uri targetUri, string authCode)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (authCode is null)
                throw new ArgumentNullException(nameof(authCode));

            var grantUri = GetGrantUri(authCode);
            var content = GetGrantRequestContent(authCode);

            using (var request = new HttpRequestMessage(HttpMethod.Post, grantUri))
            {
                // set content
                request.Content = content;

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

            var refreshUri = GetRefreshUri();

            var content = GetRefreshRequestContent(currentRefreshToken);

            using (var request = new HttpRequestMessage(HttpMethod.Post, refreshUri))
            {
                // set content
                request.Content = content;

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

        private Uri GetAuthorizationUri(IEnumerable<string> scopes)
        {
            const string AuthorizationUrl = "{0}?response_type=code&client_id={1}&state=authenticated&scope={2}&redirect_uri={3}";

            var authorityUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                             AuthorizationUrl,
                                             AuthorizeUrlPath,
                                             ConsumerKey,
                                             string.Join(",", scopes),
                                             CallbackUri);

            return new Uri(new Uri("https://bitbucket.org"), authorityUrl);
        }

        private Uri GetRefreshUri()
        {
            return new Uri(new Uri("https://bitbucket.org"), TokenUri);
        }

        private Uri GetGrantUri(string authCode)
        {
            var tokenUrl = $"{TokenUri}?grant_type=authorization_code&code={authCode}&client_id={ConsumerKey}&client_secret={ConsumerSecret}&state=authenticated";
            return new Uri(new Uri("https://bitbucket.org"), tokenUrl);
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
                { new StringContent(CallbackUri), "redirect_uri" }
            };
            return content;
        }

        private Dictionary<string, string> GetQueryParameters(string rawUrlData)
        {
            return rawUrlData.Split('&')
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
                return new GitCredential("", tokenText);
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

        private static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
