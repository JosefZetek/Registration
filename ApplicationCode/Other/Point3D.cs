using System;
using MathNet.Numerics.LinearAlgebra;

namespace Registration.ApplicationCode.Other;

/// <summary>
/// Immutable 3D point. A readonly struct so that the huge numbers of points
/// created during sampling and approximation never touch the heap.
/// </summary>
public readonly struct Point3D
{
    /* Coordinates */
    private readonly double x;
    private readonly double y;
    private readonly double z;

    public double X { get => x; }
    public double Y { get => y; }
    public double Z { get => z; }

    /// <summary>
    /// Initializes a point with given [x, y, z] coordinates
    /// </summary>
    /// <param name="x">Coordinate x</param>
    /// <param name="y">Coordinate y</param>
    /// <param name="z">Coordinate z</param>
    public Point3D(double x, double y, double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    /// <summary>
    /// Apply transformation on this point in order: firstly rotate, then translate
    /// </summary>
    /// <param name="transformation">Transformation applied on the point</param>
    /// <returns>Returns transformed point</returns>
    public Point3D ApplyRotationTranslation(Transform3D transformation)
    {
        Point3D resultPoint = Rotate(transformation.RotationMatrix);
        return resultPoint.Translate(transformation.TranslationVector);
    }

    /// <summary>
    /// Apply transformation on this point in order: firstly translate, then rotate
    /// </summary>
    /// <param name="transformation">Transformation applied on the point</param>
    /// <returns>Returns transformed point</returns>
    public Point3D ApplyTranslationRotation(Transform3D transformation)
    {
        Point3D resultPoint = Translate(transformation.TranslationVector);
        return resultPoint.Rotate(transformation.RotationMatrix);
    }

    /// <summary>
    /// Calculates coordinates for point rotated using given rotation matrix
    /// </summary>
    /// <param name="m">Rotation matrix</param>
    /// <returns>Returns new coordinates for the original point</returns>
    public Point3D Rotate(Matrix<double> m)
    {
        if(m.ColumnCount != 3 || m.RowCount != 3)
            throw new ArgumentException("Rotation matrix needs to be 3x3");

        return new Point3D(
            m[0, 0] * x + m[0, 1] * y + m[0, 2] * z,
            m[1, 0] * x + m[1, 1] * y + m[1, 2] * z,
            m[2, 0] * x + m[2, 1] * y + m[2, 2] * z
        );
    }

    public Point3D Translate(Vector<double> t)
    {
        if (t.Count != 3)
            throw new ArgumentException("Translation vector needs to have dimension 3");

        return new Point3D(this.x + t[0], this.y + t[1], this.z + t[2]);
    }

    /// <summary>
    /// Creates a copy of this instance (structs copy by value, kept for API compatibility)
    /// </summary>
    /// <returns>Returns a copy of the point</returns>
    public Point3D Copy()
    {
        return this;
    }

    public Vector<double> ToVector()
    {
        var vector = Vector<double>.Build.Dense(3);

        vector[0] = this.x;
        vector[1] = this.y;
        vector[2] = this.z;

        return vector;
    }

    /// <summary>
    /// ToString method shows basic information about the point
    /// </summary>
    /// <returns>Gives string with X, Y, Z coordinates for the given point</returns>
    public override string ToString()
    {
        return "x:" + Math.Round(X, 2) + " y:" + Math.Round(Y, 2) + " z:" + Math.Round(Z, 2);
    }

    /// <summary>
    /// Calculates the distance between this point and the passed one
    /// </summary>
    /// <param name="differentPoint">Point which is used to calculate the distance</param>
    /// <returns></returns>
    public double Distance(Point3D differentPoint)
    {

        double dx = X - differentPoint.X;
        double dy = Y - differentPoint.Y;
        double dz = Z - differentPoint.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    public static Point3D operator+ (Point3D a, Point3D b)
    {
        return new Point3D(
            a.x + b.x,
            a.y + b.y,
            a.z + b.z
        );
    }

    public static Point3D operator -(Point3D a, Point3D b)
    {
        return new Point3D(
            a.x - b.x,
            a.y - b.y,
            a.z - b.z
        );
    }

    public static Point3D operator *(double a, Point3D b)
    {
        return new Point3D(
            a * b.x,
            a * b.y,
            a * b.z
        );
    }

    public static Point3D operator /(double a, Point3D b)
    {
        if (b.x == 0 || b.y == 0 || b.z == 0)
            throw new DivideByZeroException("Cannot divide by zero in one of the coordinates");

        return new Point3D(
            a / b.x,
            a / b.y,
            a / b.z
        );
    }
}
