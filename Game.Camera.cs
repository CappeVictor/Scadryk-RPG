using System;
using System.Drawing;

public sealed partial class Game
{
    // Coordinates are measured in the same 960 x 640 space as the HUD.
    // Larger regions can override these dimensions without changing input or drawing.
    private SizeF WorldSize() => room == 43 ? FirstEchoSize : room == 46 ? SaltBasinSize : room == 49 ? SaltFrontierSize : new(960, 640);

    private PointF CameraOffset()
    {
        SizeF size = WorldSize();
        return new PointF(
            Math.Clamp(player.X - 480, 0, Math.Max(0, size.Width - 960)),
            Math.Clamp(player.Y - 320, 0, Math.Max(0, size.Height - 640)));
    }

    private PointF ScreenToWorld(PointF point)
    {
        PointF gamePoint = ScreenToGame(point, ClientSize);
        PointF camera = CameraOffset();
        return new PointF(gamePoint.X + camera.X, gamePoint.Y + camera.Y);
    }
}
