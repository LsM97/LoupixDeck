using LoupixDeck.Models;
using LoupixDeck.Utils;
using SkiaSharp;

namespace LoupixDeck.LoupedeckDevice.Device;

/// <summary>
/// Loupedeck Live — original device (VID 2ec2:0004).
/// Has a single 480×270 display split into three virtual regions:
///   left   (X=0,   w=60, h=270) — rotary labels
///   center (X=60,  w=360, h=270) — 4×3 touch grid (keys 0-11)
///   right  (X=420, w=60,  h=270) — rotary labels
///
/// 6 rotary encoders (3 left, 3 right), 8 physical buttons (0-7).
/// Same wire protocol as Loupedeck Live S / Razer Stream Controller.
/// </summary>
public class LoupedeckLiveDevice : LoupedeckDevice
{
    /// <summary>Touch index for the left narrow panel (reserved, not touch-capable on Live).</summary>
    public const int LeftSideIndex = 12;

    /// <summary>Touch index for the right narrow panel (reserved, not touch-capable on Live).</summary>
    public const int RightSideIndex = 13;

    public LoupedeckLiveDevice(string host = null, string path = null, int baudrate = 0,
        bool autoConnect = true, int reconnectInterval = Constants.DefaultReconnectInterval)
        : base(host, path, baudrate, autoConnect, reconnectInterval)
    {
        Buttons = [0, 1, 2, 3, 4, 5, 6, 7];
        Columns = 4;
        Rows = 3;
        RotaryCount = 6;
        // 12 grid slots only (side panels are displays, NOT touch-capable on Live).
        TouchButtonCount = Columns * Rows;
        // Centre 4×3 grid sits between X=60 and X=420 on the unified 480px display.
        VisibleX = [60, 420];
        VisibleY = [0, 270];
        Type = "Loupedeck Live";
        ProductId = "0004";

        // Single unified display on the wire — side regions are drawn at
        // offset X positions on the same "center" buffer (\0M).
        Displays = new Dictionary<string, DisplayInfo>
        {
            ["center"] = new() { Id = "\0M"u8.ToArray(), Width = 480, Height = 270 }
        };
    }

    protected override TouchTarget GetTarget(int x, int y)
    {
        if (VisibleX == null || VisibleY == null)
            throw new InvalidOperationException("VisibleX or VisibleY cannot be null.");

        // Left side panel (display only, not touch-capable on original Live).
        if (x < VisibleX[0])
            return new TouchTarget { Screen = "center", Key = LeftSideIndex };

        // Right side panel (display only, not touch-capable on original Live).
        if (x >= VisibleX[1])
            return new TouchTarget { Screen = "center", Key = RightSideIndex };

        // Centre 4×3 grid — clamp and translate into grid coords.
        x = Math.Clamp(x, VisibleX[0], VisibleX[1] - 1) - VisibleX[0];
        y = Math.Clamp(y, VisibleY[0], VisibleY[1]);
        var column = x / 90;
        var row = y / 90;
        var key = row * Columns + column;
        return new TouchTarget { Screen = "center", Key = key };
    }

    /// <summary>
    /// Draws an arbitrary bitmap to one touch slot — handles the 60×270 side
    /// panels (12/13) by routing to their unified-display X offsets; everything
    /// else falls through to the base 90×90 grid path.
    /// </summary>
    public override async Task DrawTouchSlot(int index, SKBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        if (index == LeftSideIndex || index == RightSideIndex)
        {
            const int sideW = 60;
            const int sideH = 270;
            var destX = index == LeftSideIndex ? 0 : 420;
            try { await DrawCanvasRegion("center", sideW, sideH, bitmap, destX, 0); }
            catch (Exception ex) { Console.WriteLine($"Live side-panel slot draw failed for index {index}: {ex.Message}"); }
            return;
        }
        await base.DrawTouchSlot(index, bitmap);
    }

    /// <summary>
    /// Overrides the base grid renderer so indices 12/13 paint the 60×270 side
    /// panels at their unified-display X offsets. Other indices fall through to
    /// the base 90×90 grid path (which honours Columns/VisibleX from this class).
    /// </summary>
    public override async Task DrawTouchButton(TouchButton touchButton, LoupedeckConfig config, bool refresh, int columns)
    {
        ArgumentNullException.ThrowIfNull(touchButton);

        if (touchButton.Index < Columns * Rows)
        {
            await base.DrawTouchButton(touchButton, config, refresh, columns);
            return;
        }

        if (touchButton.Index != LeftSideIndex && touchButton.Index != RightSideIndex)
            return;

        const int sideW = 60;
        const int sideH = 270;

        if (refresh || touchButton.RenderedImage == null)
        {
            var rendered = BitmapHelper.RenderTouchButtonContent(touchButton, config, sideW, sideH, columns);
            if (rendered == null) return;
        }

        if (touchButton.RenderedImage == null) return;

        try
        {
            var destX = touchButton.Index == LeftSideIndex ? 0 : 420;
            await DrawCanvas("center", sideW, sideH, touchButton.RenderedImage, destX, 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Live side-panel draw failed for index {touchButton.Index}: {ex.Message}");
        }
    }
}
