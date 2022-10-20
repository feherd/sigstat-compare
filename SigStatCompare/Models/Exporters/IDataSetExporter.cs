namespace SigStatCompare.Models.Exporters;

interface IDataSetExporter
{
    void Export(string foldername, string filename, IList<SignaturePairStatistics> pairStatistics);

    void SaveInfo(
        string foldername,
        string filename,
        DataSetParameters trainingSetParameters,
        DataSetParameters testSetParameters,
        int seed
    );
}
