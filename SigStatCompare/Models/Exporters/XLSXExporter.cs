using OfficeOpenXml;
using SigStat.Common.Helpers;

namespace SigStatCompare.Models.Exporters;

class XLSXExporter : DataSetExporterBase
{
    public override void Export(string filename, IList<SignaturePairStatistics> pairStatistics)
    {
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string sigStatComparePath = Path.Combine(documentsPath, "SigStatCompare");


        Directory.CreateDirectory(sigStatComparePath);

        using var excelPackage = new ExcelPackage();

        var excelWorksheet = excelPackage.Workbook.Worksheets.Add("Test");

        var data = pairStatistics
            .Select(statistics => statistics.ToList());

        var excelRange = excelWorksheet.InsertTable(1, 1, data, Headers);

        excelPackage.SaveAs(new FileInfo(Path.Combine(sigStatComparePath, filename + ".xlsx")));
    }
}
