// =============================================================================
// CoffeeAwake — IconFactory.cs
// Generates flat-design coffee cup icons programmatically via GDI+.
//
// Why GDI+ instead of embedded .ico files?
//   • Zero external assets — the project is truly self-contained.
//   • Icons scale cleanly to any DPI.
//   • The "steam" animation for the active state is trivial to implement.
//
// Memory: every Icon returned is owned by the caller and must be Disposed.
// =============================================================================

namespace CoffeeAwake.UI;

/// <summary>Generates tray icons for the active and inactive states.</summary>
internal static class IconFactory
{
    // Tray icons are typically displayed at 16×16 or 32×32.
    private const int Size = 32;

    // -------------------------------------------------------------------------
    // Palette — flat design, warm coffee tones
    // -------------------------------------------------------------------------
    private static readonly Color ColorCupBody      = Color.FromArgb(0xFF, 0x6F, 0x4E, 0x37); // coffee brown
    private static readonly Color ColorCupHandle    = Color.FromArgb(0xFF, 0x8B, 0x65, 0x4A);
    private static readonly Color ColorLiquidActive = Color.FromArgb(0xFF, 0xD2, 0x99, 0x1F); // golden coffee
    private static readonly Color ColorLiquidEmpty  = Color.FromArgb(0xFF, 0x3A, 0x2A, 0x1A); // dark empty
    private static readonly Color ColorSteam        = Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF); // translucent white
    private static readonly Color ColorSaucer       = Color.FromArgb(0xFF, 0x5C, 0x3D, 0x2A);

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>Creates a coffee cup icon for the given state.</summary>
    /// <param name="isActive">True → full cup with steam; False → empty cup.</param>
    public static Icon Create(bool isActive)
    {
        using var bitmap = new Bitmap(Size, Size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);

        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        if (isActive)
            DrawActiveCup(g);
        else
            DrawInactiveCup(g);

        return BitmapToIcon(bitmap);
    }

    // -------------------------------------------------------------------------
    // Drawing helpers
    // -------------------------------------------------------------------------

    private static void DrawActiveCup(Graphics g)
    {
        // Steam wisps above the cup
        DrawSteam(g);

        // Saucer
        DrawSaucer(g);

        // Cup body
        DrawCupBody(g, ColorLiquidActive);
    }

    private static void DrawInactiveCup(Graphics g)
    {
        // Saucer
        DrawSaucer(g);

        // Cup body (empty / dark)
        DrawCupBody(g, ColorLiquidEmpty);
    }

    private static void DrawSteam(Graphics g)
    {
        using var pen = new Pen(ColorSteam, 1.5f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };

        // Left wisp
        g.DrawBezier(pen,
            new PointF(10, 12), new PointF(8,  9), new PointF(12, 6), new PointF(10, 3));

        // Centre wisp (slightly taller)
        g.DrawBezier(pen,
            new PointF(16, 11), new PointF(14, 7), new PointF(18, 4), new PointF(16, 1));

        // Right wisp
        g.DrawBezier(pen,
            new PointF(22, 12), new PointF(20, 9), new PointF(24, 6), new PointF(22, 3));
    }

    private static void DrawSaucer(Graphics g)
    {
        using var brush = new SolidBrush(ColorSaucer);
        // Saucer is a flat ellipse at the bottom
        g.FillEllipse(brush, 4, 26, 24, 5);
    }

    private static void DrawCupBody(Graphics g, Color liquidColor)
    {
        // Cup trapezoid (wider at top, narrower at bottom)
        PointF[] cup =
        [
            new(7,  14),   // top-left
            new(25, 14),   // top-right
            new(22, 27),   // bottom-right
            new(10, 27),   // bottom-left
        ];

        // Fill cup with liquid colour
        using (var brush = new SolidBrush(liquidColor))
            g.FillPolygon(brush, cup);

        // Cup outline
        using (var pen = new Pen(ColorCupBody, 1.5f))
            g.DrawPolygon(pen, cup);

        // Handle (right side) — small arc
        using var handlePen = new Pen(ColorCupHandle, 2f) { StartCap = System.Drawing.Drawing2D.LineCap.Round, EndCap = System.Drawing.Drawing2D.LineCap.Round };
        g.DrawArc(handlePen, new RectangleF(21, 17, 7, 8), -80, 200);
    }

    // -------------------------------------------------------------------------
    // Bitmap → Icon conversion
    // -------------------------------------------------------------------------

    private static Icon BitmapToIcon(Bitmap bmp)
    {
        // Convert via a MemoryStream so we own the Icon memory.
        using var ms = new MemoryStream();
        WriteIconToStream(bmp, ms);
        ms.Seek(0, SeekOrigin.Begin);
        return new Icon(ms);
    }

    /// <summary>
    /// Writes a minimal single-image .ico file into the stream.
    /// .ico format reference: https://en.wikipedia.org/wiki/ICO_(file_format)
    /// </summary>
    private static void WriteIconToStream(Bitmap bmp, Stream stream)
    {
        using var pngStream = new MemoryStream();
        bmp.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
        var pngBytes = pngStream.ToArray();

        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        // ICO header
        writer.Write((short)0);          // reserved
        writer.Write((short)1);          // type: 1 = icon
        writer.Write((short)1);          // image count

        // Image directory entry
        writer.Write((byte)bmp.Width);   // width  (0 = 256)
        writer.Write((byte)bmp.Height);  // height (0 = 256)
        writer.Write((byte)0);           // colour count
        writer.Write((byte)0);           // reserved
        writer.Write((short)1);          // colour planes
        writer.Write((short)32);         // bits per pixel
        writer.Write(pngBytes.Length);   // data size
        writer.Write(6 + 16);            // data offset = header(6) + dirEntry(16)

        // PNG data
        writer.Write(pngBytes);
    }
}
