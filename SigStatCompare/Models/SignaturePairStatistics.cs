using SigStat.Common;
using SVC2021.Entities;

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

    public IList<object> ToList() => new List<object>(){
            referenceSignature.ID,
            referenceSignature.Signer.ID,
            (referenceSignature as Svc2021Signature).InputDevice,
            questionedSignature.ID,
            questionedSignature.Signer.ID,
            (questionedSignature as Svc2021Signature).InputDevice,
            origin,
            expectedPrediction,
            signatureStatistics1.stdevX,
            signatureStatistics1.stdevY,
            signatureStatistics1.stdevP,
            signatureStatistics1.count,
            signatureStatistics1.duration,
            signatureStatistics2.stdevX,
            signatureStatistics2.stdevY,
            signatureStatistics2.stdevP,
            signatureStatistics2.count,
            signatureStatistics2.duration,
            diffDtw,
            diffX,
            diffY,
            diffP,
            diffCount,
            diffDuration
        };
}
