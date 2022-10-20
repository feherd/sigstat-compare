using SigStat.Common;
using SigStat.Common.Pipeline;
using SigStat.Common.PipelineItems.Transforms.Preprocessing;
using SVC2021;
using SVC2021.Entities;
using SigStatCompare.Models.Transformations;
using SigStat.Common.Algorithms.Distances;
using SigStatCompare.Models.Helpers;
using SigStatCompare.Models.Exporters;

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
    private readonly SequentialTransformPipeline zNormalizationPipeline = new()
    {
        Items = {
            new ZNormalization(){
                InputFeature = Features.X,
                OutputFeature = Features.X
            },
            new ZNormalization(){
                InputFeature = Features.Y,
                OutputFeature = Features.Y
            }
        }
    };

    private readonly DtwDistance dtwDistance = new();

    public ISet<DB> DBs { get; set; } = new HashSet<DB>();
    public ISet<InputDevice> InputDevices { get; set; } = new HashSet<InputDevice>();
    public ISet<Split> Splits { get; set; } = new HashSet<Split>();

    private IList<Signer> signers;

    private Random random;

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

    internal Statistics CalculateStatistics()
    {
        if (signers == null) return new Statistics();

        int signerCount = 0;
        (int min, int max) signatureCount = (int.MaxValue, int.MinValue);
        (int min, int max) genuineSignatureCount = (int.MaxValue, int.MinValue);
        (int min, int max) forgedSignatureCount = (int.MaxValue, int.MinValue);
        foreach (var signer in signers)
        {
            IEnumerable<Signature> signatures = FilterSignatures(signer.Signatures);

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

    private IEnumerable<Signature> FilterSignatures(IEnumerable<Signature> signatures)
    {
        return signatures.Where((signature) =>
        {
            var svc2021Signature = signature as Svc2021Signature;
            return DBs.Contains(svc2021Signature.DB)
                && InputDevices.Contains(svc2021Signature.InputDevice)
                && Splits.Contains(svc2021Signature.Split);
        });
    }

    IEnumerable<(Signature, Signature)> GenuinePairs(Signer signer)
    {
        var signatures = FilterSignatures(signer.Signatures);
        var genuineSignatures = signatures.Where(sig => sig.Origin == Origin.Genuine).ToList();
        var n = genuineSignatures.Count;

        var pairIndices = Enumerable
            .Range(0, n * (n - 1))
            .Select(i => (i / n, i % n))
            .Where(p => p.Item1 < p.Item2)
            .ToList();

        return pairIndices
            .RandomOrder(random)
            .Select(p => (genuineSignatures[p.Item1], genuineSignatures[p.Item2]));
    }

    IEnumerable<(Signature, Signature)> ForgeryPairs(Signer signer)
    {
        var signatures = FilterSignatures(signer.Signatures);
        var genuineSignatures = signatures.Where(sig => sig.Origin == Origin.Genuine).ToList();
        var forgedSignatures = signatures.Where(sig => sig.Origin == Origin.Forged).ToList();
        var n = genuineSignatures.Count;
        var m = forgedSignatures.Count;

        var pairIndices = Enumerable
            .Range(0, n * m)
            .Select(i => (i / m, i % m))
            .ToList();

        return pairIndices
            .RandomOrder(random)
            .Select(p => (genuineSignatures[p.Item1], forgedSignatures[p.Item2]));
    }

    IEnumerable<(Signature, Signature)> RandomPairs(Signer signer)
    {
        var signatures = FilterSignatures(signer.Signatures);
        var genuineSignatures = signatures.Where(sig => sig.Origin == Origin.Genuine).ToList();
        
        var otherSignatures = signers.Where(s => s != signer).SelectMany(s => s.Signatures);
        var randomSignatures = FilterSignatures(otherSignatures).ToList();
        
        var n = genuineSignatures.Count;
        var m = randomSignatures.Count;

        var pairIndices = Enumerable
            .Range(0, n * m)
            .Select(i => (i / m, i % m))
            .ToList();

        return pairIndices
            .RandomOrder(random)
            .Select(p => (genuineSignatures[p.Item1], randomSignatures[p.Item2]));
    }

    IEnumerable<(Signature, Signature)> GeneratePairs(Signer signer, DataSetParameters dataSetParameters)
    {
        var genuinePairs = GenuinePairs(signer)
                .Take(dataSetParameters.genuinePairCountPerSigner);

        var forgeryPairs = ForgeryPairs(signer)
                .Take(dataSetParameters.skilledForgeryCountPerSigner);

        var randomPairs = RandomPairs(signer)
                .Take(dataSetParameters.randomForgeryCountPerSigner);

        return genuinePairs
            .Concat(forgeryPairs)
            .Concat(randomPairs);
    }

    IEnumerable<(Signature, Signature)> GeneratePairs(IEnumerable<Signer> signers, DataSetParameters dataSetParameters)
    {
        return signers.SelectMany(signer => GeneratePairs(signer, dataSetParameters));
    }


    (IEnumerable<(Signature, Signature)>, IEnumerable<(Signature, Signature)>) GenerateTrainingAndTestPairs(
        DataSetParameters trainingSetParameters,
        DataSetParameters testSetParameters,
        int seed)
    {
        random = new Random(seed);

        var randomSigners = signers
            .Where(signer => FilterSignatures(signer.Signatures).Any())
            .RandomOrder(random)
            .ToList();

        var trainingPairs = GeneratePairs(
            randomSigners
                .Take(trainingSetParameters.signerCount), trainingSetParameters
        );

        var testPairs = GeneratePairs(
            randomSigners
                .Take(new Range(
                    trainingSetParameters.signerCount,
                    trainingSetParameters.signerCount + testSetParameters.signerCount)
                ),
            testSetParameters
        );

        return (trainingPairs, testPairs);
    }

    SignatureStatistics CalculateSignatureStatistics(Signature signature)
    {
        var statistics = new SignatureStatistics();

        sequentialTransformPipeline.Transform(signature);

        var x = signature.GetFeature(Features.X);
        statistics.stdevX = MathHelper.StdDiviation(x);

        var y = signature.GetFeature(Features.Y);
        statistics.stdevY = MathHelper.StdDiviation(y);

        var p = signature.GetFeature(Features.Pressure);
        statistics.stdevP = MathHelper.StdDiviation(p);

        var t = signature.GetFeature(Svc2021.T);
        statistics.count = t.Count;
        statistics.duration = t.Last() - t.First();

        return statistics;
    }

    IList<SignaturePairStatistics> CalculatePairStatistics(IEnumerable<(Signature, Signature)> pairs)
    {
        var signaturePairStatisticsList = new List<SignaturePairStatistics>();

        foreach ((Signature signature1, Signature signature2) in pairs)
        {
            var statistics = new SignaturePairStatistics()
            {
                referenceSignature = signature1,
                questionedSignature = signature2,
                signatureStatistics1 = CalculateSignatureStatistics(signature1),
                signatureStatistics2 = CalculateSignatureStatistics(signature2)
            };

            if (signature1.Signer == signature2.Signer)
            {
                if (signature2.Origin == Origin.Genuine)
                {
                    statistics.origin = SignaturePairOrigin.Genuine;
                    statistics.expectedPrediction = 1;
                }
                else
                {
                    statistics.origin = SignaturePairOrigin.Forged;
                    statistics.expectedPrediction = 0;
                }
            }
            else
            {
                statistics.origin = SignaturePairOrigin.Random;
                statistics.expectedPrediction = 0;
            }

            {
                zNormalizationPipeline.Transform(signature1);
                zNormalizationPipeline.Transform(signature2);

                var signature1Points = signature1
                    .GetAggregateFeature(new List<FeatureDescriptor>() { Features.X, Features.Y })
                    .ToArray();

                var signature2Points = signature2
                    .GetAggregateFeature(new List<FeatureDescriptor>() { Features.X, Features.Y })
                    .ToArray();

                statistics.diffDtw = dtwDistance.Calculate(signature1Points, signature2Points);
            }

            statistics.diffX = Math.Abs(
                (statistics.signatureStatistics1.stdevX - statistics.signatureStatistics2.stdevX) / statistics.signatureStatistics1.stdevX
            );

            statistics.diffY = Math.Abs(
                (statistics.signatureStatistics1.stdevY - statistics.signatureStatistics2.stdevY) / statistics.signatureStatistics1.stdevY
            );

            statistics.diffP = Math.Abs(
                (statistics.signatureStatistics1.stdevP - statistics.signatureStatistics2.stdevP) / statistics.signatureStatistics1.stdevP
            );

            statistics.diffCount = Math.Abs(
                ((double)statistics.signatureStatistics2.count / statistics.signatureStatistics1.count) - 1
            );

            statistics.diffDuration = Math.Abs(
                ((double)statistics.signatureStatistics2.duration / statistics.signatureStatistics1.duration) - 1
            );

            signaturePairStatisticsList.Add(statistics);
        }

        return signaturePairStatisticsList;
    }

    internal void Save(
        DataSetParameters trainingSetParameters,
        DataSetParameters testSetParameters,
        int seed,
        IDataSetExporter dataSetExporter)
    {
        dataSetExporter.SaveInfo(seed.ToString(), trainingSetParameters, testSetParameters, seed);

        var (trainingPairs, testPairs) = GenerateTrainingAndTestPairs(trainingSetParameters, testSetParameters, seed);

        var trainingSet = CalculatePairStatistics(trainingPairs);
        dataSetExporter.Export(seed.ToString(), trainingSetParameters.name, trainingSet);

        var testSet = CalculatePairStatistics(testPairs);
        dataSetExporter.Export(seed.ToString(), testSetParameters.name, testSet);
    }
}
