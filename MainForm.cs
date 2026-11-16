using System.Drawing;
using System.Drawing.Imaging;

namespace MemeGen;

sealed class MainForm : Form
{
    static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff"];

    readonly PictureBox preview;
    readonly TextBox topBox;
    readonly TextBox bottomBox;
    readonly Button saveButton;
    readonly Button copyButton;
    readonly System.Windows.Forms.Timer renderTimer;

    Image? source;
    Bitmap? rendered;
    string? sourcePath;

    public MainForm(string? initialPath)
    {
        Text = "Meme Generator";
        ClientSize = new Size(1100, 700);
        MinimumSize = new Size(700, 450);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10f);
        AllowDrop = true;

        // --- side panel ---
        var side = new TableLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 300,
            Padding = new Padding(12),
            ColumnCount = 1,
            AutoScroll = true,
        };
        side.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var openButton = MakeButton("Open image…  (Ctrl+O)", (_, _) => OpenWithDialog());
        topBox = MakeTextBox();
        bottomBox = MakeTextBox();
        saveButton = MakeButton("Save as…  (Ctrl+S)", (_, _) => SaveWithDialog());
        copyButton = MakeButton("Copy to clipboard  (Ctrl+Shift+C)", (_, _) => CopyToClipboard());
        var clearButton = MakeButton("Clear text", (_, _) => { topBox.Clear(); bottomBox.Clear(); });

        side.Controls.Add(openButton);
        side.Controls.Add(MakeLabel("Top text"));
        side.Controls.Add(topBox);
        side.Controls.Add(MakeLabel("Bottom text"));
        side.Controls.Add(bottomBox);
        side.Controls.Add(clearButton);
        side.Controls.Add(new Label { Height = 12 });
        side.Controls.Add(saveButton);
        side.Controls.Add(copyButton);
        side.Controls.Add(new Label
        {
            Text = "Tip: drag an image onto the window or paste one with Ctrl+V.",
            AutoSize = true,
            MaximumSize = new Size(270, 0),
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(3, 16, 3, 3),
        });
        foreach (Control c in side.Controls)
            side.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // --- preview ---
        preview = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(32, 32, 36),
            AllowDrop = true,
        };
        preview.Paint += DrawPlaceholder;
        preview.Click += (_, _) => { if (source is null) OpenWithDialog(); };

        Controls.Add(preview);
        Controls.Add(side);

        foreach (var target in new Control[] { this, preview })
        {
            target.DragEnter += OnDragEnter;
            target.DragDrop += OnDragDrop;
        }

        // Debounce rendering so typing stays snappy on large images.
        renderTimer = new System.Windows.Forms.Timer { Interval = 60 };
        renderTimer.Tick += (_, _) => { renderTimer.Stop(); RenderNow(); };
        topBox.TextChanged += (_, _) => ScheduleRender();
        bottomBox.TextChanged += (_, _) => ScheduleRender();

        UpdateButtons();
        if (!string.IsNullOrEmpty(initialPath) && File.Exists(initialPath))
            Shown += (_, _) => LoadFromFile(initialPath);
    }

    // ---------- UI helpers ----------

    static Button MakeButton(string text, EventHandler onClick)
    {
        var b = new Button { Text = text, Dock = DockStyle.Fill, Height = 36, Margin = new Padding(3, 4, 3, 4) };
        b.Click += onClick;
        return b;
    }

    static TextBox MakeTextBox() => new()
    {
        Multiline = true,
        AcceptsReturn = true,
        Dock = DockStyle.Fill,
        Height = 70,
        ScrollBars = ScrollBars.Vertical,
        Font = new Font("Segoe UI", 12f),
    };

    static Label MakeLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Margin = new Padding(3, 12, 3, 2),
        Font = new Font("Segoe UI", 10f, FontStyle.Bold),
    };

    void DrawPlaceholder(object? sender, PaintEventArgs e)
    {
        if (source is not null) return;
        TextRenderer.DrawText(e.Graphics,
            "Drop an image here, paste with Ctrl+V,\nor click to open one",
            new Font("Segoe UI", 14f), preview.ClientRectangle, Color.FromArgb(160, 160, 170),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
    }

    void UpdateButtons()
    {
        saveButton.Enabled = copyButton.Enabled = source is not null;
    }

    // ---------- keyboard ----------

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.Control | Keys.O:
                OpenWithDialog();
                return true;
            case Keys.Control | Keys.S:
                SaveWithDialog();
                return true;
            case Keys.Control | Keys.Shift | Keys.C:
                CopyToClipboard();
                return true;
            case Keys.Control | Keys.V when TryPasteImage():
                return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    bool TryPasteImage()
    {
        try
        {
            if (Clipboard.ContainsImage() && Clipboard.GetImage() is { } img)
            {
                SetSource(new Bitmap(img), null);
                img.Dispose();
                return true;
            }
            if (Clipboard.ContainsFileDropList())
            {
                var file = Clipboard.GetFileDropList().Cast<string>().FirstOrDefault(IsImageFile);
                if (file is not null) { LoadFromFile(file); return true; }
            }
        }
        catch (Exception ex) { ShowError("Couldn't paste image", ex); return true; }
        return false; // let the text box handle normal text paste
    }

    // ---------- drag & drop ----------

    void OnDragEnter(object? sender, DragEventArgs e)
    {
        bool ok = e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Any(IsImageFile);
        e.Effect = ok ? DragDropEffects.Copy : DragDropEffects.None;
    }

    void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files &&
            files.FirstOrDefault(IsImageFile) is { } file)
            LoadFromFile(file);
    }

    static bool IsImageFile(string path) =>
        ImageExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());

    // ---------- loading ----------

    void OpenWithDialog()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Choose an image",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|All files|*.*",
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
            LoadFromFile(dlg.FileName);
    }

    void LoadFromFile(string path)
    {
        try
        {
            // Copy into a fresh bitmap so the file isn't kept locked.
            using var original = Image.FromFile(path);
            var copy = new Bitmap(original);
            ApplyExifOrientation(original, copy);
            SetSource(copy, path);
        }
        catch (Exception ex) { ShowError($"Couldn't open \"{Path.GetFileName(path)}\"", ex); }
    }

    static void ApplyExifOrientation(Image original, Image target)
    {
        const int OrientationId = 0x0112;
        if (!original.PropertyIdList.Contains(OrientationId)) return;
        var value = original.GetPropertyItem(OrientationId)?.Value;
        if (value is null || value.Length < 2) return;

        RotateFlipType? rf = BitConverter.ToUInt16(value, 0) switch
        {
            2 => RotateFlipType.RotateNoneFlipX,
            3 => RotateFlipType.Rotate180FlipNone,
            4 => RotateFlipType.Rotate180FlipX,
            5 => RotateFlipType.Rotate90FlipX,
            6 => RotateFlipType.Rotate90FlipNone,
            7 => RotateFlipType.Rotate270FlipX,
            8 => RotateFlipType.Rotate270FlipNone,
            _ => null,
        };
        if (rf is { } r) target.RotateFlip(r);
    }

    void SetSource(Image img, string? path)
    {
        source?.Dispose();
        source = img;
        sourcePath = path;
        Text = path is null ? "Meme Generator" : $"Meme Generator — {Path.GetFileName(path)}";
        UpdateButtons();
        RenderNow();
        topBox.Focus();
    }

    // ---------- rendering ----------

    void ScheduleRender()
    {
        renderTimer.Stop();
        renderTimer.Start();
    }

    void RenderNow()
    {
        if (source is null) return;
        var next = MemeRenderer.Render(source, topBox.Text, bottomBox.Text);
        var old = rendered;
        rendered = next;
        preview.Image = next;
        old?.Dispose();
    }

    // ---------- output ----------

    void SaveWithDialog()
    {
        if (source is null) return;
        renderTimer.Stop();
        RenderNow();

        var baseName = sourcePath is null ? "meme" : Path.GetFileNameWithoutExtension(sourcePath) + "-meme";
        using var dlg = new SaveFileDialog
        {
            Title = "Save meme",
            Filter = "PNG image|*.png|JPEG image|*.jpg",
            FileName = baseName,
            AddExtension = true,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK || rendered is null) return;

        try
        {
            if (dlg.FilterIndex == 2 || dlg.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                                     || dlg.FileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                SaveJpeg(rendered, dlg.FileName, 92);
            else
                rendered.Save(dlg.FileName, ImageFormat.Png);
        }
        catch (Exception ex) { ShowError("Couldn't save the image", ex); }
    }

    static void SaveJpeg(Image img, string path, long quality)
    {
        var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
        using var ps = new EncoderParameters(1);
        ps.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
        // JPEG has no alpha: flatten onto white first.
        using var flat = new Bitmap(img.Width, img.Height, PixelFormat.Format24bppRgb);
        using (var g = Graphics.FromImage(flat))
        {
            g.Clear(Color.White);
            g.DrawImage(img, 0, 0, img.Width, img.Height);
        }
        flat.Save(path, codec, ps);
    }

    void CopyToClipboard()
    {
        if (source is null) return;
        renderTimer.Stop();
        RenderNow();
        if (rendered is null) return;
        try { Clipboard.SetImage(rendered); }
        catch (Exception ex) { ShowError("Couldn't copy to clipboard", ex); }
    }

    void ShowError(string title, Exception ex) =>
        MessageBox.Show(this, ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            renderTimer.Dispose();
            rendered?.Dispose();
            source?.Dispose();
        }
        base.Dispose(disposing);
    }
}
