using Newtonsoft.Json;
using SigStat.Common.Pipeline;

namespace SigStat.Common.PipelineItems.Transforms.Preprocessing
{

    /// <summary>
    /// Maps values of a feature to a specific range.
    /// <para>InputFeature: feature to be scaled.</para>
    /// <para>OutputFeature: output feature for scaled InputFeature</para>
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public class SvcScale : PipelineBase, ITransformation
    {
        /// <summary>
        /// Gets or sets the input feature.
        /// </summary>
        [Input]
        public FeatureDescriptor<List<double>> InputFeature { get; set; }

        /// <summary>
        /// Type of the scaling which defines the scaling behavior
        /// </summary>
        public ScalingMode Mode { get; set; } = ScalingMode.Scaling1;


        /// <summary>
        /// Gets or sets the output feature.
        /// </summary>
        [Output("ScaledFeature")]
        public FeatureDescriptor<List<double>> OutputFeature { get; set; }


        /// <inheritdoc/>
        public void Transform(Signature signature)
        {
            if (InputFeature == null || OutputFeature == null)
                throw new NullReferenceException("Input or output feature is null");

            List<double> values = new List<double>(signature.GetFeature(InputFeature).ToList());

            //find actual min value            
            var oldMinValue = values.Min();

            if (InputFeature == Features.Pressure)
                oldMinValue = 0;

            //translation to 0
            values = values.Select(v => v - oldMinValue).ToList();

            switch (Mode)
            {
                case ScalingMode.Scaling1:
                    var range = values.Max();
                    if (range == 0) range = 1;
                    //scale values between 0 and 1
                    values = values.Select(v => v / range).ToList();
                    break;
                case ScalingMode.ScalingS:
                    var mean = values.Average();
                    var stdev = Math.Sqrt(values.Select(d => (d - mean) * (d - mean)).Sum() / (values.Count - 1));
                    //scale values based on standard deviation
                    values = values.Select(v => v / stdev).ToList();
                    break;
                default:
                    break;
            }

            //translation to the original place
            values = values.Select(v => v + oldMinValue).ToList();

            signature.SetFeature(OutputFeature, values);
        }
    }
}
