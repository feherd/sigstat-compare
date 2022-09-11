namespace SigStatCompare;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("deepsigndb", typeof(Views.DeepSignDBPage));
    }
}
