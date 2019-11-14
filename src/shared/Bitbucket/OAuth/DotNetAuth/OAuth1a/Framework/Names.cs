namespace DotNetAuth.OAuth1a.Framework
{
    /// <summary>
    /// A mother class for holding constant titles and names and keys.
    /// </summary>
    public class Names
    {
        /// <summary>
        /// Keys available in a response to a request for request token.
        /// </summary>
        public class RequestTokenResponse
        {
            /// <summary>
            /// oauth_token.
            /// </summary>
            public const string oauth_token = "oauth_token";
            /// <summary>
            /// oauth_token_secret.
            /// </summary>
            public const string oauth_token_secret = "oauth_token_secret";
            /// <summary>
            /// oauth_callback_confirmed.
            /// </summary>
            public const string oauth_callback_confirmed = "oauth_callback_confirmed";
        }
        /// <summary>
        /// Keys available in a response to a request for access token.
        /// </summary>
        public class AccessTokenResponse
        {
            /// <summary>
            /// oauth_token.
            /// </summary>
            public const string oauth_token = "oauth_token";
            /// <summary>
            /// oauth_token_secret.
            /// </summary>
            public const string oauth_token_secret = "oauth_token_secret";
            /// <summary>
            /// user_id.
            /// </summary>
            public const string user_id = "user_id";
            /// <summary>
            /// screen_name.
            /// </summary>
            public const string screen_name = "screen_name";
        }
        /// <summary>
        /// Keys should be added to a authorization header.
        /// </summary>
        public class AuthorizationHeader
        {
            /// <summary>
            /// oauth_consumer_key.
            /// </summary>
            public const string oauth_consumer_key = "oauth_consumer_key";
            /// <summary>
            /// oauth_nonce.
            /// </summary>
            public const string oauth_nonce = "oauth_nonce";
            /// <summary>
            /// oauth_signature_method.
            /// </summary>
            public const string oauth_signature_method = "oauth_signature_method";
            /// <summary>
            /// oauth_timestamp.
            /// </summary>
            public const string oauth_timestamp = "oauth_timestamp";
            /// <summary>
            /// oauth_version.
            /// </summary>
            public const string oauth_version = "oauth_version";
        }

    }
}
