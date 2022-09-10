using Microsoft.Maui.Controls.Shapes;
using SigStat.Common;
using SigStat.Common.PipelineItems.Transforms.Preprocessing;
using SigStatCompare.Models;

namespace SigStatCompare.Views;

public class DtwVisualizer : GraphicsView
{
    public static readonly BindableProperty FirstSignatureProperty =
        BindableProperty.Create(nameof(FirstSignature), typeof(Signature), typeof(DtwVisualizer), null, propertyChanged: FirstSignatureChanged);
    public Signature FirstSignature
    {
        get => (Signature)GetValue(FirstSignatureProperty);
        set => SetValue(FirstSignatureProperty, value);
    }

    private static void FirstSignatureChanged(BindableObject bindableObject, object oldValue, object newValue)
    {
        var visualizer = bindableObject as DtwVisualizer;
        visualizer?.Invalidate();
    }

    public static readonly BindableProperty SecondSignatureProperty =
        BindableProperty.Create(nameof(SecondSignature), typeof(Signature), typeof(DtwVisualizer), null, propertyChanged: SecondSignatureChanged);
    public Signature SecondSignature
    {
        get => (Signature)GetValue(SecondSignatureProperty);
        set => SetValue(SecondSignatureProperty, value);
    }

    private static void SecondSignatureChanged(BindableObject bindableObject, object oldValue, object newValue)
    {
        var visualizer = bindableObject as DtwVisualizer;
        visualizer?.Invalidate();
    }

    public static readonly BindableProperty ShowAxesProperty =
        BindableProperty.Create(nameof(ShowAxes), typeof(bool), typeof(DtwVisualizer), true, propertyChanged: ShowAxesChanged);
    public bool ShowAxes
    {
        get => (bool)GetValue(ShowAxesProperty);
        set => SetValue(ShowAxesProperty, value);
    }

    private static void ShowAxesChanged(BindableObject bindableObject, object oldValue, object newValue)
    {
        var visualizer = bindableObject as DtwVisualizer;
        visualizer?.Invalidate();
    }

    private Point lastOffset;

