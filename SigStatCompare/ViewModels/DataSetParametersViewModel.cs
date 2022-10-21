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
        name = name.ToLower(),
        signerCount = Math.Clamp(SignerCount, 0, int.MaxValue),
        genuinePairCountPerSigner = Math.Clamp(GenuinePairCountPerSigner, 0, int.MaxValue),
        skilledForgeryCountPerSigner = Math.Clamp(SkilledForgeryCountPerSigner, 0, int.MaxValue),
        randomForgeryCountPerSigner = Math.Clamp(RandomForgeryCountPerSigner, 0, int.MaxValue),
    };
}
