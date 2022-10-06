using SigStat.Common;
using SigStat.Common.Pipeline;
using SigStat.Common.PipelineItems.Transforms.Preprocessing;
using SVC2021;
using SVC2021.Entities;

namespace SigStatCompare.Models;

class DatasetGenerator
{
    public static readonly ITransformation Filter = new FilterPoints
    {
        InputFeatures = new List<FeatureDescriptor<List<double>>> { Features.X, Features.Y, Features.T },
        OutputFeatures = new List<FeatureDescriptor<List<double>>> { Features.X, Features.Y, Features.T },
        KeyFeatureInput = Features.Pressure,
        KeyFeatureOutput = Features.Pressure
    };

    public static readonly ITransformation Filter2 = new FilterPoints2
    {
        InputFeatures = new List<FeatureDescriptor<List<double>>> { Features.X, Features.Y, Features.T },
        OutputFeatures = new List<FeatureDescriptor<List<double>>> { Features.X, Features.Y, Features.T },
        KeyFeatureInput = Features.Pressure,
        KeyFeatureOutput = Features.Pressure
    };

    public static readonly ITransformation Scale1X = new Scale()
    {
        InputFeature = Features.X,
        OutputFeature = Features.X,
        Mode = ScalingMode.Scaling1
    };
    public static readonly ITransformation Scale1Y = new Scale()
    {
        InputFeature = Features.Y,
        OutputFeature = Features.Y,
        Mode = ScalingMode.Scaling1
    };
    public static readonly ITransformation Scale1Pressure = new Scale()
    {
        InputFeature = Svc2021.Pressure,
        OutputFeature = Features.Pressure,
        Mode = ScalingMode.Scaling1
    };

    public static readonly ITransformation SvcScale1X = new SvcScale()
    {
        InputFeature = Features.X,
        OutputFeature = Features.X,
        Mode = ScalingMode.Scaling1
    };
    public static readonly ITransformation SvcScale1Y = new SvcScale()
    {
        InputFeature = Features.Y,
        OutputFeature = Features.Y,
        Mode = ScalingMode.Scaling1
    };
    public static readonly ITransformation SvcScale1Pressure = new SvcScale()
    {
        InputFeature = Features.Pressure,
        OutputFeature = Features.Pressure,
        Mode = ScalingMode.Scaling1
    };

    public static readonly ITransformation ScaleSX = new Scale()
    {
        InputFeature = Features.X,
        OutputFeature = Features.X,
        Mode = ScalingMode.ScalingS
    };
    public static readonly ITransformation ScaleSY = new Scale()
    {
        InputFeature = Features.Y,
        OutputFeature = Features.Y,
        Mode = ScalingMode.ScalingS
    };
    public static readonly ITransformation ScaleSPressure = new Scale()
    {
        InputFeature = Features.Pressure,
        OutputFeature = Features.Pressure,
        Mode = ScalingMode.ScalingS
    };

    public static readonly ITransformation TranslateCogX = new TranslatePreproc(OriginType.CenterOfGravity)
    {
        InputFeature = Features.X,
        OutputFeature = Features.X
    };
    public static readonly ITransformation TranslateCogY = new TranslatePreproc(OriginType.CenterOfGravity)
    {
        InputFeature = Features.Y,
        OutputFeature = Features.Y
    };
    public static readonly ITransformation TranslateCogPressure = new TranslatePreproc(OriginType.CenterOfGravity)
    {
        InputFeature = Features.Pressure,
        OutputFeature = Features.Pressure
    };

    public static readonly ITransformation Rotation1 = new NormalizeRotation()
    {
        InputX = Features.X,
        InputY = Features.Y,
        OutputX = Features.X,
        OutputY = Features.Y,
        InputT = Features.T
    };
    public static readonly ITransformation Rotation2 = new NormalizeRotation2()
    {
        InputX = Features.X,
        InputY = Features.Y,
        OutputX = Features.X,
        OutputY = Features.Y
    };

    private readonly SequentialTransformPipeline sequentialTransformPipeline = new()
    {
        Items = new List<ITransformation>()
        {
            new IntToDoubleConverterTransformation(Svc2021.X, Features.X),
            new IntToDoubleConverterTransformation(Svc2021.Y, Features.Y),
            Scale1X,
            Scale1Y,
            Scale1Pressure
        }
    };

    private IList<Signer> signers;

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
                var svc2021Signature = signature as Svc2021Signature;
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

    IEnumerable<(Signature, Signature)> GenuinePairs(Signer signer, Random random)
    {
        var signatures = signer.Signatures;
        var n = signatures.Count;

        var pairIndices = Enumerable
            .Range(0, n * (n - 1))
            .Select(i => (i / n, i % n))
            .Where(p => p.Item1 < p.Item2)
            .ToList();

        while (pairIndices.Count > 1)
        {
            int index = random.Next(pairIndices.Count);
            (int firstSignatureIndex, int secondSignatureIndex) = pairIndices[index];
            pairIndices.RemoveAt(index);

            yield return (
                signatures[firstSignatureIndex],
                signatures[secondSignatureIndex]
            );
        }
    }

    IList<(Signature, Signature)> GeneratePairs(DataSetParameters dataSetParameters)
    {
        var random = new Random();

        var genuineSignaturePairs = new List<(Signature, Signature)>();

        foreach (var signer in signers.Take(dataSetParameters.signerCount))
        {
            genuineSignaturePairs.AddRange(
                GenuinePairs(signer, random)
                    .Take(dataSetParameters.genuinePairCountPerSigner)
            );
        }


        return genuineSignaturePairs;
    }

    static IList<SignaturePairStatistics> CalculatePairStatistics(IList<(Signature, Signature)> pairs)
    {
        var signaturePairStatisticsList = new List<SignaturePairStatistics>();

        foreach ((Signature signature1, Signature signature2) pair in pairs)
        {
            var statistics = new SignaturePairStatistics()
            {
                referenceSignature = pair.signature1,
                questionedSignature = pair.signature2,
                expectedPrediction = pair.signature1.Origin == Origin.Genuine && pair.signature2.Origin == Origin.Genuine ? 1 : 0
            };

            signaturePairStatisticsList.Add(statistics);
        }

        return signaturePairStatisticsList;
    }

    internal void SaveToCSV(int signerCount)
    {
        IList<(Signature, Signature)> pairs = GeneratePairs(new DataSetParameters()
        {
            signerCount = 3,
            genuinePairCountPerSigner = 10
        });

        // TODO
    }

    internal void SaveToXLSX(int signerCount)
    {
        GeneratePairs(new DataSetParameters()
        {
            signerCount = 3,
            genuinePairCountPerSigner = 10
        });

        // TODO
    }
}
