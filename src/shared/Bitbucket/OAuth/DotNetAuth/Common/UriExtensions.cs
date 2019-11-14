using System;
using System.Linq;

namespace DotNetAuth.Common
{
    internal static class UriExtensions
    {
        public static string GetQueryArgument(this Uri uri, string argumentName)
        {
            var query = uri.Query;
            if (query.StartsWith("?"))
                query = query.Substring(1);
            var arguments = query.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            var targetEntry = arguments.FirstOrDefault(a => a.StartsWith(argumentName + "=", StringComparison.OrdinalIgnoreCase));
            if (targetEntry != null) {
                var nameValue = targetEntry.Split(new[] { '=' });
                if (nameValue.Length == 2) {
                    return nameValue[1];
                }
            }
            return null;
        }
    }
}
