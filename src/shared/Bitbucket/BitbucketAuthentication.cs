using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Git.CredentialManager;
using Microsoft.Git.CredentialManager.Authentication;

namespace Bitbucket
{
    public class BitbucketAuthentication : AuthenticationBase, IBitbucketAuthentication
    {
        public BitbucketAuthentication(CommandContext context) :base(context) { }

        public static readonly string[] AuthorityIds =
        {
            "bitbucket",
            "stash",
            "bitbucketserver"
        };

        public async Task<ICredential> GetCredentialsAsync(Uri targetUri)
        {
            string userName, password;

            if (TryFindHelperExecutablePath(out string helperPath))
            {
                IDictionary<string, string> resultDict = await InvokeHelperAsync(helperPath, "--prompt userpass", null);

                if (!resultDict.TryGetValue("username", out userName))
                {
                    throw new Exception("Missing username in response");
                }

                if (!resultDict.TryGetValue("password", out password))
                {
                    throw new Exception("Missing password in response");
                }
            }
            else
            {
                EnsureTerminalPromptsEnabled();

                Context.Terminal.WriteLine("Enter Bitbucket credentials for '{0}'...", targetUri);

                userName = Context.Terminal.Prompt("Username");
                password = Context.Terminal.PromptSecret("Password");
            }

            return new GitCredential(userName, password);
        }

        public async Task<string> GetAuthenticationCodeAsync(Uri targetUri)
        {
            if (TryFindHelperExecutablePath(out string helperPath))
            {
                IDictionary<string, string> resultDict = await InvokeHelperAsync(helperPath, "--prompt authcode", null);

                if (!resultDict.TryGetValue("authcode", out string authCode))
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