using Avalonia;
using Avalonia.Markup.Xaml;

namespace Atlassian_Authentication_Helper_App
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
   }
}