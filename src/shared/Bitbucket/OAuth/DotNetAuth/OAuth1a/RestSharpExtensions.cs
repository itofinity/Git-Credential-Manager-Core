namespace DotNetAuth.OAuth1a
{
    /// <summary>
    /// Adds extension methods for <see cref="RestSharp.Http"/>.
    /// </summary>
    /// <remarks>
    /// This class is a repository for extension methods added to RestSharp.Http type in regard to OAuth 1.0a needs.
    /// </remarks>
    public static class RestSharpExtensions
    {
        /// <summary>
        /// Adds the authorization header value to the current <see cref="RestSharp.Http"/> object.
        /// </summary>
        /// <param name="http">The RestSharp http object which authorization header will be added to.</param>
        /// <param name="authorizationHeaderValue">The value for authorization header.</param>
        public static void SetOAuth1aAuthorization(this RestSharp.Http http, string authorizationHeaderValue)
        {
            http.Headers.Add(new RestSharp.HttpHeader { Name = "Authorization", Value = authorizationHeaderValue });
        }
    }
}
