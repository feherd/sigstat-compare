using System.Globalization;

namespace SigStatCompare.Views;

public partial class StatisticsView : ContentView
{
	public StatisticsView()
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