    public static readonly BindableProperty OffsetProperty =
        BindableProperty.Create(nameof(Offset), typeof(Point), typeof(DtwVisualizer), propertyChanged: OffsetChanged);
    public Point Offset
    {
        get => (Point)GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    private static void OffsetChanged(BindableObject bindableObject, object oldValue, object newValue)
    {
        var visualizer = bindableObject as DtwVisualizer;
        visualizer?.Invalidate();
    }

    public static readonly BindableProperty FeatureProperty =
        BindableProperty.Create(nameof(Feature), typeof(FeatureDescriptor<List<double>>), typeof(DtwVisualizer), defaultValue: Features.X, propertyChanged: FeatureChanged);
    public FeatureDescriptor<List<double>> Feature
    {
        get => (FeatureDescriptor<List<double>>)GetValue(FeatureProperty);
        set => SetValue(FeatureProperty, value);
    }

    private static void FeatureChanged(BindableObject bindableObject, object oldValue, object newValue)
    {
        var visualizer = bindableObject as DtwVisualizer;
        visualizer?.Invalidate();
    }

    public DtwVisualizer()
    {
        Drawable = new DtwDrawable(this);

        // Set PanGestureRecognizer.TouchPoints to control the
        // number of touch points needed to pan
        PanGestureRecognizer panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        GestureRecognizers.Add(panGesture);
    }

    void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                // Translate and ensure we don't pan beyond the wrapped user interface element bounds.
                var x = lastOffset.X + e.TotalX;
                var y = lastOffset.Y + e.TotalY;

                Offset = new Point(x, y);

                break;

            case GestureStatus.Completed:
                // Store the translation applied during the pan
                lastOffset = Offset;
                break;
        }
    }

    class Plot
    {
        public Rect rect;
        public Signature signature;
        public bool flippedAxes;
        private readonly FeatureDescriptor<List<double>> outputFeature;

        public Rect SignatureRect
        {
            get
            {
                var tt = signature.GetFeature(Features.T);
                var ft = signature.GetFeature(outputFeature);
                return new Rect(
                    tt.Min(), ft.Min(),
                    tt.Max() - tt.Min(), ft.Max() - ft.Min()
                );
            }
        }

        public Matrix Transformation
        {
            get
            {
                var sr = SignatureRect;

                var transformation = new Matrix();
                transformation.Translate(-sr.X, -sr.Y);
                transformation.Scale(1 / sr.Width, -1 / sr.Height);
                transformation.Scale(rect.Width, rect.Height);
                transformation.Translate(rect.Left, rect.Bottom);

                return transformation;
            }
        }


        public Plot(FeatureDescriptor<List<double>> outputFeature)
        {
            this.outputFeature = outputFeature;
        }
    }

    class DtwDrawable : IDrawable
    {
        private readonly ZNormalization zNormalization = new();

        private readonly DtwVisualizer dtwVisualizer;
        private readonly double padding = 40;
        private readonly Plot firstPlot;
        private readonly Plot secondPlot;

        public DtwDrawable(DtwVisualizer dtwVisualizer)
        {
            this.dtwVisualizer = dtwVisualizer;

            firstPlot = new Plot(zNormalization.OutputFeature)
            {
                flippedAxes = true
            };
            secondPlot = new Plot(zNormalization.OutputFeature);
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = dtwVisualizer.BackgroundColor;
            canvas.FillRectangle(dirtyRect);

            if (dirtyRect.Width <= 2 * padding || dirtyRect.Height <= 4 * padding) return;

            zNormalization.InputFeature = dtwVisualizer.Feature;

            CalculateTransformation(dirtyRect);

            if (dtwVisualizer.FirstSignature is not null)
            {
                zNormalization.Transform(dtwVisualizer.FirstSignature);
                firstPlot.signature = dtwVisualizer.FirstSignature;
            }

            if (dtwVisualizer.SecondSignature is not null)
            {
                zNormalization.Transform(dtwVisualizer.SecondSignature);
                secondPlot.signature = dtwVisualizer.SecondSignature;
            }

            if (dtwVisualizer.FirstSignature is not null && dtwVisualizer.SecondSignature is not null)
                DrawDtwLines(canvas, firstPlot.Transformation, secondPlot.Transformation);

            if (dtwVisualizer.FirstSignature is not null)
            {
                if (dtwVisualizer.ShowAxes)
                    DrawAxes(canvas, firstPlot);

                DrawFeatureFunction(canvas, dtwVisualizer.FirstSignature, firstPlot.Transformation);
            }

            if (dtwVisualizer.SecondSignature is not null)
            {
                if (dtwVisualizer.ShowAxes)
                    DrawAxes(canvas, secondPlot);

                DrawFeatureFunction(canvas, dtwVisualizer.SecondSignature, secondPlot.Transformation);
            }
        }

        private void CalculateTransformation(RectF dirtyRect)
        {
            var size = new Size(
                dirtyRect.Width - 2 * padding,
                (dirtyRect.Height - 4 * padding) / 2
            );

            firstPlot.rect.Size = size;
            secondPlot.rect.Size = size;

            firstPlot.rect.Location = new Point(padding, padding);
            secondPlot.rect.Location = new Point(padding, 3 * padding + size.Height);
        }

        private void DrawFeatureFunction(ICanvas canvas, Signature signature, Matrix transformMatrix)
        {
            if (signature == null) return;
            var strokes = signature.GetStrokes();
            var tt = signature.GetFeature(Features.T);
            var ft = signature.GetFeature(zNormalization.OutputFeature);

            double tRange = tt.Max() - tt.Min();
            double fRange = ft.Max() - ft.Min();

            canvas.StrokeSize = 3;
            canvas.StrokeLineJoin = LineJoin.Round;

            foreach (var stroke in strokes)
            {
                canvas.StrokeColor = stroke.StrokeType == StrokeType.Down ? Colors.Blue : Colors.Red;

                var polyline = new PathF();
                var points = tt.Zip(ft, (x, y) => new Point(x, y));

                foreach (var point in points)
                    polyline.LineTo(transformMatrix.Transform(point));

                canvas.DrawPath(polyline);
            }
        }

        private void DrawDtwLines(ICanvas canvas, Matrix firstTransformMatrix, Matrix secondTransformMatrix)
        {
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 1;
            canvas.StrokeLineCap = LineCap.Square;

            var ftt = dtwVisualizer.FirstSignature.GetFeature(Features.T);
            var fft = dtwVisualizer.FirstSignature.GetFeature(zNormalization.OutputFeature);

            var stt = dtwVisualizer.SecondSignature.GetFeature(Features.T);
            var sft = dtwVisualizer.SecondSignature.GetFeature(zNormalization.OutputFeature);

            var dtw = new Dtw<double>(fft, sft, (f, s) => Math.Abs(s - f));

            foreach ((var firstIndex, var secondIndex) in dtw.GetPath())
            {
                canvas.DrawLine(
                    firstTransformMatrix.Transform(new Point(ftt[firstIndex], fft[firstIndex])),
                    secondTransformMatrix.Transform(new Point(stt[secondIndex], sft[secondIndex]))
                );
            }
        }

        private static void DrawAxes(ICanvas canvas, Plot plot)
        {
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 1;
            canvas.StrokeLineCap = LineCap.Square;

            var transformation = plot.Transformation;

            var sr = plot.SignatureRect;

            canvas.DrawLine(
                transformation.Transform(new Point(sr.Left, sr.Bottom)) + new Size(0, -10),
                transformation.Transform(new Point(sr.Left, sr.Top)) + new Size(0, +10)
            );
            canvas.DrawLine(
                transformation.Transform(new Point(sr.Left, sr.Bottom)) + new Size(0, -10),
                transformation.Transform(new Point(sr.Left, sr.Bottom)) + new Size(+4, -5)
            );
            canvas.DrawLine(
                transformation.Transform(new Point(sr.Left, sr.Bottom)) + new Size(0, -10),
                transformation.Transform(new Point(sr.Left, sr.Bottom)) + new Size(-4, -5)
            );

            for (var v = sr.Top; v <= sr.Bottom; v += sr.Height / (plot.rect.Height / 50))
            {
                var point = transformation.Transform(new Point(sr.Left, v));
                canvas.DrawLine(point, point + new Size(-5, 0));
                canvas.DrawString(
                    v.ToString("+0.0;-0.0;0"),
                    (float)point.X - 50 - 10, (float)point.Y - 10,
                    50, 20,
                    HorizontalAlignment.Right,
                    VerticalAlignment.Center
                );
            }

            canvas.DrawLine(
                transformation.Transform(new Point(sr.Left, plot.flippedAxes ? sr.Bottom : sr.Top)) + new Size(-10, 0),
                transformation.Transform(new Point(sr.Right, plot.flippedAxes ? sr.Bottom : sr.Top)) + new Size(+10, 0)
            );
            canvas.DrawLine(
                transformation.Transform(new Point(sr.Right, plot.flippedAxes ? sr.Bottom : sr.Top)) + new Size(+10, 0),
                transformation.Transform(new Point(sr.Right, plot.flippedAxes ? sr.Bottom : sr.Top)) + new Size(+5, +4)
            );
            canvas.DrawLine(
                transformation.Transform(new Point(sr.Right, plot.flippedAxes ? sr.Bottom : sr.Top)) + new Size(+10, 0),
                transformation.Transform(new Point(sr.Right, plot.flippedAxes ? sr.Bottom : sr.Top)) + new Size(+5, -4)
            );

            for (var v = sr.Left; v <= sr.Right; v += sr.Width / (plot.rect.Width / 50))
            {
                var point = transformation.Transform(new Point(v, plot.flippedAxes ? sr.Bottom : sr.Top));
                canvas.DrawLine(point, point + new Size(0, plot.flippedAxes ? -5 : +5));
                canvas.DrawString(
                    (v - sr.Left).ToString("+0;-0;0"),
                    (float)point.X - 25, (float)point.Y + (plot.flippedAxes ? -20 : +20) - 10,
                    50, 20,
                    HorizontalAlignment.Center,
                    plot.flippedAxes ? VerticalAlignment.Bottom : VerticalAlignment.Top
                );
            }
        }
    }
}
