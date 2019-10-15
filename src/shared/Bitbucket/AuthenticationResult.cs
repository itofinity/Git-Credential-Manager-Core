/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Atlassian
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

using System.Diagnostics;
using Microsoft.Git.CredentialManager;

namespace Bitbucket.Authentication
{
    /// <summary>
    /// Defines the properties of interest of the results of an Authentication attempt.
    /// </summary>
    [DebuggerDisplay("{Type}")]
    public struct AuthenticationResult
    {
        public AuthenticationResult(BitbucketAuthenticationResultType type)
        {
            Type = type;
            Token = null;
            RefreshToken = null;
            RemoteUsername = null;
        }

        public AuthenticationResult(BitbucketAuthenticationResultType type, string username)
        {
            Type = type;
            Token = null;
            RefreshToken = null;
            RemoteUsername = username;
        }

        public AuthenticationResult(BitbucketAuthenticationResultType type, GitCredential token)
        {
            Type = type;
            Token = token;
            RefreshToken = null;
            RemoteUsername = null;
        }

        public AuthenticationResult(BitbucketAuthenticationResultType type, GitCredential token, string username)
        {
            Type = type;
            Token = token;
            RefreshToken = null;
            RemoteUsername = username;
        }

        public AuthenticationResult(BitbucketAuthenticationResultType type, GitCredential accessToken,
            GitCredential refreshToken)
        {
            Type = type;
            Token = accessToken;
            RefreshToken = refreshToken;
            RemoteUsername = null;
        }

        public AuthenticationResult(BitbucketAuthenticationResultType type, GitCredential accessToken,
            GitCredential refreshToken, string remoteUsername)
        {
            Type = type;
            Token = accessToken;
            RefreshToken = refreshToken;
            RemoteUsername = remoteUsername;
        }

        public readonly BitbucketAuthenticationResultType Type;
        public GitCredential Token { get; internal set; }
        public GitCredential RefreshToken { get; internal set; }
        public string RemoteUsername { get; internal set; }

        /// <summary>
        /// Flag indicating if the results is a success
        /// </summary>
        public bool IsSuccess
        {
            get { return Type.Equals(BitbucketAuthenticationResultType.Success); }
        }

        public static implicit operator bool(AuthenticationResult result)
        {
            return result.Type == BitbucketAuthenticationResultType.Success;
        }

        public static implicit operator BitbucketAuthenticationResultType(AuthenticationResult result)
        {
            return result.Type;
        }

        public static implicit operator AuthenticationResult(BitbucketAuthenticationResultType type)
        {
            return new AuthenticationResult(type);
        }
    }

    /// <summary>
    /// Defines the types of Authentication results we care about.
    /// </summary>
    public enum BitbucketAuthenticationResultType
    {
        None,
        Success,
        Failure,
        TwoFactor,
    }
}
