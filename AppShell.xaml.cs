using Encryptor.Views;

namespace Encryptor;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Register routes for navigation
        Routing.RegisterRoute("ViewerPage", typeof(ViewerPage));
    }
}
