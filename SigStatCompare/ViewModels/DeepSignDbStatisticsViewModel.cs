namespace SigStatCompare.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class DeepSignDbStatisticsViewModel : ObservableObject
{
    [ObservableProperty]
    private int signerCount;

    [ObservableProperty]
    private (int min, int max) signatureCountPerSigner;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaxGenuinePairCountPerSigner))]
    [NotifyPropertyChangedFor(nameof(MaxForgedPairCountPerSigner))]
    private (int min, int max) genuineSignatureCountPerSigner;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MaxForgedPairCountPerSigner))]
    private (int min, int max) forgedSignatureCountPerSigner;

    public int MaxGenuinePairCountPerSigner
    {
        get
        {
            int min = GenuineSignatureCountPerSigner.min;
            return min * (min - 1) / 2;
        }
    }

    public int MaxForgedPairCountPerSigner
    {
        get
        {
            int genuineMin = GenuineSignatureCountPerSigner.min;
            int forgedMin = ForgedSignatureCountPerSigner.min;
            return genuineMin * forgedMin / 2;
        }
    }
}
