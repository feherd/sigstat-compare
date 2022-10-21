using SigStat.Common.Helpers;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Reflection;
using SigStat.Common;
using SigStat.Common.Loaders;
using SVC2021.Entities;
using System.Globalization;

namespace SVC2021;

public enum DB
{
    Mcyt,
    eBioSignDS1,
    eBioSignDS2,
    BiosecurID,
    BiosecureDS2,
    EvalDB
}

public enum InputDevice
{
    Unkown,
    Finger,
    Stylus
}

[Flags]
public enum Split
{
    Unkonwn = 0,
    Development = 1,
    Evaluation = 2
}



/// <summary>
/// Set of features containing raw data loaded from SVC2004-format database.
/// </summary>
public static class Svc2021
{
    public static readonly FeatureDescriptor<string> FileName = FeatureDescriptor.Get<string>("Svc2021.FileName");

    public static readonly FeatureDescriptor<DB> DB = FeatureDescriptor.Get<DB>("Svc2021.DB");
    public static readonly FeatureDescriptor<Split> Split = FeatureDescriptor.Get<Split>("Svc2021.Split");
    public static readonly FeatureDescriptor<InputDevice> InputDevice = FeatureDescriptor.Get<InputDevice>("Svc2021.InputDevice");

    public static readonly FeatureDescriptor<bool> IsPreprocessed = FeatureDescriptor.Get<bool>("Svc2021.IsPreprocessed");


    /// <summary>
    /// X cooridnates from the online signature imported from the SVC2021 database
    /// </summary>
    public static readonly FeatureDescriptor<List<int>> X = FeatureDescriptor.Get<List<int>>("Svc2021.X");
    /// <summary>
    /// Y cooridnates from the online signature imported from the SVC2021 database
    /// </summary>
    public static readonly FeatureDescriptor<List<int>> Y = FeatureDescriptor.Get<List<int>>("Svc2021.Y");
    /// <summary>
    /// T values from the online signature imported from the SVC2021 database
    /// </summary>
    public static readonly FeatureDescriptor<List<long>> T = FeatureDescriptor.Get<List<long>>("Svc2021.T");

    /// <summary>
    /// Pressure values from the online signature imported from the SVC2021 database
    /// </summary>
    public static readonly FeatureDescriptor<List<double>> Pressure = FeatureDescriptor.Get<List<double>>("Svc2021.Pressure");

    /// <summary>
    /// A list of all Svc2004 feature descriptors
    /// </summary>
    public static readonly FeatureDescriptor[] All =
        typeof(Svc2021)
        .GetFields(BindingFlags.Static | BindingFlags.Public)
        .Where(f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(FeatureDescriptor<>))
        .Select(f => (FeatureDescriptor)f.GetValue(null))
        .ToArray();

}

/// <summary>
/// Loads SVC2021-format database from .zip
/// </summary>
[JsonObject(MemberSerialization.OptOut)]
public class Svc2021Loader : DataSetLoader
{
    private static readonly IFormatProvider numberFormat = new CultureInfo("EN-US").NumberFormat;
    /// <summary>
    /// Sampling Frequency of the SVC database
    /// </summary>
    public override int SamplingFrequency { get { return 100; } }

    public struct SignatureFile
    {
        public string File { get; set; }
        public string SignerID { get; set; }
        public string SignatureID { get; set; }
        public DB DB { get; set; }
        public Split Split { get; set; }
        public InputDevice InputDevice { get; set; }
        public Origin Origin { get; set; }

        public SignatureFile(string file)
        {
            this.File = file;
            string[] pathParts = file.Contains("/") ? file.Split('/') : file.Split('\\');

            //EvalDB
            if (file.Contains("signature"))
            {
                this.SignatureID = string.Join('\\', pathParts[^1]);
                this.SignerID = string.Join('\\', pathParts[^1]);
                this.DB = DB.EvalDB;
                this.Split = Split.Evaluation;
                this.Origin = Origin.Unknown;
                this.InputDevice = InputDevice.Unkown;
                return;
            }


            this.Split = Enum.Parse<Split>(pathParts[^3], true);
            this.InputDevice = Enum.Parse<InputDevice>(pathParts[^2], true);

            var parts = pathParts[^1].Split("_");
            if (parts[1] == "g")
                this.Origin = Origin.Genuine;
            else if (parts[1] == "s")
                this.Origin = Origin.Forged;
            else
                throw new NotSupportedException($"Unsupported origin: {parts[1]}");

            this.SignerID = parts[0].Replace("u", "");
            this.SignatureID = string.Join('\\', pathParts[^3..]); //Path.GetFileNameWithoutExtension(pathParts[^1]);

            this.DB = GetDatabase(Split, InputDevice, SignerID, file);
            //SignerID = parts[0].PadLeft(2, '0');
        }

