﻿using Microsoft.Maui.Controls.Shapes;
using SigStat.Common;

namespace SigStatCompare.Views;

public partial class SignatureVisualizer : GraphicsView
{
    private double xRange;
    private double yRange;

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

        var sig = visualizer.Signature;
        if (sig != null)
        {
            visualizer.Zoom = 1.0;
            visualizer.Offset = new Point();
            var xt = sig.GetFeature(Features.X);
            var yt = sig.GetFeature(Features.Y);

            visualizer.xRange = xt.Max() - xt.Min();
            visualizer.yRange = yt.Max() - yt.Min();
        }

        visualizer?.Invalidate();
    }

    double SignatureScale => Math.Max(0.01, Math.Min(
                (Width - Padding) / xRange,
                (Height - Padding) / yRange
            ));

    public static readonly BindableProperty ShowAxesProperty =
        BindableProperty.Create(nameof(ShowAxes), typeof(bool), typeof(SignatureVisualizer), false, propertyChanged: ShowAxesChanged);
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

    private Point startOffset;

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

    const double MinZoom = 0.25;
    const double MaxZoom = 100.0;

    public static readonly BindableProperty ZoomProperty =
        BindableProperty.Create(nameof(Zoom), typeof(double), typeof(SignatureVisualizer), defaultValue: 1.0, propertyChanged: ZoomChanged);
    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, Math.Clamp(value, MinZoom, MaxZoom));
    }

    private static void ZoomChanged(BindableObject bindableObject, object oldValue, object newValue)
    {
        var visualizer = bindableObject as SignatureVisualizer;
        visualizer?.Invalidate();
    }

    public static readonly BindableProperty PaddingProperty =
        BindableProperty.Create(nameof(Padding), typeof(double), typeof(SignatureVisualizer), defaultValue: 0.0, propertyChanged: PaddingChanged);
    public double Padding
    {
        get => (double)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    private static void PaddingChanged(BindableObject bindableObject, object oldValue, object newValue)
    {
        var visualizer = bindableObject as SignatureVisualizer;
        visualizer?.Invalidate();
    }

    public SignatureVisualizer()
    {
        Drawable = new SignatureDrawable(this);

        StartInteraction += (object sender, TouchEventArgs e) =>
        {
            var t = e.Touches.First();

            startOffset = new Point(
                t.X / Zoom / SignatureScale - Offset.X,
                t.Y / Zoom / SignatureScale - Offset.Y
            );

            return;
        };
        DragInteraction += (object sender, TouchEventArgs e) =>
        {
            var t = e.Touches.First();

            Offset = new Point(
                t.X / Zoom / SignatureScale - startOffset.X,
                t.Y / Zoom / SignatureScale - startOffset.Y
            );

            return;
        };

        HandlerChanged += OnHandlerChanged;
    }

    partial void OnHandlerChanged(object sender, EventArgs e);

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


            var transformMatrix = CalculateTransformation(dirtyRect, signatureVisualizer.Offset);

            DrawSignature(canvas, transformMatrix);

            if (signatureVisualizer.ShowAxes)
                DrawAxes(canvas, transformMatrix);
        }

        private Matrix CalculateTransformation(RectF dirtyRect, Point offset)
        {
            var matrix = new Matrix();
            matrix.Translate(
                -signatureVisualizer.xRange / 2,
                -signatureVisualizer.yRange / 2
            );
            matrix.Translate(offset.X, offset.Y);
            matrix.Scale(signatureVisualizer.Zoom, signatureVisualizer.Zoom);
            matrix.Scale(signatureVisualizer.SignatureScale, signatureVisualizer.SignatureScale);
            matrix.Translate(
                dirtyRect.Width / 2,
                dirtyRect.Height / 2
            );

            return matrix;
        }

        private void DrawSignature(ICanvas canvas, Matrix transformMatrix)
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

            canvas.StrokeSize = (float)Math.Clamp(20 * signatureVisualizer.Zoom * signatureVisualizer.SignatureScale, 1, 10);
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

        private void DrawAxes(ICanvas canvas, Matrix matrix)
        {
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = (float)Math.Clamp(10 * signatureVisualizer.Zoom * signatureVisualizer.SignatureScale, 1, 10);
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
