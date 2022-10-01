namespace SigStatCompare.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class StatisticsViewModel : ObservableObject
{
    [ObservableProperty]
    private int signerCount;

    [ObservableProperty]
    private (int min, int max) signatureCountPerSigner;

    [ObservableProperty]
    private (int min, int max) genuineSignatureCountPerSigner;

    [ObservableProperty]
    private (int min, int max) forgedSignatureCountPerSigner;

    [ObservableProperty]
    private int maxGenuinePairCountPerSigner;

    [ObservableProperty]
    private int maxForgedPairCountPerSigner;

    public void SetStatistics(Models.Statistics statistics)
    {
        SignerCount = statistics.SignerCount;
        SignatureCountPerSigner = statistics.SignatureCountPerSigner;
        GenuineSignatureCountPerSigner = statistics.GenuineSignatureCountPerSigner;
        ForgedSignatureCountPerSigner = statistics.ForgedSignatureCountPerSigner;
        MaxGenuinePairCountPerSigner = statistics.MaxGenuinePairCountPerSigner;
        MaxForgedPairCountPerSigner = statistics.MaxForgedPairCountPerSigner;
    }
}
