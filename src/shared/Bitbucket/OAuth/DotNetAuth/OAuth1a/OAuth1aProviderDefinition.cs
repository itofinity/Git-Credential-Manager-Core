using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DotNetAuth.OAuth1a.Framework;

namespace DotNetAuth.OAuth1a
{
    /// <summary>
    /// A definition for an OAuth1.0a provider. Mostly you only need to set the endpoints for request token and authorization and access token.
    /// In case you need to support a provider that has slight different implementation you may customize the implementation of your client
    /// by overriding a type from this type and providing your own specific implementation.
    /// </summary>
    public abstract class OAuth1aProviderDefinition
    {
        #region endpoints
        /// <summary>
        /// request token URI. You need to find the proper value for your service provider by searching through their documentation.
        /// </summary>
        public string RequestTokenEndopointUri { get; set; }
        /// <summary>
        /// user authorization URI. You need to find the proper value for your service provider by searching through their documentation.
        /// </summary>
        public string AuthorizeEndpointUri { get; set; }
        /// <summary>
        /// user authorization URI. You need to find the proper value for your service provider by searching through their documentation.
        /// this value is always the same as <see cref="AuthorizeEndpointUri"/>. However some providers two different endpoints for 
        /// authorization and authentication.
        /// </summary>
        public string AuthenticateEndpointUri { get; set; }
        /// <summary>
        /// access token URI. You need to find the proper value for your service provider by searching through their documentation.
        /// </summary>
        public string AccessTokenEndpointUri { get; set; }
        #endregion
        /// <summary>
        /// Gets a list of parameters to make request to get request token.
        /// </summary>
        /// <remarks>
        /// Override this method to provide a list of parameters according to your provider's specifications.
        /// </remarks>
        /// <param name="credentials">The OAuth user's application credentials.</param>
        /// <param name="callback">A URI to a end point in your website that user will be redirected to after authorizing OAuth user's application.</param>
        /// <returns>The list of parameters to make request token request.</returns>
        public virtual ParameterSet GetRequestTokenParameters(ApplicationCredentials credentials, string callback)
        {
            var result = new ParameterSet(new Dictionary<string, string> { 
                {Names.AuthorizationHeader.oauth_consumer_key,      credentials.ConsumerKey},
                {Names.AuthorizationHeader.oauth_nonce,             GenerateNonce()},
                {Names.AuthorizationHeader.oauth_signature_method,  GetSignatureMethod()},
                {Names.AuthorizationHeader.oauth_timestamp,         GetTimestampValue()},
                {Names.AuthorizationHeader.oauth_version,           "1.0"}
            });
            result.Add("oauth_callback", callback);
            return result;
        }
        /// <summary>
        /// Get a list of parameters to make a request for access token.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Override this method to provide a list of parameters according to your provider's specifications.
        /// </para>
        /// <para>
        /// You should not include <c>oauth_signature</c> as this will be added later by DotNetAuth.
        /// </para>
        /// </remarks>
        /// <param name="credentials">The OAuth user's application credentials.</param>
        /// <param name="oauthToken">The oauth_token parameter.</param>
        /// <param name="oauthVerifier">The oauth_verifier parameter</param>
        /// <returns>The list of parameters to make a request for access token.</returns>
        public virtual ParameterSet GetGetAccessTokenParameters(ApplicationCredentials credentials, string oauthToken, string oauthVerifier)
        {
            var result = new ParameterSet(new Dictionary<string, string> { 
                {Names.AuthorizationHeader.oauth_consumer_key,      credentials.ConsumerKey},
                {Names.AuthorizationHeader.oauth_nonce,             GenerateNonce()},
                {Names.AuthorizationHeader.oauth_signature_method,  GetSignatureMethod()},
                {Names.AuthorizationHeader.oauth_timestamp,         GetTimestampValue()},
                {Names.AuthorizationHeader.oauth_version,           "1.0"}
            });
            result.Add("oauth_token", oauthToken, System.Web.HttpUtility.UrlEncode);
            result.Add("oauth_verifier", oauthVerifier);
            return result;
        }
        /// <summary>
        /// Get a list of parameters to make a authorized request.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Override this method to provide a list of parameters according to your provider's specifications.
        /// </para>
        /// <para>
        /// You should not include <c>oauth_signature</c> as this will be added later by DotNetAuth.
        /// </para>
        /// </remarks>
        /// <param name="credentials">The OAuth user's application credentials.</param>
        /// <param name="oauthToken">The oauth_token parameter.</param>
        /// <returns>The list of parameters to make a authorized request.</returns>
        public virtual ParameterSet GetAuthorizationParameters(ApplicationCredentials credentials, string oauthToken)
        {
            var result = new ParameterSet(new Dictionary<string, string> { 
                {Names.AuthorizationHeader.oauth_consumer_key,      credentials.ConsumerKey},
                {Names.AuthorizationHeader.oauth_nonce,             GenerateNonce()},
                {Names.AuthorizationHeader.oauth_signature_method,  GetSignatureMethod()},
                {Names.AuthorizationHeader.oauth_timestamp,         GetTimestampValue()},
                {Names.AuthorizationHeader.oauth_version,           "1.0"},
                {"oauth_token",                                     oauthToken}                             
            });
            return result;
        }
        /// <summary>
        /// Returns the value for oauth_signature_method.
        /// </summary>
        /// <remarks>
        /// When overridden the <see cref="Sign"/> method should be overridden as well to provide the relevant sign algorithm.
        /// </remarks>
        /// <returns></returns>
        public virtual string GetSignatureMethod()
        {
            return "HMAC-SHA1";
        }
        /// <summary>
        /// Generates a nonce value.
        /// </summary>
        /// <returns></returns>
        public virtual string GenerateNonce()
        {
            return OAuth1aUtil.GenerateNonce();
        }
        /// <summary>
        /// Return a timestamp value expressed in the number of seconds since January 1, 1970 00:00:00 GMT.
        /// </summary>
        /// <returns></returns>
        public virtual string GetTimestampValue()
        {
            // per OAuth1.0a specification we need to pass a timestamp value which is
            // expressed in the number of seconds since January 1, 1970 00:00:00 GMT.
            return ((long)TimestampUtil.GetTimeStampFrom1_1_1970().TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }
        /// <summary>
        /// Appends the oauth_token to <see cref="AuthorizeEndpointUri"/> and returns the URI.
        /// </summary>
        /// <param name="oauthToken">The oauth_token.</param>
        /// <returns>A URI of <see cref="AuthorizeEndpointUri"/> with oauth_token as query string.</returns>
        public virtual Uri GetAuthorizationUri(string oauthToken)
        {
            var uriBuilder = new UriBuilder(AuthorizeEndpointUri);
            uriBuilder.Query = (uriBuilder.Query.Length == 0 ? string.Empty : uriBuilder.Query + "&") + "oauth_token=" + oauthToken;
            return uriBuilder.Uri;
        }
        /// <summary>
        /// Returns a string value for Authorization header based on given parameters.
        /// </summary>
        /// <param name="authorizationHeaderParameters">The parameters to be included in Authorization header.</param>
        /// <returns>The value for Authorization header.</returns>
        public virtual string GetAuthorizationHeader(ParameterSet authorizationHeaderParameters)
        {
            var list = authorizationHeaderParameters.ToList();
            return "OAuth " + string.Join(",", list.Select(i => i.Name + "=\"" + i.EncodedValue + "\"").ToArray());
        }
        /// <summary>
        /// Calculates the signature base string of given parameters.
        /// </summary>
        /// <param name="allParameters">The list of all parameters to be included in signature base string.</param>
        /// <returns>The signature base string.</returns>
        public virtual string CalculateSignatureBaseString(Parameter[] allParameters)
        {
            return OAuth1aUtil.CalculateParameterString(allParameters.Select(i => new KeyValuePair<string, string>(i.Name, i.Value)).ToArray());
        }
        /// <summary>
        /// Gets the key to sign the signature base string which is a combination of consumerSecret and tokenSecret.
        /// </summary>
        /// <param name="consumerSecret">The consumerSecret.</param>
        /// <param name="tokenSecret">The tokenSecret.</param>
        /// <returns>The key for signing.</returns>
        public virtual string GetSigningKey(string consumerSecret, string tokenSecret)
        {
            return OAuth1aUtil.GetSigningKey(consumerSecret, tokenSecret);
        }
        /// <summary>
        /// Signs the given string by the given signing key.
        /// </summary>
        /// <remarks>
        /// Make sure that <see cref="GetSignatureMethod"/> returns the name of signing method used by this method.
        /// </remarks>
        /// <param name="stringToSign">The string to sign.</param>
        /// <param name="signingKey">The key to sign with.</param>
        /// <returns>The signed string.</returns>
        public virtual string Sign(string stringToSign, string signingKey)
        {
            return HMACSHA1.Sign(stringToSign, signingKey);
        }
        /// <summary>
        /// Given a set of parameters and contributing factors calculates a signature base string and then signs it and returns equivalent signature.
        /// </summary>
        /// <param name="consumerSecret">The OAuth user's application consumer secret(part of application credential)</param>
        /// <param name="tokenSecret">The token secret.</param>
        /// <param name="uri">The URI of the target request.</param>
        /// <param name="method">The http method which will be used to make request(POST or GET)</param>
        /// <param name="parameters">A list of all parameters included in request as part of OAuth protocol or user defined.</param>
        /// <returns>A signature generated by the passed in factors.</returns>
        public virtual string GetSignature(string consumerSecret, string tokenSecret, string uri, string method, params ParameterSet[] parameters)
        {
            var allParameters = parameters.SelectMany(p => p.ToList()).ToArray();
            var parametersString = CalculateSignatureBaseString(allParameters);
            var signatureBaseString = OAuth1aUtil.CalcualteSignatureBaseString(method, uri, parametersString);
            var sigingKey = GetSigningKey(consumerSecret, tokenSecret);
            var signature = Sign(signatureBaseString, sigingKey);
            return signature;
        }
    }
}
