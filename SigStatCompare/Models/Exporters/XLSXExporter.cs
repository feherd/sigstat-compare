using OfficeOpenXml;
using SigStat.Common.Helpers;

namespace SigStatCompare.Models.Exporters;

class XLSXExporter : DataSetExporterBase
{
    public override void Export(string foldername, string filename, IEnumerable<SignaturePairStatistics> pairStatistics)
    {
        string folderPath = CreateDirectory(foldername);

        using var excelPackage = new ExcelPackage();

        var excelWorksheet = excelPackage.Workbook.Worksheets.Add("Test");

        var data = pairStatistics
            .Select(statistics => statistics.ToList());

        var excelRange = excelWorksheet.InsertTable(1, 1, data, Headers);

        excelPackage.SaveAs(new FileInfo(Path.Combine(folderPath, filename + ".xlsx")));
    }
}
