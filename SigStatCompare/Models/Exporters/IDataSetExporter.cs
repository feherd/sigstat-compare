namespace SigStatCompare.Models.Exporters;

interface IDataSetExporter
{
    void Export(string foldername, string filename, IEnumerable<SignaturePairStatistics> pairStatistics);

    void SaveInfo(
        string foldername,
        ISet<SVC2021.DB> dBs,
        ISet<SVC2021.InputDevice> inputDevices,
        ISet<SVC2021.Split> splits,
        DataSetParameters trainingSetParameters,
        DataSetParameters testSetParameters,
        int seed
    );
}
