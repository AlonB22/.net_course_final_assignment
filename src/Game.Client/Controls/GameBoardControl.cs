using System.ComponentModel;
using System.Drawing.Drawing2D;
using Game.Contracts.Enums;
using Game.Contracts.Models;

namespace Game.Client.Controls;

public sealed class GameBoardControl : Control
{
    private const int BoardRows = 8;
    private const int BoardColumns = 4;

    private readonly Rectangle[,] _cellBounds = new Rectangle[BoardRows, BoardColumns];
    private BoardSnapshotDto? _snapshot;
    private Rectangle? _selectionSourceBounds;
    private Rectangle? _selectionDestinationBounds;
    private Rectangle? _animatedSourceBounds;
    private Rectangle? _animatedDestinationBounds;
    private Guid? _animatedPieceId;
    private PlayerSide? _animatedPieceSide;
    private DateTime _animationStartedUtc;
    private TimeSpan _animationDuration = TimeSpan.FromMilliseconds(650);
    private PlayerSide? _winnerSide;
    private bool _winnerBlinkVisible;
    private bool _readOnly;
    private string _placeholderMessage = "Board will render here.";

    public GameBoardControl()
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(13, 18, 30);
        ForeColor = Color.WhiteSmoke;
        Dock = DockStyle.Fill;
        Margin = new Padding(0);
        Size = new Size(700, 560);
        Resize += (_, _) => RecalculateCellBounds();
        MouseClick += OnMouseClick;
    }

    public event EventHandler<BoardCellClickedEventArgs>? CellClicked;

    public Rectangle[,] CellBounds => _cellBounds;

    public bool IsAnimating => _animatedPieceId.HasValue;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ReadOnly
    {
        get => _readOnly;
        set => _readOnly = value;
    }

    public void SetPlaceholderMessage(string message)
    {
        _placeholderMessage = message;
        Invalidate();
    }

    public void SetSnapshot(BoardSnapshotDto? snapshot)
    {
        _snapshot = snapshot;
        Invalidate();
    }

    public void SetSelection(Rectangle? source, Rectangle? destination)
    {
        _selectionSourceBounds = source;
        _selectionDestinationBounds = destination;
        Invalidate();
    }

    public void ClearSelection()
    {
        SetSelection(null, null);
    }

    public void BeginPieceAnimation(BoardSnapshotDto snapshot, MoveDescriptorDto move, TimeSpan duration)
    {
        _snapshot = snapshot;
        _animatedPieceId = move.PieceId;
        _animatedPieceSide = move.Side;
        _animatedSourceBounds = TryGetCellRectangle(move.FromRow, move.FromColumn, out var fromBounds) ? fromBounds : Rectangle.Empty;
        _animatedDestinationBounds = TryGetCellRectangle(move.ToRow, move.ToColumn, out var toBounds) ? toBounds : Rectangle.Empty;
        _animationStartedUtc = DateTime.UtcNow;
        _animationDuration = duration <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(650) : duration;
        Invalidate();
    }

    public void BeginPieceAnimation(Rectangle from, Rectangle to, TimeSpan duration)
    {
        _animatedPieceId = Guid.Empty;
        _animatedPieceSide = null;
        _animatedSourceBounds = from;
        _animatedDestinationBounds = to;
        _animationStartedUtc = DateTime.UtcNow;
        _animationDuration = duration <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(650) : duration;
        Invalidate();
    }

    public void EndPieceAnimation()
    {
        _animatedSourceBounds = null;
        _animatedDestinationBounds = null;
        _animatedPieceId = null;
        _animatedPieceSide = null;
        Invalidate();
    }

    public void AdvanceAnimationFrame(int frameIndex)
    {
        if (!IsAnimating)
        {
            return;
        }

        if (DateTime.UtcNow - _animationStartedUtc >= _animationDuration)
        {
            EndPieceAnimation();
            return;
        }

        Invalidate();
    }

    public void SetWinnerBlink(PlayerSide? side, bool visible)
    {
        _winnerSide = side;
        _winnerBlinkVisible = visible;
        Invalidate();
    }

    public void ToggleWinnerBlink(bool visible)
    {
        _winnerBlinkVisible = visible;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.Clear(BackColor);

        if (_cellBounds[0, 0].Width == 0 || _cellBounds[0, 0].Height == 0)
        {
            RecalculateCellBounds();
        }

        var bounds = GetBoardBounds();
        DrawBoardBackground(e.Graphics, bounds);
        DrawGrid(e.Graphics, bounds);
        DrawSelectionHighlights(e.Graphics);
        DrawPieces(e.Graphics);
        DrawAnimatedPiece(e.Graphics);
        DrawOverlay(e.Graphics, bounds);
    }

    private void RecalculateCellBounds()
    {
        var bounds = GetBoardBounds();
        var cellWidth = bounds.Width / BoardColumns;
        var cellHeight = bounds.Height / BoardRows;

        for (var row = 0; row < BoardRows; row++)
        {
            for (var column = 0; column < BoardColumns; column++)
            {
                _cellBounds[row, column] = new Rectangle(
                    bounds.Left + column * cellWidth,
                    bounds.Top + row * cellHeight,
                    cellWidth,
                    cellHeight);
            }
        }

        Invalidate();
    }

    private Rectangle GetBoardBounds()
    {
        var padding = 18;
        var usableWidth = Math.Max(0, Width - padding * 2);
        var usableHeight = Math.Max(0, Height - padding * 2);
        var targetAspect = BoardColumns / (float)BoardRows;
        var currentAspect = usableWidth / (float)Math.Max(1, usableHeight);

        int boardWidth;
        int boardHeight;
        if (currentAspect > targetAspect)
        {
            boardHeight = usableHeight;
            boardWidth = (int)(boardHeight * targetAspect);
        }
        else
        {
            boardWidth = usableWidth;
            boardHeight = (int)(boardWidth / targetAspect);
        }

        var x = padding + (usableWidth - boardWidth) / 2;
        var y = padding + (usableHeight - boardHeight) / 2;
        return new Rectangle(x, y, Math.Max(1, boardWidth), Math.Max(1, boardHeight));
    }

    private void DrawBoardBackground(Graphics graphics, Rectangle bounds)
    {
        using var backgroundBrush = new SolidBrush(Color.FromArgb(20, 29, 49));
        using var borderPen = new Pen(Color.FromArgb(95, 112, 152), 2);
        using var gridPen = new Pen(Color.FromArgb(38, 48, 75), 1);

        graphics.FillRoundedRectangle(backgroundBrush, bounds, 18);
        graphics.DrawRoundedRectangle(borderPen, bounds, 18);

        for (var row = 0; row < BoardRows; row++)
        {
            for (var column = 0; column < BoardColumns; column++)
            {
                var rect = _cellBounds[row, column];
                if (rect.Width == 0 || rect.Height == 0)
                {
                    continue;
                }

                var fillColor = (row + column) % 2 == 0
                    ? Color.FromArgb(42, 56, 85)
                    : Color.FromArgb(29, 40, 63);
                using var cellBrush = new SolidBrush(fillColor);
                graphics.FillRectangle(cellBrush, rect);
                graphics.DrawRectangle(gridPen, rect);
            }
        }
    }

    private void DrawGrid(Graphics graphics, Rectangle bounds)
    {
        using var borderPen = new Pen(Color.FromArgb(118, 135, 173), 1);
        for (var row = 0; row < BoardRows; row++)
        {
            for (var column = 0; column < BoardColumns; column++)
            {
                var rect = _cellBounds[row, column];
                if (rect.Width == 0 || rect.Height == 0)
                {
                    continue;
                }

                graphics.DrawRectangle(borderPen, rect);
            }
        }
    }

    private void DrawSelectionHighlights(Graphics graphics)
    {
        using var sourceBrush = new SolidBrush(Color.FromArgb(95, 88, 168, 255));
        using var destinationBrush = new SolidBrush(Color.FromArgb(95, 255, 208, 90));

        if (_selectionSourceBounds is { } source)
        {
            graphics.FillEllipse(sourceBrush, Rectangle.Inflate(source, -8, -8));
        }

        if (_selectionDestinationBounds is { } destination)
        {
            graphics.FillEllipse(destinationBrush, Rectangle.Inflate(destination, -8, -8));
        }
    }

    private void DrawPieces(Graphics graphics)
    {
        if (_snapshot is null)
        {
            DrawPlaceholder(graphics);
            return;
        }

        var animatedMoveId = _animatedPieceId;
        foreach (var piece in _snapshot.Pieces)
        {
            if (piece.IsCaptured)
            {
                continue;
            }

            if (animatedMoveId.HasValue && piece.PieceId == animatedMoveId.Value)
            {
                continue;
            }

            if (!TryGetCellRectangle(piece.Row, piece.Column, out var cell))
            {
                continue;
            }

            DrawPiece(graphics, cell, piece.Side, piece.HasUsedBackwardMove, false);
        }
    }

    private void DrawAnimatedPiece(Graphics graphics)
    {
        if (_snapshot is null || _animatedSourceBounds is null || _animatedDestinationBounds is null || !_animatedPieceId.HasValue)
        {
            return;
        }

        var progress = Math.Clamp((DateTime.UtcNow - _animationStartedUtc).TotalMilliseconds / Math.Max(1.0, _animationDuration.TotalMilliseconds), 0.0, 1.0);
        var source = _animatedSourceBounds.Value;
        var destination = _animatedDestinationBounds.Value;
        var current = Interpolate(source, destination, progress);

        var piece = _snapshot.Pieces.FirstOrDefault(candidate => candidate.PieceId == _animatedPieceId.Value);
        var side = piece?.Side ?? _animatedPieceSide ?? PlayerSide.Human;
        var hasBackwardMove = piece?.HasUsedBackwardMove ?? false;
        DrawPiece(graphics, current, side, hasBackwardMove, true);

        if (progress >= 1.0)
        {
            EndPieceAnimation();
        }
    }

    private void DrawPiece(Graphics graphics, Rectangle cell, PlayerSide side, bool hasUsedBackwardMove, bool isAnimated)
    {
        var inset = isAnimated ? 8 : 10;
        var ellipse = Rectangle.Inflate(cell, -inset, -inset);
        var winningSide = _winnerBlinkVisible ? _winnerSide : null;
        var isWinningPiece = winningSide.HasValue && side == winningSide.Value && _winnerBlinkVisible;
        var fillColor = isWinningPiece
            ? Color.FromArgb(94, 224, 114)
            : side == PlayerSide.Human
                ? Color.FromArgb(241, 202, 79)
                : Color.FromArgb(108, 187, 255);

        using var shadowBrush = new SolidBrush(Color.FromArgb(80, 0, 0, 0));
        using var fillBrush = new SolidBrush(fillColor);
        using var borderPen = new Pen(Color.WhiteSmoke, isAnimated ? 3 : 2);
        using var backwardMovePen = new Pen(Color.FromArgb(40, 30, 30), 2);
        using var pathPen = new Pen(Color.FromArgb(180, 255, 255, 255), 2)
        {
            DashStyle = DashStyle.Dash
        };

        graphics.FillEllipse(shadowBrush, Rectangle.Inflate(ellipse, 4, 4));
        graphics.FillEllipse(fillBrush, ellipse);
        graphics.DrawEllipse(borderPen, ellipse);

        if (hasUsedBackwardMove)
        {
            graphics.DrawEllipse(backwardMovePen, Rectangle.Inflate(ellipse, -6, -6));
        }

        if (isAnimated && _animatedSourceBounds is not null && _animatedDestinationBounds is not null)
        {
            graphics.DrawLine(pathPen, _animatedSourceBounds.Value.Location, _animatedDestinationBounds.Value.Location);
        }
    }

    private void DrawPlaceholder(Graphics graphics)
    {
        var bounds = GetBoardBounds();
        using var font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
        using var brush = new SolidBrush(Color.FromArgb(208, 215, 229));
        var messageSize = graphics.MeasureString(_placeholderMessage, font);
        graphics.DrawString(
            _placeholderMessage,
            font,
            brush,
            bounds.Left + (bounds.Width - messageSize.Width) / 2,
            bounds.Top + bounds.Height + 12);
    }

    private void DrawOverlay(Graphics graphics, Rectangle bounds)
    {
        if (_winnerBlinkVisible && _winnerSide.HasValue)
        {
            using var blinkBrush = new SolidBrush(Color.FromArgb(26, 106, 224, 94));
            graphics.FillRoundedRectangle(blinkBrush, bounds, 18);
        }

        using var labelFont = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
        using var labelBrush = new SolidBrush(Color.FromArgb(205, 211, 224));

        for (var row = 0; row < BoardRows; row++)
        {
            var leftLabel = graphics.MeasureString(row.ToString(), labelFont);
            var cell = _cellBounds[row, 0];
            graphics.DrawString(
                row.ToString(),
                labelFont,
                labelBrush,
                bounds.Left - 16,
                cell.Top + (cell.Height - leftLabel.Height) / 2);
        }

        for (var column = 0; column < BoardColumns; column++)
        {
            var columnLabel = graphics.MeasureString(((char)('A' + column)).ToString(), labelFont);
            var cell = _cellBounds[0, column];
            graphics.DrawString(
                ((char)('A' + column)).ToString(),
                labelFont,
                labelBrush,
                cell.Left + (cell.Width - columnLabel.Width) / 2,
                bounds.Bottom + 4);
        }
    }

    private bool TryGetCellRectangle(int row, int column, out Rectangle rectangle)
    {
        if (row >= 0 && row < BoardRows && column >= 0 && column < BoardColumns)
        {
            rectangle = _cellBounds[row, column];
            return rectangle.Width > 0 && rectangle.Height > 0;
        }

        rectangle = Rectangle.Empty;
        return false;
    }

    private void OnMouseClick(object? sender, MouseEventArgs e)
    {
        if (_readOnly)
        {
            return;
        }

        for (var row = 0; row < BoardRows; row++)
        {
            for (var column = 0; column < BoardColumns; column++)
            {
                if (_cellBounds[row, column].Contains(e.Location))
                {
                    CellClicked?.Invoke(this, new BoardCellClickedEventArgs(row, column));
                    return;
                }
            }
        }
    }

    private static Rectangle Interpolate(Rectangle from, Rectangle to, double progress)
    {
        var left = (int)Math.Round(from.Left + ((to.Left - from.Left) * progress));
        var top = (int)Math.Round(from.Top + ((to.Top - from.Top) * progress));
        var width = (int)Math.Round(from.Width + ((to.Width - from.Width) * progress));
        var height = (int)Math.Round(from.Height + ((to.Height - from.Height) * progress));
        return new Rectangle(left, top, Math.Max(1, width), Math.Max(1, height));
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using var path = CreateRoundedRectanglePath(bounds, radius);
        graphics.FillPath(brush, path);
    }

    public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
    {
        using var path = CreateRoundedRectanglePath(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
