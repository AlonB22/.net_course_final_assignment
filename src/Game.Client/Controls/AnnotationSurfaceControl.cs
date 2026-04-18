using System.ComponentModel;
using System.Drawing.Drawing2D;
using Game.Client.Replay.Models;

namespace Game.Client.Controls;

public sealed class AnnotationSurfaceControl : Control
{
    private Bitmap? _backingBitmap;
    private Graphics? _bitmapGraphics;
    private Point? _lastPoint;
    private readonly List<Point> _currentStrokePoints = [];
    private bool _readOnly;

    public AnnotationSurfaceControl()
    {
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        ForeColor = Color.Black;
        Margin = new Padding(0);
        Resize += (_, _) => ResizeBackingBitmap();
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
    }

    public event EventHandler<AnnotationStrokeCommittedEventArgs>? StrokeCommitted;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color StrokeColor { get; set; } = Color.FromArgb(34, 84, 173);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float StrokeWidth { get; set; } = 3.5f;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ReadOnly
    {
        get => _readOnly;
        set => _readOnly = value;
    }

    public void ClearCanvas()
    {
        EnsureBitmap();
        if (_bitmapGraphics is null || _backingBitmap is null)
        {
            return;
        }

        _bitmapGraphics.Clear(BackColor);
        Invalidate();
    }

    public void LoadStrokes(IEnumerable<AnnotationStrokeSnapshot> strokes)
    {
        EnsureBitmap();
        if (_bitmapGraphics is null || _backingBitmap is null)
        {
            return;
        }

        _bitmapGraphics.Clear(BackColor);
        foreach (var stroke in strokes.OrderBy(stroke => stroke.TurnIndex).ThenBy(stroke => stroke.StrokeIndex))
        {
            DrawStrokeSnapshot(stroke);
        }

        Invalidate();
    }

    public Bitmap? Snapshot()
    {
        if (_backingBitmap is null)
        {
            return null;
        }

        return (Bitmap)_backingBitmap.Clone();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        EnsureBitmap();

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(BackColor);

        if (_backingBitmap is not null)
        {
            e.Graphics.DrawImageUnscaled(_backingBitmap, 0, 0);
        }

        using var borderPen = new Pen(Color.FromArgb(210, 217, 228), 1);
        e.Graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _bitmapGraphics?.Dispose();
            _backingBitmap?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (_readOnly || e.Button != MouseButtons.Left)
        {
            return;
        }

        EnsureBitmap();
        _lastPoint = e.Location;
        _currentStrokePoints.Clear();
        _currentStrokePoints.Add(e.Location);
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (_readOnly || _lastPoint is null || e.Button != MouseButtons.Left)
        {
            return;
        }

        DrawStroke(_lastPoint.Value, e.Location);
        _lastPoint = e.Location;
        _currentStrokePoints.Add(e.Location);
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (_readOnly || e.Button != MouseButtons.Left)
        {
            return;
        }

        CommitStroke();
        _lastPoint = null;
    }

    private void DrawStroke(Point start, Point end)
    {
        EnsureBitmap();
        if (_bitmapGraphics is null)
        {
            return;
        }

        using var pen = new Pen(StrokeColor, StrokeWidth)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        _bitmapGraphics.SmoothingMode = SmoothingMode.AntiAlias;
        _bitmapGraphics.DrawLine(pen, start, end);
        Invalidate();
    }

    private void DrawStrokeSnapshot(AnnotationStrokeSnapshot stroke)
    {
        if (_bitmapGraphics is null || stroke.Points.Count == 0)
        {
            return;
        }

        using var pen = new Pen(Color.FromArgb(stroke.StrokeColorArgb), stroke.StrokeWidth)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        _bitmapGraphics.SmoothingMode = SmoothingMode.AntiAlias;

        var points = stroke.Points.Select(point => new Point(point.X, point.Y)).ToArray();
        if (points.Length == 1)
        {
            using var fillBrush = new SolidBrush(Color.FromArgb(stroke.StrokeColorArgb));
            _bitmapGraphics.FillEllipse(fillBrush, points[0].X, points[0].Y, stroke.StrokeWidth, stroke.StrokeWidth);
            return;
        }

        _bitmapGraphics.DrawLines(pen, points);
    }

    private void CommitStroke()
    {
        if (_currentStrokePoints.Count < 1)
        {
            return;
        }

        var points = _currentStrokePoints.ToArray();
        StrokeCommitted?.Invoke(this, new AnnotationStrokeCommittedEventArgs(points, StrokeColor, StrokeWidth));
        _currentStrokePoints.Clear();
    }

    private void ResizeBackingBitmap()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        var oldBitmap = _backingBitmap;
        var newBitmap = new Bitmap(Width, Height);
        using var newGraphics = Graphics.FromImage(newBitmap);
        newGraphics.Clear(BackColor);
        if (oldBitmap is not null)
        {
            newGraphics.DrawImageUnscaled(oldBitmap, Point.Empty);
        }

        _bitmapGraphics?.Dispose();
        _backingBitmap?.Dispose();
        _backingBitmap = newBitmap;
        _bitmapGraphics = Graphics.FromImage(_backingBitmap);
        Invalidate();
    }

    private void EnsureBitmap()
    {
        if (_backingBitmap is not null && _bitmapGraphics is not null)
        {
            return;
        }

        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        _backingBitmap = new Bitmap(Width, Height);
        _bitmapGraphics = Graphics.FromImage(_backingBitmap);
        _bitmapGraphics.Clear(BackColor);
    }
}
