namespace DotNetAuth.OAuth1a.Providers
{
    /// <summary>
    /// OAuth 1.0a provider for Vimeo.
    /// </summary>
    public class VimeoOAuth1a : OAuth1aProviderDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VimeoOAuth1a"/> class.
        /// </summary>
        public VimeoOAuth1a()
        {
            RequestTokenEndopointUri = "https://vimeo.com/oauth/request_token";
            AuthorizeEndpointUri = "https://vimeo.com/oauth/authorize";
            AuthenticateEndpointUri = "https://vimeo.com/oauth/authorize";
            AccessTokenEndpointUri = "https://vimeo.com/oauth/access_token";
        }
    }
}
