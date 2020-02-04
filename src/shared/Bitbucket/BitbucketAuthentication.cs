using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication;
using Bitbucket.Auth;
using Bitbucket.BasicAuth;

namespace Bitbucket
{
    public class BitbucketAuthentication : AuthenticationBase, IBitbucketAuthentication
    {
        private BasicAuthAuthenticator _basicAuthAuthenticator;

        public BitbucketAuthentication(CommandContext context) :base(context) { 
            _basicAuthAuthenticator = new BasicAuthAuthenticator(context);
        }

        public static readonly string[] AuthorityIds =
        {
            "bitbucket",
            "stash",
            "bitbucketserver"
        };

        public async Task<AuthenticationResult> GetCredentialsAsync(Uri targetUri, IEnumerable<string> scopes)
        {
            string userName, password, scheme;

            if (TryFindHelperExecutablePath(out string helperPath))
            {
                IDictionary<string, string> resultDict = await InvokeHelperAsync(helperPath, $"--prompt userpass --host {targetUri}", null);

                if (!resultDict.TryGetValue("username", out userName))
                {
                    Context.Trace.WriteLine("Missing username in response");
                    return new AuthenticationResult(AuthenticationResultType.Failure);
                }

                if (!resultDict.TryGetValue("password", out password))
                {
                    Context.Trace.WriteLine("Missing password in response");
                    return new AuthenticationResult(AuthenticationResultType.Failure);
                }

                if (!resultDict.TryGetValue("scheme", out scheme))
                {
                    Context.Trace.WriteLine("Missing scheme in response");
                    return new AuthenticationResult(AuthenticationResultType.Failure);
                }

                return new AuthenticationResult(AuthenticationResultType.Success, new ExtendedCredential(userName, password, scheme));
            }
            else
            {
                EnsureTerminalPromptsEnabled();

                Context.Terminal.WriteLine("Enter Bitbucket credentials for '{0}'...", targetUri);

                userName = Context.Terminal.Prompt("Username");
                password = Context.Terminal.PromptSecret("Password");
                scheme = Constants.Http.WwwAuthenticateBasicScheme; 
                var credentials = new ExtendedCredential(userName, password, scheme);

                return await _basicAuthAuthenticator.AcquireTokenAsync(
                    targetUri, scopes, 
                    credentials);
  
            }
        }

        public async Task<string> GetAuthenticationCodeAsync(Uri targetUri)
        {
            if (TryFindHelperExecutablePath(out string helperPath))
            {
                IDictionary<string, string> resultDict = await InvokeHelperAsync(helperPath, "--prompt authcode", null);

                if (!resultDict.TryGetValue("accesstoken", out string authCode))
                {
                    throw new Exception("Missing authentication code in response");
                }

                return authCode;
            }

            return null;
        }

        // HACK cut'n'paste from GitGHubAuthentication
        private bool TryFindHelperExecutablePath(out string path)
        {
            string helperName = BitbucketConstants.AuthHelperName;

            if (PlatformUtils.IsWindows())
            {
                helperName += ".exe";
            }

            string executableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (Context.Settings.AuthGuiHelperPaths is string guiHelperPaths) 
            {
                var paths = guiHelperPaths.Split(new char[] {',', ';'}, StringSplitOptions.RemoveEmptyEntries);
                foreach(var possiblePath in paths)
                {
                    path = Path.Combine(executableDirectory, possiblePath, helperName);
                    if (!Context.FileSystem.FileExists(path))
                    {
                        Context.Trace.WriteLine($"Did not find helper '{helperName}' in '{path}'");
                    }
                    else
                    {
                        Context.Trace.WriteLine($"Found helper '{helperName}' in '{path}'");
                        return  true;
                    }
                }
            }

            
            path = Path.Combine(executableDirectory, helperName);
            if (!Context.FileSystem.FileExists(path))
            {
                Context.Trace.WriteLine($"Did not find helper '{helperName}' in '{executableDirectory}'");

                // We currently only have a helper on Windows. If we failed to find the helper we should warn the user.
                if (PlatformUtils.IsWindows())
                {
                    Context.Streams.Error.WriteLine($"warning: missing '{helperName}' from installation.");
                }

                return false;
            }

            return true;
        }
    }
}