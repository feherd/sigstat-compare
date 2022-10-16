namespace SigStatCompare.Models.Exporters;

class CSVExporter : IDataSetExporter
{
    public void Export(string filename, IList<SignaturePairStatistics> pairStatistics)
    {
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string sigStatComparePath = Path.Combine(documentsPath, "SigStatCompare");

        Directory.CreateDirectory(sigStatComparePath);

        using var file = new StreamWriter(Path.Combine(sigStatComparePath, filename + ".csv"));

        file.WriteLine(string.Join(',', IDataSetExporter.Headers));
        foreach (var statistics in pairStatistics)
        {
            file.WriteLine(string.Join(',', statistics.ToList()));
        }
    }
}
