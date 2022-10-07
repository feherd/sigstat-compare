using CommunityToolkit.Mvvm.ComponentModel;
using SigStatCompare.Models;

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

    internal DataSetParameters DataSetParameters => new()
    {
        signerCount = SignerCount,
        genuinePairCountPerSigner = GenuinePairCountPerSigner,
        skilledForgeryCountPerSigner = SkilledForgeryCountPerSigner,
        randomForgeryCountPerSigner = RandomForgeryCountPerSigner,
    };
}
