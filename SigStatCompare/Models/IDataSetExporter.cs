namespace SigStatCompare.Models;

interface IDataSetExporter
{
    static IList<string> Headers => new List<string>(){
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

    void Export(string filename, IList<SignaturePairStatistics> pairStatistics);
}
