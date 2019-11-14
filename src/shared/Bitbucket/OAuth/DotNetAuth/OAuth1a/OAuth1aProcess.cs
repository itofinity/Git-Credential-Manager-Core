using System;
using System.Threading.Tasks;
using DotNetAuth.Common;
using DotNetAuth.OAuth1a.Framework;

namespace DotNetAuth.OAuth1a
{
    /// <summary>
    /// Provides basic methods to deal with authorization using OAuth1.a protocol.    
    /// </summary>
    /// <remarks>    
    /// <para>
    /// In an OAuth 1.0a authentication process you need to do several actions which this class methods help you in handling
    /// difficulties involved. However this class does not do any extra work that can be done by yourself. So the redirection
    /// of user's browser and also saving temporary tokens is left to you.
    /// <list type="table">
    /// <item><term>Step 1 - Initiating the process</term> <description>To begin an authentication process you should call <see cref="RequestToken"/> method.</description></item>
    /// <item><term>Step 2 - Building the redirection URI</term><description>Then you call <see cref="GetAuthorizationUri(OAuth1aProviderDefinition,ParameterSet)"/> and will receive a URI.</description></item>
    /// <item><term>Step 3 - Redirecting user to authentication endpoint</term><description>You redirect user to the given URI.</description></item>
    /// <item><term>Step 4 - User has to decide on grant or reject</term> <description>User will grant or reject your application.</description></item>
    /// <item><term>Step 5 - Redirecting user to your site from authentication endpoint</term><description>User is redirected to your site again.</description></item>
    /// <item><term>Step 6 - Process user's response, receive access token</term> <description>At end point in which users are redirected to your website after authentication, you should call <see cref="ProcessUserResponse"/> to find out user's response and, if user granted access, to get access token.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <h4>
    /// How to redirect users?
    /// </h4>
    /// To redirect users 
    /// <list type="table">
    /// <item><term>Asp.Net Web Forms</term><description>Use Response.Redirect method.</description></item>
    /// <item><term>Asp.Net MVC</term><description>Use Redirect method in a controller or set the Result to an instance of RedirectResult.</description></item>
    /// <b>Important Note:</b> It is not necessary to redirect user using a HTTP redirection. You could also open a pop up and set the location of that pop up
    /// to the given authentication endpoint. This may be the only solution in some cases, for example if your page is within an IFrame. Because some providers, 
    /// due to security concerns, does not allow their authentication dialog to be shown in an IFrame.
    /// <br/>
    /// Remember to close the pop-up after authentication is completed.
    /// </list>
    /// </para>
    /// <h4>
    /// How to keep track of users when they are back to our website?
    /// </h4>
    /// <para> 
    ///     You may use session or cockies or any other means to find out your site's user identity. However there is also 
    ///     an option provided by OAuth 1.0a which allows you to associate a redirection to your ProcessUserResponse endpoint
    ///     (the URI in which users will be redirected after their granted access to your application) to the actual request 
    ///     originated the authorization process.
    /// </para>
    /// <para>
    ///     In step 1 when you call <see cref="RequestToken"/> you are given a request token(passed by name of 'oauth_token')
    ///     You can save this value (in a temporary manner) and when user is back to your site again this value is passed to 
    ///     you again as request_token. You can match these two tokens to associate 
    ///     the grant response to the initial request.
    /// </para>
    /// <para>
    ///     For OAuth 2.0, there is a arbitrary state argument which makes possible the same objective. You pass an arbitrary 
    ///     state in the initial request and you will receive that state once your application is granted access.
    /// </para>
    /// </remarks>
    public static class OAuth1aProcess
    {
        /// <summary>
        /// As the first step in an OAuth 1.0a authentication process you need to receive a request token.
        /// </summary>
        /// <remarks>
        /// This method will communicate with the OAuth 1.0 provider to get a temporary set of keys.
        /// You need to save these keys temporarily(you can delete them when the authentication process finished).
        /// Later in the process after user is being redirected back to your site you need to provide the secret key
        /// given the request token.
        /// To continue the process you should call <see cref="GetAuthorizationUri(OAuth1aProviderDefinition,ParameterSet)"/> method.
        /// </remarks>
        /// <param name="definition">An instance of object representing an OAuth1.0a service provider. You may find a
        /// predefined implementation in the <see cref="DotNetAuth.OAuth1a.Providers"/> namespace.</param>
        /// <param name="credentials">An object which represent your application keys. To obtains keys you need to
        /// register an application in the OAuth1.0a service provider's website for developers.</param>
        /// <param name="callback">A URI to a end point in your website that user will be redirected to after
        /// authorizing your application. This endpoint is responsible for invoking <see cref="ProcessUserResponse"/>
        /// to exchange request token for access token.</param>
        /// <param name="stateManager">The state manager which will receive the temporary token secret.</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> which when executed will provide a <see cref="ParameterSet"/> containing result parameters which can be passed to <see cref="GetAuthorizationUri(OAuth1aProviderDefinition,ParameterSet)"/> as argument.</returns>
        public static Task<ParameterSet> RequestToken(OAuth1aProviderDefinition definition, ApplicationCredentials credentials, string callback, IOAuth10aStateManager stateManager)
        {
            var authorizationHeaderParameters = definition.GetRequestTokenParameters(credentials, callback);

            // Calculate signature
            var signature = definition.GetSignature(credentials.ConsumerSecret, null, definition.RequestTokenEndopointUri, "POST", authorizationHeaderParameters);

            // Add arguments to list
            authorizationHeaderParameters.Add("oauth_signature", signature);

            // Create authorization header
            var authorizationValue = definition.GetAuthorizationHeader(authorizationHeaderParameters);

            // Make HTTP request
            var http = new RestSharp.Http { Url = new Uri(definition.RequestTokenEndopointUri), };
            http.SetOAuth1aAuthorization(authorizationValue);
            return Task.Factory.StartNew(() => {
                var response = http.Post();
                var result = ParameterSet.FromResponseBody(response.Content);
                #region check oauth_callback_confirmed
                var confirmed = 0 == string.Compare("true", result[Names.RequestTokenResponse.oauth_callback_confirmed], StringComparison.OrdinalIgnoreCase);
                if (!confirmed)
                {
                    throw ErrorProcessing.ProcessRequestTokenNotConfirmedResponse(definition, response.Content, result);
                }
                #endregion
                stateManager.SaveTemporaryTokenSecret(result[Names.RequestTokenResponse.oauth_token], result[Names.RequestTokenResponse.oauth_token_secret]);
                return result;
            });
        }
        /// <summary>
        /// Step 2. After receiving request token you need redirect user(user's browser) to the the URI produced by this method.
        /// You should pass a callback URI. After user authorizes your application they will be redirected to this callback URI.
        /// When handling requests to that callback URI you should use <see cref="ProcessUserResponse"/> to process the user response
        /// and continue the process in there.
        /// </summary>
        /// <param name="definition"></param>
        /// <param name="requestTokenResponse"></param>
        /// <returns>The authorization URI to redirect user to it.</returns>
        public static Uri GetAuthorizationUri(OAuth1aProviderDefinition definition, ParameterSet requestTokenResponse)
        {
            var oauthToken = requestTokenResponse[Names.RequestTokenResponse.oauth_token];
            var result = definition.GetAuthorizationUri(oauthToken);
            return result;
        }
        /// <summary>
        /// A convenient method for calling <see cref="RequestToken"/> and <see cref="GetAuthorizationUri(OAuth1aProviderDefinition,ParameterSet)"/> together.
        /// </summary>
        /// <remarks>
        /// Usually you do not need to call <see cref="RequestToken"/> directly, unless you want to inspect the parameters returned by provider along with token secret.
        /// </remarks>
        /// <param name="definition">An instance of object representing an OAuth1.0a service provider. You may find a
        /// predefined implementation in the <see cref="DotNetAuth.OAuth1a.Providers"/> namespace.</param>
        /// <param name="credentials">An object which represent your application keys. To obtains keys you need to
        /// register an application in the OAuth1.0a service provider's website for developers.</param>
        /// <param name="callback">A URI to an end point in your website that user will be redirected to after
        /// authorizing your application. This endpoint is responsible for invoking <see cref="ProcessUserResponse"/>
        /// to exchange request token for access token.</param>
        /// <param name="stateManager">The state manager which will receive the temporary token secret.</param>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> which when executed provides the authorization URI to redirect user to it.</returns>
        public static Task<Uri> GetAuthorizationUri(OAuth1aProviderDefinition definition, ApplicationCredentials credentials, string callback, IOAuth10aStateManager stateManager)
        {
            return RequestToken(definition, credentials, callback, stateManager)
                .ContinueWith(requestTokenResponse => GetAuthorizationUri(definition, requestTokenResponse.Result));
        }
        /// <summary>
        /// Step 3. This method should be called when handling a request to your website which supposedly is a redirection originated
        /// from OAuth1.a provider when user authorized your application. This method will look at the requestedUri and fetch out its
        /// required data and going through a communication with OAuth1.a provider will produce a valid OAuth token which can be used
        /// to authorize requests to protected resources and actions on behalf of user.
        /// </summary>
        /// <param name="definition">The definition of provider, some default providers are defined in <see cref="DotNetAuth.OAuth1a.Providers"/> namespace.</param>
        /// <param name="credentials">The OAuth user's application credentials.</param>
        /// <param name="requestedUri">The requested URI, the URI of request received by your site which caused by user being redirected back to your website be the OAuth provider.</param>
        /// <param name="stateManager">The state manager which has to give back the token secret. The value which initially given to state manager when calling <see cref="RequestToken"/>.</param>
        /// <returns></returns>
        public static Task<ProcessUserResponseOutput> ProcessUserResponse(OAuth1aProviderDefinition definition, ApplicationCredentials credentials, Uri requestedUri, IOAuth10aStateManager stateManager)
        {
            var oauthToken = requestedUri.GetQueryArgument("oauth_token");
            var oauthVerifier = requestedUri.GetQueryArgument("oauth_verifier");

            var tokenSecret = stateManager.LoadTemporaryTokenSecret(oauthToken);

            var authorizationHeaderParameters = definition.GetGetAccessTokenParameters(credentials, oauthToken, oauthVerifier);

            // Calculate signature
            var signature = definition.GetSignature(credentials.ConsumerSecret, tokenSecret, definition.AccessTokenEndpointUri, "POST", authorizationHeaderParameters);

            // Add arguments to list
            authorizationHeaderParameters.Add(new Parameter("oauth_signature", signature));

            // Create Authorization Header
            var authorizationValue = definition.GetAuthorizationHeader(authorizationHeaderParameters);

            // Make http request
            var http = new RestSharp.Http {Url = new Uri(definition.AccessTokenEndpointUri)};
            //http.RequestContentType = "application/x-www-form-urlencoded";
            http.Headers.Add(new RestSharp.HttpHeader { Name = "Authorization", Value = authorizationValue });

            return Task.Factory.StartNew(() => {
                var response = http.Post();
                var responseParameters = ParameterSet.FromResponseBody(response.Content);
                return new ProcessUserResponseOutput {
                    RequestToken = oauthToken,
                    AllParameters = responseParameters,
                };
            });
        }
    }

    public static class OAuth10aAuthorizedCalls
    {
        public static void ApplyAccessTokenToHeader(this RestSharp.Http http, OAuth1aProviderDefinition definition, ApplicationCredentials credentials, string accessToken, string accessTokenSecret, string method)
        {
            var url = http.Url.GetLeftPart(UriPartial.Path);
            var queryParameters = ParameterSet.FromResponseBody(http.Url.Query.StartsWith("?") ? http.Url.Query.Substring(1) : http.Url.Query);
            var bodyParameters = http.RequestContentType == "application/x-www-form-urlencoded" ? ParameterSet.FromResponseBody(http.RequestBody) : new ParameterSet();
            var authorizationHeaderParameters = definition.GetAuthorizationParameters(credentials, accessToken);
            var signature = definition.GetSignature(credentials.ConsumerSecret, accessTokenSecret, url, method, authorizationHeaderParameters, queryParameters, bodyParameters);
            authorizationHeaderParameters.Add("oauth_signature", signature);

            // Create Authorization Header
            var authorizationValue = definition.GetAuthorizationHeader(authorizationHeaderParameters);
            http.Headers.Add(new RestSharp.HttpHeader { Name = "Authorization", Value = authorizationValue });
        }
    }


}