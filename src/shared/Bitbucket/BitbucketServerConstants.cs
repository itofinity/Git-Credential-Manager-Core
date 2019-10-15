namespace Bitbucket
{
    public static class BitbucketServerConstants
    {
        public const string AuthHelperName = "Bitbucket.Authentication.Helper";

        /// <summary>
        /// The Bitbucket required HTTP accepts header value
        /// </summary>
        public const string BitbucketServerApiAcceptsHeaderValue = "application/json";

        public static class TokenScopes
        {
            public const string RepositoryWrite = "REPO_WRITE";
        }
    }
}
