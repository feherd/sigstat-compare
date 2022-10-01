namespace SigStatCompare.Models;

public class Statistics
{

    public int SignerCount { get; set; }

    public (int min, int max) SignatureCountPerSigner { get; set; }

    public (int min, int max) GenuineSignatureCountPerSigner { get; set; }

    public (int min, int max) ForgedSignatureCountPerSigner { get; set; }

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
