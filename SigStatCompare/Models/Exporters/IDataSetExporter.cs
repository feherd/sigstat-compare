namespace SigStatCompare.Models.Exporters;

interface IDataSetExporter
{
    void Export(string foldername, string filename, IEnumerable<SignaturePairStatistics> pairStatistics);

    void SaveInfo(
        string foldername,
        DataSetParameters trainingSetParameters,
        DataSetParameters testSetParameters,
        int seed
    );
}
