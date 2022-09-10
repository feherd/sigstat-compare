namespace SigStatCompare;

using System;
using System.Globalization;
using SigStatCompare.ViewModels;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        var viewModel = BindingContext as MainViewModel;

        {
            var menuBarItem = MenuBarItems.First(i => i.Text == "File");
            var menuFlyoutSubItem = new MenuFlyoutSubItem() { Text = "Load" };
            foreach (var dataSetLoader in viewModel.DatasetLoaders)
            {
                var menuFlyoutItem = new MenuFlyoutItem();
                menuFlyoutItem.Text = dataSetLoader.Name;
                menuFlyoutItem.CommandParameter = dataSetLoader;
                menuFlyoutItem.Command = viewModel.LoadCommand;
                menuFlyoutSubItem.Add(menuFlyoutItem);
            }
            menuBarItem.Insert(0, menuFlyoutSubItem);
        }

        {
            var menuBarItem = MenuBarItems.First(i => i.Text == "Dtw");
            foreach (var feature in viewModel.DtwFeatures)
            {
                var menuFlyoutItem = new MenuFlyoutItem();
                menuFlyoutItem.Text = feature.Name;
                menuFlyoutItem.CommandParameter = feature;
                menuFlyoutItem.Command = viewModel.SelectDtwFeature;
                menuBarItem.Add(menuFlyoutItem);
            }
        }
    }
}

public class TypeToNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value as Type)?.Name ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // TODO
        return value;
    }
}
