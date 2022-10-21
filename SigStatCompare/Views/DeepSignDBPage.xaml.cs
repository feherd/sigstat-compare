using SigStatCompare.Models.Exporters;
using SigStatCompare.ViewModels;

namespace SigStatCompare.Views;

public partial class DeepSignDBPage : ContentPage
{
    public void LoadDeepSignDB(object sender, EventArgs e)
    {
        var viewModel = BindingContext as DeepSignDBViewModel;
        viewModel.LoadCommand.Execute(null);
    }

    private static void Save(DeepSignDBViewModel viewModel, IDataSetExporter exporter)
    {
        var saveCommand = viewModel.SaveCommand;

        if (!saveCommand.CanExecute(exporter)) return;

        saveCommand.Execute(exporter);
    }

    public void SaveToCSV(object sender, EventArgs e)
    {
        var viewModel = BindingContext as DeepSignDBViewModel;
        var csvExporter = viewModel.csvExporter;
        Save(viewModel, csvExporter);
    }

    public void SaveToXLSX(object sender, EventArgs e)
    {
        var viewModel = BindingContext as DeepSignDBViewModel;
        var xlsxExporter = viewModel.xlsxExporter;
        Save(viewModel, xlsxExporter);
    }

    public DeepSignDBPage()
    {
        InitializeComponent();
    }
}