        public static DB GetDatabase(Split split, InputDevice inputDevice, string signerId, string file)
        {
            switch (split)
            {
                case Split.Development:
                    switch (inputDevice)
                    {
                        case InputDevice.Finger:
                            if (signerId.Between("1009", "1038")) return DB.eBioSignDS1;
                            else if (signerId.Between("1039", "1084")) return DB.eBioSignDS2;
                            else throw new NotSupportedException($"Undefined DB for file: {file}");
                        case InputDevice.Stylus:
                            if (signerId.Between("0001", "0230")) return DB.Mcyt;
                            else if (signerId.Between("0231", "0498")) return DB.BiosecurID;
                            else if (signerId.Between("1009", "1038")) return DB.eBioSignDS1;
                            else if (signerId.Between("1039", "1084")) return DB.eBioSignDS2;
                            else throw new NotSupportedException($"Undefined DB for file: {file}");
                        default:
                            throw new NotSupportedException($"Undefined InputDevice for file: {file}");

                    }
                case Split.Evaluation:
                    switch (inputDevice)
                    {
                        case InputDevice.Finger:
                            if (signerId.Between("0373", "0407")) return DB.eBioSignDS1;
                            else if (signerId.Between("0408", "0442")) return DB.eBioSignDS2;
                            else throw new NotSupportedException($"Undefined DB for file: {file}");
                        case InputDevice.Stylus:
                            if (signerId.Between("0001", "0100")) return DB.Mcyt;
                            else if (signerId.Between("0101", "0232")) return DB.BiosecurID;
                            else if (signerId.Between("0233", "0372")) return DB.BiosecureDS2;
                            else if (signerId.Between("0373", "0407")) return DB.eBioSignDS1;
                            else if (signerId.Between("0408", "0442")) return DB.eBioSignDS2;
                            else throw new NotSupportedException($"Undefined DB for file: {file}");
                        default:
                            throw new NotSupportedException($"Undefined InputDevice for file: {file}");

                    }
                default:
                    throw new NotSupportedException($"Undefined Split for file: {file}");
            }
        }
    }

    /// <summary>
    /// Gets or sets the database path.
    /// </summary>
    public string DatabasePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether features are also loaded as <see cref="Features"/>
    /// </summary>
    public bool StandardFeatures { get; set; }
    /// <summary>
    /// Ignores any signers during the loading, that do not match the predicate
    /// </summary>
    public Predicate<Signer> SignerFilter { get; set; }



