namespace SigStatCompare.Models.Exporters;

class CSVExporter : DataSetExporterBase
{
    public override void Export(string filename, IList<SignaturePairStatistics> pairStatistics)
    {
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string sigStatComparePath = Path.Combine(documentsPath, "SigStatCompare");

        Directory.CreateDirectory(sigStatComparePath);

        using var file = new StreamWriter(Path.Combine(sigStatComparePath, filename + ".csv"));

        file.WriteLine(string.Join(',', Headers));
        foreach (var statistics in pairStatistics)
        {
            file.WriteLine(string.Join(',', statistics.ToList()));
        }
    }
}
