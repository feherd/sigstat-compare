using System.Globalization;
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

public class TupleToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var (min, max) = ((int min, int max))value;
        return $"{min}-{max}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // TODO
        return value;
    }
}
