namespace DotNetAuth.OAuth1a.Providers
{
    /// <summary>
    /// OAuth 1.0a provider for LinkedIn.
    /// </summary>
    public class LinkedInOAuth1a : OAuth1aProviderDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LinkedInOAuth1a"/> class.
        /// </summary>
        public LinkedInOAuth1a()
        {
            RequestTokenEndopointUri = "https://api.linkedin.com/uas/oauth/requestToken";
            AuthenticateEndpointUri = "https://www.linkedin.com/uas/oauth/authenticate";
            AuthorizeEndpointUri = "https://www.linkedin.com/uas/oauth/authenticate";
            AccessTokenEndpointUri = "https://api.linkedin.com/uas/oauth/accessToken";
        }
    }
}
