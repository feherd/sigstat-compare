using SigStat.Common;
using SigStat.Common.Pipeline;

namespace SigStatCompare.Models.Transformations;

class IntToDoubleConverterTransformation : PipelineBase, ITransformation
{
    [Input]
    FeatureDescriptor<List<int>> IntInput { get; set; }

    [Output]
    FeatureDescriptor<List<double>> DoubleOutput { get; set; }

    public IntToDoubleConverterTransformation(FeatureDescriptor<List<int>> intInput, FeatureDescriptor<List<double>> doubleOutput)
    {
        IntInput = intInput;
        DoubleOutput = doubleOutput;
    }

    public void Transform(Signature signature)
    {
        signature.SetFeature(DoubleOutput,
            signature.GetFeature(IntInput)
                .Select(i => (double)i)
                .ToList()
        );
    }
}
