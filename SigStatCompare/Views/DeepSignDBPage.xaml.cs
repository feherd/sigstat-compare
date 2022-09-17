using SigStatCompare.ViewModels;

namespace SigStatCompare.Views;

public partial class DeepSignDBPage : ContentPage
{
    public void LoadDeepSignDB(object sender, EventArgs e)
    {
        var viewModel = BindingContext as DeepSignDBViewModel;
        viewModel.LoadCommand.Execute(null);
    }

    public DeepSignDBPage()
    {
        InitializeComponent();
    }
}
