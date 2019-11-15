using Avalonia.Controls;

namespace Atlassian_Authentication_Helper_App
{
    public abstract class AbstractAuthWindow : Window, IAuthWindow
    {
        public abstract bool Success { get; }

        public abstract string Response { get; }
    }
}