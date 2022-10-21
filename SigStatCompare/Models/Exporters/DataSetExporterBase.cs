namespace SigStatCompare.Models.Exporters;

abstract class DataSetExporterBase : IDataSetExporter
{
    protected IList<string> Headers => new List<string>(){
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

    public abstract void Export(string foldername, string filename, IEnumerable<SignaturePairStatistics> pairStatistics);

    public void SaveInfo(
        string foldername,
        ISet<SVC2021.DB> dBs,
        ISet<SVC2021.InputDevice> inputDevices,
        ISet<SVC2021.Split> splits,
        DataSetParameters trainingSetParameters,
        DataSetParameters testSetParameters,
        int seed
    )
    {
        string folderPath = CreateDirectory(foldername);

        using var file = new StreamWriter(Path.Combine(folderPath, "info.txt"));

        const string Separator = ", ";
        file.WriteLine($"DB: {string.Join(Separator, dBs)}");
        file.WriteLine($"InputDevice: {string.Join(Separator, inputDevices)}");
        file.WriteLine($"Split: {string.Join(Separator, splits)}");
        file.WriteLine();
        file.WriteLine("Training:");
        file.WriteLine($"Signer count: {trainingSetParameters.signerCount}");
        file.WriteLine($"Genuine pair count per signer: {trainingSetParameters.genuinePairCountPerSigner}");
        file.WriteLine($"Skilled forgery count per signer: {trainingSetParameters.skilledForgeryCountPerSigner}");
        file.WriteLine($"Random forgery count per signer: {trainingSetParameters.randomForgeryCountPerSigner}");
        file.WriteLine();
        file.WriteLine("Test:");
        file.WriteLine($"Signer count: {testSetParameters.signerCount}");
        file.WriteLine($"Genuine pair count per signer: {testSetParameters.genuinePairCountPerSigner}");
        file.WriteLine($"Skilled forgery count per signer: {testSetParameters.skilledForgeryCountPerSigner}");
        file.WriteLine($"Random forgery count per signer: {testSetParameters.randomForgeryCountPerSigner}");
        file.WriteLine();
        file.WriteLine($"Seed {seed}");
    }

    protected string CreateDirectory(string foldername)
    {
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string sigStatComparePath = Path.Combine(documentsPath, "SigStatCompare");

        Directory.CreateDirectory(sigStatComparePath);
        Directory.CreateDirectory(Path.Combine(sigStatComparePath, foldername));

        string folderPath = Path.Combine(sigStatComparePath, foldername);

        return folderPath;
    }
}
