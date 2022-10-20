namespace SigStatCompare.Models.Exporters;

class CSVExporter : DataSetExporterBase
{
    public override void Export(string foldername, string filename, IEnumerable<SignaturePairStatistics> pairStatistics)
    {
        string folderPath = CreateDirectory(foldername);

        using var file = new StreamWriter(Path.Combine(folderPath, filename + ".csv"));

        file.WriteLine(string.Join(',', Headers));
        foreach (var statistics in pairStatistics)
        {
            file.WriteLine(string.Join(',', statistics.ToList()));
        }
    }
}
