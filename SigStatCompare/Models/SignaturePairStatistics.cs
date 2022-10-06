using SigStat.Common;

namespace SigStatCompare.Models;

struct SignaturePairStatistics
{
    public string referenceSignatureFile;
    public Signer referenceSigner;
    public int referenceInput;
    public string questionedSignatureFile;
    public Signer questionedSigner;
    public int questionedInput;
    public int origin;
    public double prediction;
    public double expectedPrediction;
    public double stdevX1;
    public double stdevY1;
    public double stdevP1;
    public int count1;
    public int duration1;
    public double stdevX2;
    public double stdevY2;
    public double stdevP2;
    public int count2;
    public int duration2;
    public double diffDtw;
    public double diffX;
    public double diffY;
    public double diffP;
    public double diffCount;
    public double diffDuration;
}
