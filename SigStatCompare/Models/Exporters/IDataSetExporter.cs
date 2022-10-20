namespace SigStatCompare.Models.Exporters;

interface IDataSetExporter
{
    void Export(string filename, IList<SignaturePairStatistics> pairStatistics);

    void SaveInfo(
        string filename,
        DataSetParameters trainingSetParameters,
        DataSetParameters testSetParameters,
        int seed
    );
}
