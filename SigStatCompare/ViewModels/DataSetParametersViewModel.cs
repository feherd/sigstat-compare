using CommunityToolkit.Mvvm.ComponentModel;
using SigStatCompare.Views;

namespace SigStatCompare.ViewModels;

public partial class DataSetParametersViewModel : ObservableObject
{
    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private int signerCount;

    [ObservableProperty]
    private int genuinePairCountPerSigner;

    [ObservableProperty]
    private int skilledForgeryCountPerSigner;

    [ObservableProperty]
    private int randomForgeryCountPerSigner;
}
