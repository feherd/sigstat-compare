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
            
            if (Zoom == MinZoom && relativeZoom < 1) return;
            if (Zoom == MaxZoom && relativeZoom > 1) return;

            var position = currentPoint.Position;
            var relativeMousePosition = new Point(
               position.X - Width / 2,
               position.Y - Height / 2
            );
            var relativeMouseOffset = new Point(
                relativeMousePosition.X / SignatureScale / Zoom,
                relativeMousePosition.Y / SignatureScale / Zoom
            );
            var relativeOffset = new Point(
               (1 - relativeZoom) * relativeMouseOffset.X,
               (1 - relativeZoom) * relativeMouseOffset.Y
            );

            Zoom *= relativeZoom;

            Offset = new Point(
                Offset.X + relativeOffset.X,
                Offset.Y + relativeOffset.Y
            );
        };
    }
}