    /// <summary>
    /// Initializes a new instance of the <see cref="Svc2021Loader"/> class with specified database.
    /// </summary>
    /// <param name="databasePath">Represents the path, to load the signatures from. It supports two basic approaches:
    /// <list type="bullet">
    /// <item>DatabasePath may point to a (non password protected) zip file, containing the siganture files</item>
    /// <item>DatabasePath may point to a directory with all the signer files or with files grouped in subdirectories</item>
    /// </list></param>
    /// <param name="standardFeatures">Convert loaded data (<see cref="Svc2021"/>) to standard <see cref="Features"/>.</param>
    [JsonConstructor]
    public Svc2021Loader(string databasePath, bool standardFeatures)
    {
        DatabasePath = databasePath;
        StandardFeatures = standardFeatures;
        SignerFilter = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Svc2021Loader"/> class with specified database.
    /// </summary>
    /// <param name="databasePath">Represents the path, to load the signatures from. It supports two basic approaches:
    /// <list type="bullet">
    /// <item>DatabasePath may point to a (non password protected) zip file, containing the siganture files</item>
    /// <item>DatabasePath may point to a directory with all the signer files or with files grouped in subdirectories</item>
    /// </list></param>
    /// <param name="standardFeatures">Convert loaded data (<see cref="Svc2021"/>) to standard <see cref="Features"/>.</param>
    /// <param name="signerFilter">Sets the <see cref="SignerFilter"/> property</param>
    public Svc2021Loader(string databasePath, bool standardFeatures, Predicate<Signer> signerFilter = null)
    {
        DatabasePath = databasePath;
        StandardFeatures = standardFeatures;
        SignerFilter = signerFilter;
    }

    /// <inheritdoc/>
    public override IEnumerable<Signer> EnumerateSigners(Predicate<Signer> signerFilter)
    {

        //TODO: EnumerateSigners should ba able to operate with a directory path, not just a zip file
        signerFilter = signerFilter ?? SignerFilter;

        this.LogInformation("Enumerating signers started.");
        using (ZipArchive zip = ZipFile.OpenRead(DatabasePath))
        {
            IEnumerable<IGrouping<string, SignatureFile>> signatureGroups = DatabasePath switch
            {
                var s when s.EndsWith("DeepSignDB.zip") => zip.Entries
                    .Where(f => f.FullName.StartsWith("DeepSignDB") && f.Name.EndsWith(".txt"))
                    .Select(f => new SignatureFile(f.FullName))
                    .GroupBy(sf => sf.SignerID),
                var s when s.EndsWith("SVC2021_EvalDB.zip") => zip.Entries
                    .Where(f => f.Name.EndsWith(".txt"))
                    .Select(f => new SignatureFile(f.FullName))
                    .GroupBy(sf => sf.SignerID),
                _ => throw new ArgumentException()
            };

            using (var progress = ProgressHelper.StartNew(signatureGroups.Count(), 10))
            {
                foreach (var group in signatureGroups)
                {
                    Signer signer = new Signer { ID = group.Key };

                    if (signerFilter != null && !signerFilter(signer))
                    {
                        continue;
                    }
                    foreach (var signatureFile in group)
                    {
                        Svc2021Signature signature = new Svc2021Signature
                        {
                            Signer = signer,
                            ID = signatureFile.SignatureID,
                            DB = signatureFile.DB,
                            Split = signatureFile.Split,
                            FileName = signatureFile.File,
                            InputDevice = signatureFile.InputDevice,
                            Origin = signatureFile.Origin
                        };
                        using (Stream s = zip.GetEntry(signatureFile.File).Open())
                        {
                            LoadSignature(signature, s, StandardFeatures);
                        }
                        signer.Signatures.Add(signature);


                    }
                    signer.Signatures = signer.Signatures.OrderBy(s => s.ID).ToList();

                    progress.Value++;
                    yield return signer;
                }
            }

            this.LogInformation("Enumerating signers finished.");
        }
    }



    //public IEnumerable<Signer> ListSignersFast(Predicate<Signer> signerFilter)
    //{

    //    //TODO: EnumerateSigners should ba able to operate with a directory path, not just a zip file
    //    signerFilter = signerFilter ?? SignerFilter;

    //    this.LogInformation("Enumerating signers started.");
    //    var signers = new Dictionary<string, Signer>();

    //    using (ZipArchive zip = ZipFile.OpenRead(DatabasePath))
    //    {
    //        var entries = zip.Entries.Where(f => f.FullName.StartsWith("DeepSignDB") && f.Name.EndsWith(".txt"));
    //        //cut names if the files are in directories
    //        using (var progress = ProgressHelper.StartNew(zip.Entries.Count, 1))
    //        {
    //            foreach (var entry in entries)
    //            {
    //                SignatureFile signatureFile = new SignatureFile(entry.FullName);
    //                Signer signer = null;
    //                if (!signers.TryGetValue(signatureFile.SignerID, out signer))
    //                {
    //                    signer = new Signer() { ID = signatureFile.SignerID };
    //                    signers.Add(signatureFile.SignerID, signer);
    //                }
    //                Svc2021Signature signature = new Svc2021Signature
    //                {
    //                    Signer = signer,
    //                    ID = signatureFile.SignatureID,
    //                    DB = signatureFile.DB,
    //                    Split = signatureFile.Split,
    //                    FileName = signatureFile.File,
    //                    InputDevice = signatureFile.InputDevice,
    //                    Origin = signatureFile.Origin
    //                };
    //                using (Stream s = entry.Open())
    //                {
    //                    LoadSignature(signature, s, StandardFeatures);
    //                }
    //                signer.Signatures.Add(signature);
    //                progress.IncrementValue();
    //            }
    //        }
    //    }
    //    return signers.Values;
    //}

    public IEnumerable<Svc2021Signature> LoadSignatures(params string[] signatureIds)
    {
        string[] localIds = signatureIds.ToArray();
        for (int i = 0; i < localIds.Length; i++)
        {
            if (localIds[i].Contains("\\"))
                localIds[i] = localIds[i].Replace("\\", "/");
        }
        this.LogInformation($"Loading {signatureIds.Length} signatures");
        using (ZipArchive zip = ZipFile.OpenRead(DatabasePath))
        {
            // We know the structure of the zip file, therefore we can generate the entry names directly from IDs, 
            // without the need to read the entries once
            var signatureFiles = localIds.Select(id => new SignatureFile("DeepSignDB/" + id)).ToList();

            foreach (var signatureFile in signatureFiles)
            {

                Svc2021Signature signature = new Svc2021Signature
                {
                    Signer = new Signer() { ID = signatureFile.SignerID },
                    ID = signatureFile.SignatureID,
                    DB = signatureFile.DB,
                    Split = signatureFile.Split,
                    FileName = signatureFile.File,
                    InputDevice = signatureFile.InputDevice,
                    Origin = signatureFile.Origin
                };
                using (Stream s = zip.GetEntry(signatureFile.File).Open())
                {
                    LoadSignature(signature, s, StandardFeatures);
                }
                yield return signature;
            }
        }
    }
    /// <summary>
    /// Loads one signature from specified file path.
    /// </summary>
    /// <param name="signature">Signature to write features to.</param>
    /// <param name="path">Path to a file of format "U*S*.txt"</param>
    /// <param name="standardFeatures">Convert loaded data to standard <see cref="Features"/>.</param>
    public void LoadSignature(Signature signature, string path, bool standardFeatures)
    {
        ParseSignature(signature, File.ReadAllLines(path), standardFeatures, Logger);
    }

    /// <summary>
    /// Loads one signature from specified stream.
    /// </summary>
    /// <param name="signature">Signature to write features to.</param>
    /// <param name="stream">Stream to read svc2004 data from.</param>
    /// <param name="standardFeatures">Convert loaded data to standard <see cref="Features"/>.</param>
    public static void LoadSignature(Signature signature, Stream stream, bool standardFeatures)
    {
        using (StreamReader sr = new StreamReader(stream))
        {
            ParseSignature(signature, sr.ReadToEnd().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None), standardFeatures);
        }
    }

