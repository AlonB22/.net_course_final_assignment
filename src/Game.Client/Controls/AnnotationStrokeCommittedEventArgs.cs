namespace Game.Client.Controls;

public sealed class AnnotationStrokeCommittedEventArgs : EventArgs
{
    public AnnotationStrokeCommittedEventArgs(IReadOnlyList<Point> points, Color strokeColor, float strokeWidth)
    {
        Points = points;
        StrokeColor = strokeColor;
        StrokeWidth = strokeWidth;
    }

    public IReadOnlyList<Point> Points { get; }

    public Color StrokeColor { get; }

    public float StrokeWidth { get; }
}
