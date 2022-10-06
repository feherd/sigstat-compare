using SigStat.Common;

namespace SigStatCompare.Models;

struct SignaturePairStatistics
{
    public Signature referenceSignature;
    public Signature questionedSignature;
    public SignaturePairOrigin origin;
    public double expectedPrediction;
    public SignatureStatistics signatureStatistics1;
    public SignatureStatistics signatureStatistics2;
    public double diffDtw;
    public double diffX;
    public double diffY;
    public double diffP;
    public double diffCount;
    public double diffDuration;
}
