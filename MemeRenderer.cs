using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MemeGen;

/// <summary>Draws classic meme captions: white Impact, black outline, top and bottom.</summary>
static class MemeRenderer
{
    static readonly FontFamily Family = PickFamily();

    static FontFamily PickFamily()
    {
        foreach (var name in new[] { "Impact", "Arial Black", "Arial" })
        {
            try { return new FontFamily(name); }
            catch (ArgumentException) { /* not installed, try next */ }
        }
        return FontFamily.GenericSansSerif;
    }

    public static Bitmap Render(Image source, string top, string bottom)
    {
        var bmp = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(source, 0, 0, source.Width, source.Height);

        DrawCaption(g, top, bmp.Width, bmp.Height, atTop: true);
        DrawCaption(g, bottom, bmp.Width, bmp.Height, atTop: false);
        return bmp;
    }

    static void DrawCaption(Graphics g, string text, int w, int h, bool atTop)
    {
        text = text.Trim().ToUpperInvariant();
        if (text.Length == 0) return;

        float margin = Math.Max(4f, Math.Min(w, h) * 0.03f);
        float maxHeight = h * 0.30f; // each caption may use up to 30% of the image height
        float minSize = Math.Max(8f, h * 0.025f);
        float size = Math.Max(12f, Math.Min(w, h) * 0.13f);

        using var fmt = new StringFormat(StringFormat.GenericTypographic)
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Near,
        };
        fmt.FormatFlags &= ~StringFormatFlags.LineLimit;

        GraphicsPath path;
        float stroke, boxW;
        while (true)
        {
            stroke = Math.Max(2f, size * 0.12f);
            boxW = w - 2 * margin - stroke;
            path = new GraphicsPath();
            path.AddString(text, Family, (int)FontStyle.Regular, size,
                new RectangleF(0, 0, boxW, 1_000_000), fmt);
            var b = path.GetBounds();

            bool fits = b.Height <= maxHeight && WordsFit(text, size, boxW, fmt);
            if (fits || size <= minSize) break;

            path.Dispose();
            size *= 0.92f;
        }

        using (path)
        {
            var bounds = path.GetBounds();
            float dx = (w - boxW) / 2f;
            float dy = atTop
                ? margin + stroke / 2f - bounds.Top
                : h - margin - stroke / 2f - bounds.Bottom;
            using (var m = new Matrix())
            {
                m.Translate(dx, dy);
                path.Transform(m);
            }

            using var pen = new Pen(Color.Black, stroke) { LineJoin = LineJoin.Round };
            g.DrawPath(pen, path);
            g.FillPath(Brushes.White, path);
        }
    }

    /// <summary>True if no single word is wider than the box (so wrapping never splits a word).</summary>
    static bool WordsFit(string text, float size, float boxW, StringFormat fmt)
    {
        foreach (var word in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            using var p = new GraphicsPath();
            p.AddString(word, Family, (int)FontStyle.Regular, size, PointF.Empty, fmt);
            if (p.GetBounds().Width > boxW) return false;
        }
        return true;
    }
}
