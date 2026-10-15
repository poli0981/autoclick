using System.Globalization;

namespace AutoClick.ClickTarget;

/// <summary>
/// A plain window with no child controls, so messages posted to the top-level HWND are the
/// ones it handles. Received messages are drawn on the client area and optionally logged.
/// </summary>
internal sealed class TargetForm : Form
{
    private const int WmKeyDown = 0x0100;
    private const int WmKeyUp = 0x0101;
    private const int WmChar = 0x0102;
    private const int WmLButtonDown = 0x0201;
    private const int WmLButtonUp = 0x0202;
    private const int WmLButtonDblClk = 0x0203;
    private const int WmRButtonDown = 0x0204;
    private const int WmRButtonUp = 0x0205;

    private const int MaxLines = 18;

    private readonly string? _logPath;
    private readonly List<string> _lines = [];
    private readonly List<Point> _clicks = [];

    public TargetForm(string title, string? logPath)
    {
        _logPath = logPath;
        Text = title;
        ClientSize = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;
        BackColor = Color.FromArgb(24, 24, 32);
        ForeColor = Color.Gainsboro;
        KeyPreview = true;
    }

    protected override void WndProc(ref Message m)
    {
        var mouse = m.Msg switch
        {
            WmLButtonDown => "LBUTTONDOWN",
            WmLButtonUp => "LBUTTONUP",
            WmLButtonDblClk => "LBUTTONDBLCLK",
            WmRButtonDown => "RBUTTONDOWN",
            WmRButtonUp => "RBUTTONUP",
            _ => null,
        };
        var key = m.Msg switch
        {
            WmKeyDown => "KEYDOWN",
            WmKeyUp => "KEYUP",
            WmChar => "CHAR",
            _ => null,
        };

        if (mouse is not null)
        {
            Record(mouse, m);
        }
        else if (key is not null)
        {
            RecordKey(key, m);
        }

        base.WndProc(ref m);
    }

    private void Record(string name, Message m)
    {
        var lParam = (long)m.LParam;
        var x = (short)(lParam & 0xFFFF);
        var y = (short)((lParam >> 16) & 0xFFFF);
        if (name.EndsWith("DOWN", StringComparison.Ordinal))
        {
            _clicks.Add(new Point(x, y));
        }

        Append($"{name}\t{x}\t{y}\t0\t0");
    }

    private void RecordKey(string name, Message m)
    {
        var vk = (int)(long)m.WParam;
        var scanCode = (int)(((long)m.LParam >> 16) & 0xFF);
        Append($"{name}\t0\t0\t{vk}\t{scanCode}");
    }

    private void Append(string entry)
    {
        var line = $"{DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)}\t{entry}";
        _lines.Add(line);
        if (_lines.Count > MaxLines)
        {
            _lines.RemoveAt(0);
        }

        if (_logPath is not null)
        {
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;

        using var gridPen = new Pen(Color.FromArgb(48, 48, 64));
        using var labelBrush = new SolidBrush(Color.FromArgb(110, 110, 140));
        for (var x = 0; x < ClientSize.Width; x += 50)
        {
            g.DrawLine(gridPen, x, 0, x, ClientSize.Height);
            g.DrawString(x.ToString(CultureInfo.InvariantCulture), Font, labelBrush, x + 2, 2);
        }

        for (var y = 50; y < ClientSize.Height; y += 50)
        {
            g.DrawLine(gridPen, 0, y, ClientSize.Width, y);
            g.DrawString(y.ToString(CultureInfo.InvariantCulture), Font, labelBrush, 2, y + 2);
        }

        using var clickBrush = new SolidBrush(Color.FromArgb(200, 255, 90, 60));
        foreach (var p in _clicks.TakeLast(50))
        {
            g.FillEllipse(clickBrush, p.X - 4, p.Y - 4, 8, 8);
        }

        using var textBrush = new SolidBrush(ForeColor);
        var lineY = 30f;
        foreach (var line in _lines)
        {
            g.DrawString(line, Font, textBrush, 60, lineY);
            lineY += Font.Height + 2;
        }
    }
}
