namespace Registration.ApplicationCode.Other;

/// <summary>
/// Point for XY scatter (CSVWriter)
/// </summary>
public class Point2D
{
    public double X { get; }
    public double Y { get; }

    public Point2D()
    {
        this.X = 0;
        this.Y = 0;
    }

    public Point2D(double x, double y)
    {
        this.X = x;
        this.Y = y;
    }
}

