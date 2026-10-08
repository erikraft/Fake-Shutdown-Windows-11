using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FakeShutdown;

internal sealed class MainForm : Form
{
    private const int AnimationDurationMs = 6500;
    private readonly FakeShutdownSurface surface = new();
    private readonly System.Windows.Forms.Timer animationTimer = new() { Interval = 33 };
    private readonly Stopwatch stopwatch = new();
    private bool suspending;

    public MainForm()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(8, 12, 20);
        FormBorderStyle = FormBorderStyle.None;
        KeyPreview = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        WindowState = FormWindowState.Normal;
        Bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1280, 720);

        surface.Dock = DockStyle.Fill;
        Controls.Add(surface);
        animationTimer.Tick += AnimationTimer_Tick;
        Shown += MainForm_Shown;
        FormClosing += MainForm_FormClosing;
        KeyDown += MainForm_KeyDown;
    }

    private void MainForm_Shown(object? sender, EventArgs e)
    {
        try
        {
            Activate();
            Focus();
            stopwatch.Start();
            animationTimer.Start();
        }
        catch (Exception exception)
        {
            ReportFailure("Não foi possível iniciar a animação.", exception);
        }
    }

    private async void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        int elapsed = (int)stopwatch.ElapsedMilliseconds;
        surface.Progress = Math.Min(1d, (double)elapsed / AnimationDurationMs);
        surface.Invalidate();

        if (elapsed < AnimationDurationMs || suspending)
        {
            return;
        }

        suspending = true;
        animationTimer.Stop();
        surface.Status = "Entrando em suspensão";
        surface.Invalidate();
        await Task.Delay(450);

        if (IsDisposed)
        {
            return;
        }

        try
        {
            bool requested = SetSuspendState(false, false, false);
            if (!requested)
            {
                throw new InvalidOperationException("O Windows recusou a solicitação de suspensão.");
            }
        }
        catch (Exception exception)
        {
            suspending = false;
            ReportFailure("A suspensão não pôde ser iniciada. A tela será fechada.", exception);
        }
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Close();
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        animationTimer.Stop();
        animationTimer.Dispose();
    }

    private void ReportFailure(string message, Exception exception)
    {
        string diagnostic = $"{DateTimeOffset.Now:O} {message} {exception}";
        Debug.WriteLine(diagnostic);
        try
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FakeShutdown");
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "errors.log"), diagnostic + Environment.NewLine);
        }
        catch
        {
        }
        MessageBox.Show(
            $"{message}\n\nDetalhes: {exception.Message}",
            "Fake Shutdown",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
        Close();
    }

    [DllImport("powrprof.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetSuspendState(
        [MarshalAs(UnmanagedType.Bool)] bool hibernate,
        [MarshalAs(UnmanagedType.Bool)] bool forceCritical,
        [MarshalAs(UnmanagedType.Bool)] bool disableWakeEvent);
}

internal sealed class FakeShutdownSurface : Panel
{
    private double progress;
    private string status = "Desligando";

    public double Progress
    {
        get => progress;
        set => progress = Math.Clamp(value, 0d, 1d);
    }

    public string Status
    {
        get => status;
        set => status = value;
    }

    public FakeShutdownSurface()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        TabStop = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics graphics = e.Graphics;
        graphics.Clear(Color.FromArgb(8, 12, 20));
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using Pen gridPen = new(Color.FromArgb(14, 21, 32), 1f);
        int gridSize = Math.Max(48, (int)(64 * DeviceDpi / 96f));
        for (int x = 0; x < Width; x += gridSize)
        {
            graphics.DrawLine(gridPen, x, 0, x, Height);
        }
        for (int y = 0; y < Height; y += gridSize)
        {
            graphics.DrawLine(gridPen, 0, y, Width, y);
        }

        float centerX = Width / 2f;
        float centerY = Height / 2f;
        float scale = Math.Max(1f, DeviceDpi / 96f);
        using Font titleFont = new("Segoe UI", 22f * scale, FontStyle.Regular, GraphicsUnit.Pixel);
        using Font hintFont = new("Segoe UI", 14f * scale, FontStyle.Regular, GraphicsUnit.Pixel);
        using Brush titleBrush = new SolidBrush(Color.FromArgb(242, 245, 250));
        using Brush hintBrush = new SolidBrush(Color.FromArgb(146, 159, 177));

        SizeF titleSize = graphics.MeasureString(status, titleFont);
        graphics.DrawString(status, titleFont, titleBrush, centerX - titleSize.Width / 2f, centerY - 70f * scale);

        float ringRadius = 34f * scale;
        RectangleF ring = new(centerX - ringRadius, centerY - ringRadius + 28f * scale, ringRadius * 2f, ringRadius * 2f);
        using Pen trackPen = new(Color.FromArgb(42, 55, 72), 4f * scale);
        using Pen progressPen = new(Color.FromArgb(108, 179, 255), 4f * scale);
        trackPen.StartCap = trackPen.EndCap = progressPen.StartCap = progressPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
        graphics.DrawArc(trackPen, ring, 0, 360);
        graphics.DrawArc(progressPen, ring, -90, Math.Max(8f, (float)(progress * 360d)));

        string hint = "Pressione Esc para sair";
        SizeF hintSize = graphics.MeasureString(hint, hintFont);
        graphics.DrawString(hint, hintFont, hintBrush, centerX - hintSize.Width / 2f, centerY + 105f * scale);
    }
}