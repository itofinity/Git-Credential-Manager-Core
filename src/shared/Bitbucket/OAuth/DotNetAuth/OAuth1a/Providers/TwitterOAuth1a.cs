namespace DotNetAuth.OAuth1a.Providers
{
    /// <summary>
    /// OAuth 1.0a provider for twitter.
    /// </summary>
    public class TwitterOAuth1a : OAuth1aProviderDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TwitterOAuth1a"/> class.
        /// </summary>
        public TwitterOAuth1a()
        {
            RequestTokenEndopointUri = "https://api.twitter.com/oauth/request_token";
            AuthorizeEndpointUri = "https://api.twitter.com/oauth/authorize";
            AuthenticateEndpointUri = "https://api.twitter.com/oauth/authenticate";
            AccessTokenEndpointUri = "https://api.twitter.com/oauth/access_token";
        }
    }
}
