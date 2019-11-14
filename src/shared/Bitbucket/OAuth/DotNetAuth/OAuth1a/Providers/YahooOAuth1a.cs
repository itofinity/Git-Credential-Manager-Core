namespace DotNetAuth.OAuth1a.Providers
{
    /// <summary>
    /// OAuth 1.0a provider for Yahoo.
    /// </summary>
    public class YahooOAuth1a : OAuth1aProviderDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="YahooOAuth1a"/> class.
        /// </summary>
        public YahooOAuth1a()
        {
            RequestTokenEndopointUri = "https://api.login.yahoo.com/oauth/v2/get_request_token";
            AuthorizeEndpointUri = "https://api.login.yahoo.com/oauth/v2/request_auth";
            AuthenticateEndpointUri = "https://api.login.yahoo.com/oauth/v2/request_auth";
            AccessTokenEndpointUri = "https://api.login.yahoo.com/oauth/v2/get_token";
        }
    }
}
