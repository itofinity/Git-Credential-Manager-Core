using System.Text.RegularExpressions;

namespace Bitbucket
{
    public static class BitbucketServerConstants
    {
        public const string AuthHelperName = "Bitbucket.Authentication.Helper";

        /// <summary>
        /// The Bitbucket required HTTP accepts header value
        /// </summary>
        public const string ApiAcceptsHeaderValue = "application/json";

        public static class TokenScopes
        {
            public const string RepositoryWrite = "REPO_WRITE";
        }

        public const string PersonalAccessTokenRegexCommand = @"\s*""user"":{.*""name""\s*:\s*""([^""]+)"".*""token""\s*:\s*""([^""]+)""\s*";
        //public const Regex PersonalAccessTokenRegex = new Regex(@"\s*""user"":{.*""name""\s*:\s*""([^""]+)"".*""token""\s*:\s*""([^""]+)""\s*");
    }
}
