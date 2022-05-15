namespace SigStatCompare;

public partial class SignatureVisualizer : GraphicsView
{
    partial void OnHandlerChanged(object sender, EventArgs e)
    {
        var platformView = (sender as SignatureVisualizer).Handler.PlatformView as Microsoft.Maui.Platform.PlatformTouchGraphicsView;
        platformView.PointerWheelChanged += (sender, e) =>
        {
            var currentPoint = e.GetCurrentPoint(sender as Microsoft.Maui.Platform.PlatformTouchGraphicsView);

            int mouseWheelDelta = currentPoint.Properties.MouseWheelDelta;
            var relativeZoom = Math.Exp(mouseWheelDelta / 1000.0);

            var zoomAfterZoom = Math.Clamp(Zoom * relativeZoom, MinZoom, MaxZoom);

            var mousePosition = currentPoint.Position;

            var mouseOffset = new Point(
                (mousePosition.X - Width / 2) / SignatureScale / Zoom,
                (mousePosition.Y - Height / 2) / SignatureScale / Zoom
            );

            var mouseOffsetAfterZoom = new Point(
                (mousePosition.X - Width / 2) / SignatureScale / zoomAfterZoom,
                (mousePosition.Y - Height / 2) / SignatureScale / zoomAfterZoom
            );

            Zoom = zoomAfterZoom;

            Offset = new Point(
                Offset.X - mouseOffset.X + mouseOffsetAfterZoom.X,
                Offset.Y - mouseOffset.Y + mouseOffsetAfterZoom.Y
            );
        };
    }
}
