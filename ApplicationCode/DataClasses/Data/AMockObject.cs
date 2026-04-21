using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.DataClasses.Data;

public abstract class AMockObject
{
    public abstract double XSpacing { get; }
    public abstract double YSpacing { get; }
    public abstract double ZSpacing { get; }

    private double[] _spacings;

    public double[] Spacings {
        get
        {
            if (_spacings == null)
                _spacings = new double[] { XSpacing, YSpacing, ZSpacing };

            return _spacings;
        }
    }

    public abstract int[] Measures { get; }

    public abstract double GetValue(double x, double y, double z);
    public abstract double GetValue(Point3D p);

    /// <summary>
    /// Method ensures point is within bounds of an object
    /// </summary>
    /// <param name="p">Point to be checked</param>
    /// <returns>Returns true if point is within bounds, false otherwise</returns>
    public bool PointWithinBounds(Point3D p)
    {
        return PointWithinBounds(p.X, p.Y, p.Z);
    }

    /// <summary>
    /// Method ensures point is within bounds of an object
    /// </summary>
    /// <param name="x">Points X coordinate</param>
    /// <param name="y">Points Y coordinate</param>
    /// <param name="z">Points Z coordinate</param>
    /// <returns>Returns true if point is within bounds, false otherwise</returns>
    public bool PointWithinBounds(double x, double y, double z)
    {
        return (
            x <= MaxValueX &&
            y <= MaxValueY &&
            z <= MaxValueZ &&
            x >= 0 &&
            y >= 0 &&
            z >= 0
        );
    }

    /// <summary>
    /// This array contains max coordinates (inclusive) in order [boundX, boundY, boundZ]
    /// </summary>
    public double[] Bounds { get => new double[] { MaxValueX, MaxValueY, MaxValueZ }; }


    private double? _maxValueX;
    private double? _maxValueY;
    private double? _maxValueZ;


    /// <summary>
    /// Max coordinate (inclusive) in X axis
    /// </summary>
    public double MaxValueX
    {
        get
        {
            if (_maxValueX == null)
                _maxValueX = XSpacing * (Measures[0] - 1);
            return _maxValueX.Value;
        }
    }


    /// <summary>
    /// Max coordinate (inclusive) in Y axis
    /// </summary>
    public double MaxValueY
    {
        get
        {
            if (_maxValueY == null)
                _maxValueY = YSpacing * (Measures[1] - 1);
            return _maxValueY.Value;
        }
    }

    /// <summary>
    /// Max coordinate (inclusive) in Z axis
    /// </summary>
    public double MaxValueZ
    {
        get
        {
            if (_maxValueZ == null)
                _maxValueZ = ZSpacing * (Measures[2] - 1);
            return _maxValueZ.Value;
        }
    }
}
