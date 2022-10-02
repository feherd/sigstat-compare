using SigStat.Common;
using SVC2021;

namespace SigStatCompare.Models;

class DatasetGenerator
{
    private IEnumerable<Signer> signers;

    internal IEnumerable<(int, int)> LoadSignatures(string path)
    {
        var loader = new Svc2021Loader(path, false);

        var signers = new List<Signer>();

        int signerCount = 0;
        int signatureCount = 0;
        foreach (var signer in loader.EnumerateSigners())
        {
            signers.Add(signer);

            signerCount++;
            signatureCount += signer.Signatures.Count;

            yield return (signerCount, signatureCount);
        }

        this.signers = signers;
    }

    private static void Update(ref (int min, int max) signatureCount, int count)
    {
        if (count < signatureCount.min) signatureCount.min = count;
        if (count > signatureCount.max) signatureCount.max = count;
    }

    internal Statistics CalculateStatistics(
        HashSet<DB> dbs,
        HashSet<InputDevice> inputDevices,
        HashSet<Split> splits)
    {
        if (signers == null) return new Statistics();

        int signerCount = 0;
        (int min, int max) signatureCount = (int.MaxValue, int.MinValue);
        (int min, int max) genuineSignatureCount = (int.MaxValue, int.MinValue);
        (int min, int max) forgedSignatureCount = (int.MaxValue, int.MinValue);
        foreach (var signer in signers)
        {
            var signatures = signer.Signatures.Where((signature) =>
            {
                var svc2021Signature = signature as SVC2021.Entities.Svc2021Signature;
                return dbs.Contains(svc2021Signature.DB)
                    && inputDevices.Contains(svc2021Signature.InputDevice)
                    && splits.Contains(svc2021Signature.Split);
            });

            int count = signatures.Count();
            int genuine = signatures.Count(signature => signature.Origin == Origin.Genuine);
            int forged = signatures.Count(signature => signature.Origin == Origin.Forged);

            if (count == 0) continue;

            signerCount++;

            Update(ref signatureCount, count);
            Update(ref genuineSignatureCount, genuine);
            Update(ref forgedSignatureCount, forged);
        }

        return new Statistics()
        {
            SignerCount = signerCount,
            SignatureCountPerSigner = signatureCount != (int.MaxValue, int.MinValue) ? signatureCount : (0, 0),
            GenuineSignatureCountPerSigner = genuineSignatureCount != (int.MaxValue, int.MinValue) ? genuineSignatureCount : (0, 0),
            ForgedSignatureCountPerSigner = forgedSignatureCount != (int.MaxValue, int.MinValue) ? forgedSignatureCount : (0, 0)
        };
    }
}