    struct Line
    {
        public int X;
        public int Y;
        public long T;
        public double Pressure;

        public Line(Line line)
        {
            this.X = line.X;
            this.Y = line.Y;
            this.T = line.T;
            this.Pressure = line.Pressure;
        }
    }
    private static void ParseSignature(Signature sig, string[] linesArray, bool standardFeatures, ILogger logger = null)
    {
        var signature = (Svc2021Signature)sig;

        // Set pressure column based on database
        int pressureColumn = signature.DB switch
        {
            DB.Mcyt => 5,
            DB.BiosecurID => 6,
            DB.BiosecureDS2 => 6,
            DB.eBioSignDS1 => 3,
            DB.eBioSignDS2 => 3,
            DB.EvalDB => 3,
            _ => throw new NotSupportedException($"Unsupported DB: {signature.DB}")
        };

        List<Line> lines;
        try
        {
            lines = linesArray
                .Skip(1)
                .Where(l => l != "")
                .Select(l => ParseLine(l, pressureColumn, signature.InputDevice))
                .ToList();
        }
        catch (Exception exc)
        {
            throw new Exception("Error parsing signature: " + sig.ID, exc);
        }

        // Sometimes timestamps are missing. In these cases we fill them in with uniform data. E.g: Evaluation\\stylus\\u0114_s_u1015s0001_sg0004.txt
        if (lines.All(l => l.T == 0))
        {
            logger?.LogWarning($"All timestamps for signature {sig.ID} were 0. Compensating with uniform timestamps.");
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i] = new Line(lines[i]) { T = i * 10 };
            }
        }


        //HACK: same timestamp for measurements do not make sense
        // therefore, we remove the second entry
        // a better solution would be to change the timestamps based on their environments
        for (int i = 0; i < lines.Count - 1; i++)
        {
            if (lines[i].T == lines[i + 1].T)
            {
                lines.RemoveAt(i + 1);
                i--;
            }
        }

        // We need to manually calculate the input type for the Eval DB
        if (signature.InputDevice == InputDevice.Unkown)
        {
            if (lines.TrueForAll(l => l.Pressure == 0))
                signature.InputDevice = InputDevice.Finger;
            else
                signature.InputDevice = InputDevice.Stylus;
        }

        if (signature.InputDevice == InputDevice.Stylus)
        {
            // Remove noise (points with 0 pressure) from the beginning of the signature
            while (lines.Count > 0 && lines[0].Pressure == 0)
            {
                lines.RemoveAt(0);
            }
            // Remove noise (points with 0 pressure) from the end of the signature
            while (lines.Count > 0 && lines[lines.Count - 1].Pressure == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }
        }

        if (lines.Count == 0)
            throw new Exception("No lines were loaded for signature: " + signature.ID);


        // Task1, Task2
        signature.SetFeature(Svc2021.X, lines.Select(l => l.X).ToList());
        signature.SetFeature(Svc2021.Y, lines.Select(l => l.Y).ToList());
        signature.SetFeature(Svc2021.T, lines.Select(l => l.T).ToList());
        signature.SetFeature(Svc2021.Pressure, lines.Select(l => l.Pressure).ToList());



        //signature.SetFeature(Svc2021.Button, lines.Select(l => l[3]).ToList());

        // There are some anomalies in the database which have to be eliminated by standard features
        var standardLines = lines.ToList();
        if (standardFeatures)
        {
            // There are no upstrokes in the database, the starting points of downstrokes are marked by button=0 values 
            // Tere are some anomalies in the database: button values between 2-5 and some upstrokes were not deleted               // Button is 2 or 4 if the given point's pressure is 0
            // Button is 1, 3, 5 if the given point is in a downstroke
            //var button = signature.GetFeature(Svc2021.Button).ToArray();
            //var pointType = new double[button.Length];
            //for (int i = 0; i < button.Length; i++)
            //{
            //    if (button[i] == 0)
            //        pointType[i] = 1;
            //    else if (i == button.Length - 1 || (button[i] % 2 == 1 && button[i + 1] % 2 == 0))
            //        pointType[i] = 2;
            //    else if (button[i] == 2 || button[i] == 4)
            //        pointType[i] = 0;
            //    else if (button[i] % 2 == 1 && button[i - 1] % 2 == 0 && button[i - 1] != 0)
            //        pointType[i] = 1;
            //    else
            //        pointType[i] = 0;

            //}


            // Because of the anomalies we have to remove some zero pressure points
            //standardLines.Reverse();
            //var standartPointType = pointType.ToList();
            //standartPointType.Reverse();
            //for (int i = standardLines.Count - 1; i >= 0; i--)
            //{
            //    if (standardLines[i][3] == 2 || standardLines[i][3] == 4)
            //    {
            //        standardLines.RemoveAt(i);
            //        standartPointType.RemoveAt(i); // we have to remove generated point type values of zero pressure points as well
            //    }
            //}
            //standardLines.Reverse();
            //standartPointType.Reverse();


            signature.SetFeature(Features.X, standardLines.Select(l => (double)l.X).ToList());
            signature.SetFeature(Features.Y, standardLines.Select(l => (double)l.Y).ToList());
            signature.SetFeature(Features.T, standardLines.Select(l => (double)l.T).ToList());
            signature.SetFeature(Features.Pressure, standardLines.Select(l => l.Pressure).ToList());

            //signature.SetFeature(Features.PenDown, standardLines.Select(l => l[3] != 0).ToList());
            //signature.SetFeature(Features.PointType, standartPointType);


            //SignatureHelper.CalculateStandardStatistics(signature);


        }


    }

    private static Line ParseLine(string lineString, int pressureColumn, InputDevice inputDevice)
    {
        var parts = lineString.Split(' ');
        return new Line()
        {
            X = int.Parse(parts[0]),
            Y = int.Parse(parts[1]),
            T = long.Parse(parts[2]),
            //Finger datasets do not contain meaningfull pressure information
            Pressure = double.Parse(parts[pressureColumn], numberFormat)
            //inputDevice == InputDevice.Finger ? 1: double.Parse(parts[pressureColumn], numberFormat)
        };
    }


}
