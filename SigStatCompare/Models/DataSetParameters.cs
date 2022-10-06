namespace SigStatCompare.Models;

struct DataSetParameters
{
    public string name;
    public int signerCount;
    public int genuinePairCountPerSigner;
    public int skilledForgeryCountPerSigner;
    public int randomForgeryCountPerSigner;
}
