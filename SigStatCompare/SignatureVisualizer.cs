using Microsoft.Maui.Controls.Shapes;
using SigStat.Common;

namespace SigStatCompare;

public class SignatureVisualizer : GraphicsView
{
    public static readonly BindableProperty SignatureProperty =
        BindableProperty.Create(nameof(Signature), typeof(Signature), typeof(SignatureVisualizer), null, propertyChanged: SignatureChanged);
    public Signature Signature
    {
        get => (Signature)GetValue(SignatureProperty);
        set => SetValue(SignatureProperty, value);
    }

    private static void SignatureChanged(BindableObject bindableObject, object oldValue, object newValue)
    {
        var visualizer = bindableObject as SignatureVisualizer;
        visualizer.Zoom = 1.0;
        visualizer?.Invalidate();
    }

    public static readonly BindableProperty ShowAxesProperty =
        BindableProperty.Create(nameof(ShowAxes), typeof(bool), typeof(SignatureVisualizer), true, propertyChanged: ShowAxesChanged);
    public bool ShowAxes
    {
        get => (bool)GetValue(ShowAxesProperty);
        set => SetValue(ShowAxesProperty, value);
    }

    private static void ShowAxesChanged(BindableObject bindableObject, object oldValue, object newValue)
    {
        var visualizer = bindableObject as SignatureVisualizer;
        visualizer?.Invalidate();
    }

    public static readonly BindableProperty InteractiveProperty =
        BindableProperty.Create(nameof(Interactive), typeof(bool), typeof(SignatureVisualizer), true);
    public bool Interactive
    {
        get => (bool)GetValue(InteractiveProperty);
        set => SetValue(InteractiveProperty, value);
    }

    private Point lastOffset;

    public static readonly BindableProperty OffsetProperty =
        BindableProperty.Create(nameof(Offset), typeof(Point), typeof(SignatureVisualizer), propertyChanged: OffsetChanged);
    public Point Offset
    {
        get => (Point)GetValue(OffsetProperty);
        set => SetValue(OffsetProperty, value);
    }

    private static void OffsetChanged(BindableObject bindableObject, object oldValue, object newValue)
    {
        var visualizer = bindableObject as SignatureVisualizer;
        visualizer?.Invalidate();
    }

    public static readonly BindableProperty ZoomProperty =
        BindableProperty.Create(nameof(Zoom), typeof(double), typeof(SignatureVisualizer), defaultValue: 1.0, propertyChanged: ZoomChanged);
    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    private static void ZoomChanged(BindableObject bindableObject, object oldValue, object newValue)
    {
        var visualizer = bindableObject as SignatureVisualizer;
        visualizer?.Invalidate();
    }

    public SignatureVisualizer()
    {
        Drawable = new SignatureDrawable(this);

        // Set PanGestureRecognizer.TouchPoints to control the
        // number of touch points needed to pan
        PanGestureRecognizer panGesture = new PanGestureRecognizer();
        panGesture.PanUpdated += OnPanUpdated;
        GestureRecognizers.Add(panGesture);
    }

    void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (!Interactive) return;

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

    class SignatureDrawable : IDrawable
    {
        private readonly SignatureVisualizer signatureVisualizer;

        public SignatureDrawable(SignatureVisualizer signatureVisualizer)
        {
            this.signatureVisualizer = signatureVisualizer;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.FillColor = signatureVisualizer.BackgroundColor;
            canvas.FillRectangle(dirtyRect);

            if (signatureVisualizer.Signature is null) return;


            (var transformMatrix, var scale) = CalculateTransformation(dirtyRect, signatureVisualizer.Offset);

            DrawSignature(canvas, transformMatrix, scale);

            if (signatureVisualizer.ShowAxes)
                DrawAxes(canvas, transformMatrix, scale);
        }

        private (Matrix, double) CalculateTransformation(RectF dirtyRect, Point panOffset)
        {
            var sig = signatureVisualizer.Signature;
            var xt = sig.GetFeature(Features.X);
            var yt = sig.GetFeature(Features.Y);

            double xRange = xt.Max() - xt.Min();
            double yRange = yt.Max() - yt.Min();

            double scale = Math.Min(dirtyRect.Width / xRange, dirtyRect.Height / yRange);

            var matrix = new Matrix();
            matrix.Scale(scale, scale);
            matrix.Translate(
                dirtyRect.Width / 2 - xRange * scale / 2,
                dirtyRect.Height / 2 - yRange * scale / 2
            );
            matrix.Translate(panOffset.X, panOffset.Y);
            matrix.Scale(signatureVisualizer.Zoom, signatureVisualizer.Zoom);

            return (matrix, scale);
        }

        private void DrawSignature(ICanvas canvas, Matrix transformMatrix, double scale)
        {
            var sig = signatureVisualizer.Signature;
            if (sig == null) return;
            var strokes = sig.GetStrokes();
            var xt = sig.GetFeature(Features.X);
            var yt = sig.GetFeature(Features.Y);

            var originM = new Matrix();
            originM.Translate(-xt.Min(), -yt.Max());
            originM.Scale(1, -1);
            originM.Append(transformMatrix);

            canvas.StrokeSize = (float)Math.Max(1, 20 * scale);
            canvas.StrokeLineJoin = LineJoin.Round;

            foreach (var stroke in strokes)
            {
                canvas.StrokeColor = stroke.StrokeType == StrokeType.Down ? Colors.Blue : Colors.Red;

                var polyline = new PathF();

                for (int i = stroke.StartIndex + 1; i <= stroke.EndIndex; i++)
                    polyline.LineTo(originM.Transform(new Point(xt[i], yt[i])));

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
