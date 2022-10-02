using SigStatCompare.ViewModels;

namespace SigStatCompare.Views;

public partial class DeepSignDBPage : ContentPage
{
    public void LoadDeepSignDB(object sender, EventArgs e)
    {
        var viewModel = BindingContext as DeepSignDBViewModel;
        viewModel.LoadCommand.Execute(null);
    }

    public void SaveToCSV(object sender, EventArgs e)
    {
        var viewModel = BindingContext as DeepSignDBViewModel;
        viewModel.SaveToCSV.Execute(null);
    }

    public void SaveToXLSX(object sender, EventArgs e)
    {
        var viewModel = BindingContext as DeepSignDBViewModel;
        viewModel.SaveToXLSX.Execute(null);
    }

    public DeepSignDBPage()
    {
        InitializeComponent();
    }
}
