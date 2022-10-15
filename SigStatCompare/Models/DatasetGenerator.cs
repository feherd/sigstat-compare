using OfficeOpenXml;
using SigStat.Common;
using SigStat.Common.Pipeline;
using SigStat.Common.PipelineItems.Transforms.Preprocessing;
using SVC2021;
using SVC2021.Entities;
using SigStat.Common.Helpers;
using SigStatCompare.Models.Transformations;

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
        var signatures = signer.Signatures.Where(sig => sig.Origin == Origin.Genuine).ToList();
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

    IEnumerable<(Signature, Signature)> ForgeryPairs(Signer signer, Random random)
    {
        var genuineSignatures = signer.Signatures.Where(sig => sig.Origin == Origin.Genuine).ToList();
        var forgedSignatures = signer.Signatures.Where(sig => sig.Origin == Origin.Forged).ToList();
        var n = genuineSignatures.Count;
        var m = forgedSignatures.Count;

        var pairIndices = Enumerable
            .Range(0, n * m)
            .Select(i => (i / m, i % m))
            .ToList();

        while (pairIndices.Count > 1)
        {
            int index = random.Next(pairIndices.Count);
            (int genuineSignatureIndex, int forgedSignatureIndex) = pairIndices[index];
            pairIndices.RemoveAt(index);

            yield return (
                genuineSignatures[genuineSignatureIndex],
                forgedSignatures[forgedSignatureIndex]
            );
        }
    }

    IEnumerable<(Signature, Signature)> RandomPairs(Signer signer, Random random)
    {
        var genuineSignatures = signer.Signatures.Where(sig => sig.Origin == Origin.Genuine).ToList();
        var randomSignatures = signers.Where(s => s != signer).SelectMany(s => s.Signatures).ToList();
        var n = genuineSignatures.Count;
        var m = randomSignatures.Count;

        var pairIndices = Enumerable
            .Range(0, n * m)
            .Select(i => (i / m, i % m))
            .ToList();

        while (pairIndices.Count > 1)
        {
            int index = random.Next(pairIndices.Count);
            (int genuineSignatureIndex, int randomSignatureIndex) = pairIndices[index];
            pairIndices.RemoveAt(index);

            yield return (
                genuineSignatures[genuineSignatureIndex],
                randomSignatures[randomSignatureIndex]
            );
        }
    }

    IEnumerable<Signer> RandomSigners(Random random)
    {
        var signers1 = new List<Signer>(signers);

        while (signers1.Count > 0)
        {
            int index = random.Next(signers1.Count);
            Signer signer = signers1[index];

            yield return signer;
        }
    }

    IList<(Signature, Signature)> GeneratePairs(DataSetParameters dataSetParameters, int seed)
    {
        var random = new Random(seed);

        var signaturePairs = new List<(Signature, Signature)>();

        foreach (var signer in RandomSigners(random).Take(dataSetParameters.signerCount))
        {
            signaturePairs.AddRange(
                GenuinePairs(signer, random)
                    .Take(dataSetParameters.genuinePairCountPerSigner)
            );

            signaturePairs.AddRange(
                ForgeryPairs(signer, random)
                    .Take(dataSetParameters.skilledForgeryCountPerSigner)
            );

            signaturePairs.AddRange(
                RandomPairs(signer, random)
                    .Take(dataSetParameters.randomForgeryCountPerSigner)
            );
        }

        return signaturePairs;
    }

    SignatureStatistics CalculateSignatureStatistics(Signature signature)
    {
        var statistics = new SignatureStatistics();

        sequentialTransformPipeline.Transform(signature);
        {
            var x = signature.GetFeature(Features.X);
            var meanX = x.Average();
            statistics.stdevX = Math.Sqrt(x.Select(d => (d - meanX) * (d - meanX)).Sum() / (x.Count - 1));
        }
        {
            var y = signature.GetFeature(Features.Y);
            var meanY = y.Average();
            statistics.stdevY = Math.Sqrt(y.Select(d => (d - meanY) * (d - meanY)).Sum() / (y.Count - 1));
        }
        {
            var p = signature.GetFeature(Features.Pressure);
            var meanP = p.Average();
            statistics.stdevP = Math.Sqrt(p.Select(d => (d - meanP) * (d - meanP)).Sum() / (p.Count - 1));
        }
        {
            var t = signature.GetFeature(Svc2021.T);
            statistics.count = t.Count;
            statistics.duration = t.Last() - t.First();
        }

        return statistics;
    }

    IList<SignaturePairStatistics> CalculatePairStatistics(IList<(Signature, Signature)> pairs)
    {
        var signaturePairStatisticsList = new List<SignaturePairStatistics>();

        foreach ((Signature signature1, Signature signature2) pair in pairs)
        {
            var statistics = new SignaturePairStatistics()
            {
                referenceSignature = pair.signature1,
                questionedSignature = pair.signature2,
                expectedPrediction = pair.signature1.Origin == Origin.Genuine && pair.signature2.Origin == Origin.Genuine ? 1 : 0,
                signatureStatistics1 = CalculateSignatureStatistics(pair.signature1),
                signatureStatistics2 = CalculateSignatureStatistics(pair.signature2)
            };

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

    internal void SaveToCSV(DataSetParameters dataSetParameters, int seed)
    {
        IList<(Signature, Signature)> pairs = GeneratePairs(dataSetParameters, seed);

        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string sigStatComparePath = Path.Combine(documentsPath, "SigStatCompare");


        Directory.CreateDirectory(sigStatComparePath);

        using var file = new StreamWriter(Path.Combine(sigStatComparePath, "test.csv"));

        file.WriteLine(
            "ReferenceSignatureFile,"
            + "ReferenceSigner,"
            + "ReferenceInput,"
            + "QuestionedSignatureFile,"
            + "QuestionedSigner,"
            + "QuestionedInput,"
            + "Origin,"
            + "ExpectedPrediction,"
            + "stdevX1,"
            + "stdevY1,"
            + "stdevP1,"
            + "count1,"
            + "duration1,"
            + "stdevX2,"
            + "stdevY2,"
            + "stdevP2,"
            + "count2,"
            + "duration2,"
            + "diffDTW,"
            + "diffX,"
            + "diffY,"
            + "diffP,"
            + "diffCount,"
            + "diffDuration"
        );

        foreach (var statistics in CalculatePairStatistics(pairs))
        {
            file.WriteLine(string.Join(',', statistics.ToList()));
        }
    }

    internal void SaveToXLSX(DataSetParameters dataSetParameters, int seed)
    {
        IList<(Signature, Signature)> pairs = GeneratePairs(dataSetParameters, seed);

        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string sigStatComparePath = Path.Combine(documentsPath, "SigStatCompare");


        Directory.CreateDirectory(sigStatComparePath);

        using var excelPackage = new ExcelPackage();

        var excelWorksheet = excelPackage.Workbook.Worksheets.Add("Test");

        var data = CalculatePairStatistics(pairs)
            .Select(statistics => statistics.ToList());

        IList<string> headers = new List<string>(){
            "ReferenceSignatureFile",
            "ReferenceSigner",
            "ReferenceInput",
            "QuestionedSignatureFile",
            "QuestionedSigner",
            "QuestionedInput",
            "Origin",
            "ExpectedPrediction",
            "stdevX1",
            "stdevY1",
            "stdevP1",
            "count1",
            "duration1",
            "stdevX2",
            "stdevY2",
            "stdevP2",
            "count2",
            "duration2",
            "diffDTW",
            "diffX",
            "diffY",
            "diffP",
            "diffCount",
            "diffDuration"
        };
        var excelRange = excelWorksheet.InsertTable(
            1, 1,
            data,
            headers
        );

        excelPackage.SaveAs(new FileInfo(Path.Combine(sigStatComparePath, "test.xlsx")));
    }
}
