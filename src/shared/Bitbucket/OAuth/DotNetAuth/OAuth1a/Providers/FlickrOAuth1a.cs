namespace DotNetAuth.OAuth1a.Providers
{
    /// <summary>
    /// OAuth 1.0a provider for Flicker.
    /// </summary>
    public class FlickrOAuth1a : OAuth1aProviderDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlickrOAuth1a"/> class.
        /// </summary>
        public FlickrOAuth1a()
        {
            RequestTokenEndopointUri = "http://www.flickr.com/services/oauth/request_token";
            AuthorizeEndpointUri = "http://www.flickr.com/services/oauth/authorize";
            AuthenticateEndpointUri = "http://www.flickr.com/services/oauth/authorize";
            AccessTokenEndpointUri = "http://www.flickr.com/services/oauth/access_token";
        }
    }
}
