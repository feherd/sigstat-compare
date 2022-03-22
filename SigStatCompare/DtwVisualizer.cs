using Microsoft.Maui.Controls.Shapes;
using SigStat.Common;

namespace SigStatCompare;

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

    public static readonly BindableProperty DisplayModeProperty =
        BindableProperty.Create(nameof(DisplayMode), typeof(DisplayMode), typeof(DtwVisualizer), DisplayMode.Zoom);
    public DisplayMode DisplayMode
    {
        get => (DisplayMode)GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
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

    class DtwDrawable : IDrawable
    {
        private readonly DtwVisualizer dtwVisualizer;
        double padding = 25;

        public DtwDrawable(DtwVisualizer dtwVisualizer)
        {
            this.dtwVisualizer = dtwVisualizer;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = dtwVisualizer.BackgroundColor;
            canvas.FillRectangle(dirtyRect);

            if (dtwVisualizer.FirstSignature is null) return;


            (var transformMatrix, var scale) = CalculateTransformation(dirtyRect, dtwVisualizer.Offset);

            if (dtwVisualizer.ShowAxes)
                DrawAxes(canvas, transformMatrix, scale);

            DrawFeatureFunction(canvas, dtwVisualizer.FirstSignature, transformMatrix, scale);

            transformMatrix.Translate(0, (dirtyRect.Height - padding) / 2);
            DrawFeatureFunction(canvas, dtwVisualizer.SecondSignature, transformMatrix, scale);
        }

        private (Matrix, double) CalculateTransformation(RectF dirtyRect, Point panOffset)
        {
            var firstSignature = dtwVisualizer.FirstSignature;
            var ftt = firstSignature.GetFeature(Features.T);
            var fft = firstSignature.GetFeature(Features.Y);
            double ftRange = ftt.Max() - ftt.Min();
            double ffRange = fft.Max() - fft.Min();

            var secondSignature = dtwVisualizer.SecondSignature;
            var stt = secondSignature.GetFeature(Features.T);
            var sft = secondSignature.GetFeature(Features.Y);
            double stRange = stt.Max() - stt.Min();
            double sfRange = sft.Max() - sft.Min();

            double tRange = Math.Max(ftRange, stRange);
            double fRange = Math.Max(ffRange, sfRange);

            double width = dirtyRect.Width - 2 * padding;
            double height = dirtyRect.Height - 3 * padding;
            double scale = Math.Sqrt((width / ftRange) * (height / ffRange));

            var matrix = new Matrix();
            matrix.Scale(width / ftRange, height / 2 / ffRange);
            matrix.Translate(padding, padding);
            matrix.Translate(panOffset.X, panOffset.Y);

            return (matrix, scale);
        }

        private void DrawFeatureFunction(ICanvas canvas, Signature signature, Matrix transformMatrix, double scale)
        {
            if (signature == null) return;
            var strokes = signature.GetStrokes();
            var tt = signature.GetFeature(Features.T);
            var ft = signature.GetFeature(Features.Y);

            var originM = new Matrix();
            originM.Translate(-tt.Min(), -ft.Max());
            originM.Scale(1, -1);
            originM.Append(transformMatrix);

            canvas.StrokeSize = (float)Math.Max(1, 20 * scale);
            canvas.StrokeLineJoin = LineJoin.Round;

            foreach (var stroke in strokes)
            {
                canvas.StrokeColor = stroke.StrokeType == StrokeType.Down ? Colors.Blue : Colors.Red;

                var polyline = new PathF();
                var points = tt.Zip(ft, (x, y) => new Point(x, y));

                foreach (var point in points)
                    polyline.LineTo(originM.Transform(point));

                canvas.DrawPath(polyline);
            }
        }

        private static void DrawAxes(ICanvas canvas, Matrix matrix, double scale)
        {
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = (float)Math.Max(1, 10 * scale);
            canvas.StrokeLineCap = LineCap.Square;

            canvas.DrawLine(
                matrix.Transform(new Point(0, -100)),
                matrix.Transform(new Point(0, +1000))
            );

            canvas.DrawLine(
                matrix.Transform(new Point(-100, 0)),
                matrix.Transform(new Point(+1000, 0))
            );
        }
    }
}
