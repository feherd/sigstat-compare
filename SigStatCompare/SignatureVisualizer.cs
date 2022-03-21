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
        visualizer?.Invalidate();
    }

    public static readonly BindableProperty DisplayModeProperty =
        BindableProperty.Create(nameof(DisplayMode), typeof(DisplayMode), typeof(SignatureVisualizer), DisplayMode.Zoom);
    public DisplayMode DisplayMode
    {
        get => (DisplayMode)GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
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
}

class SignatureDrawable : IDrawable
{
    private SignatureVisualizer signatureVisualizer;

    public SignatureDrawable(SignatureVisualizer signatureVisualizer)
    {
        this.signatureVisualizer = signatureVisualizer;
    }

    private void DrawAxes(ICanvas canvas, double s, Point offset, double minX, double maxX, double minY, double maxY)
    {
        maxX = Math.Max(Math.Abs(minX), Math.Abs(maxX));
        maxY = Math.Max(Math.Abs(minY), Math.Abs(maxY));
        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = (float)Math.Max(1, 10 * s);
        canvas.StrokeLineCap = LineCap.Square;
        canvas.DrawLine(
            (float)offset.X,
            (float)(-maxY + offset.Y),
            (float)offset.X,
            (float)(maxY + offset.Y)
        );
        canvas.DrawLine(
            (float)(-maxX + offset.X),
            (float)(0 + offset.Y),
            (float)(maxX + offset.X),
            (float)(0 + offset.Y)
        );
    }

    private void DrawSignature(ICanvas canvas, RectF dirtyRect)
    {
        var sig = signatureVisualizer.Signature;
        if (sig == null)
            return;
        var strokes = sig.GetStrokes();
        var xt = sig.GetFeature(Features.X); // .Select(x => x / 100).ToList();
        var yt = sig.GetFeature(Features.Y); // .Select(x => x / 100).ToList();

        double xr = xt.Max() - xt.Min();
        double yr = yt.Max() - yt.Min();

        double s = Math.Min(dirtyRect.Width / xr, dirtyRect.Height / yr);
        var offset = new Point(
            dirtyRect.Width / 2 - (xt.Max() - xt.Min()) * s / 2,
            dirtyRect.Height / 2 - (yt.Max() - yt.Min()) * s / 2
        );

        foreach (var stroke in strokes)
        {
            var polyline = new PathF();
            canvas.StrokeSize = (float)Math.Max(1, 20 * s);
            canvas.StrokeLineJoin = LineJoin.Round;
            canvas.StrokeColor = stroke.StrokeType == StrokeType.Down ? Colors.Blue : Colors.Red;
            for (int i = stroke.StartIndex; i <= stroke.EndIndex; i++)
            {
                polyline.LineTo(new Point(
                    (xt[i] - xt.Min()) * s + offset.X,
                    (yt.Max() - yt[i]) * s + offset.Y
                ));
            }
            canvas.DrawPath(polyline);
        }

        if (signatureVisualizer.ShowAxes)
            DrawAxes(canvas, s, offset, xt.Min() * s, xt.Max() * s, yt.Min() * s, yt.Max() * s);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = signatureVisualizer.BackgroundColor;
        canvas.FillRectangle(dirtyRect);

        canvas.Translate((float)signatureVisualizer.Offset.X, (float)signatureVisualizer.Offset.Y);

        DrawSignature(canvas, dirtyRect);

        canvas.Translate((float)-signatureVisualizer.Offset.X, (float)-signatureVisualizer.Offset.Y);
    }
}
