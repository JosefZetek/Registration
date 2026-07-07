namespace Registration.ApplicationCode.Other;

/* Point3D is a readonly struct, so this holds the point by composition instead of inheritance. */
public class PointWithFeatures
{
    public Point3D point;
    public double[] featureVector;

    public double X { get => point.X; }
    public double Y { get => point.Y; }
    public double Z { get => point.Z; }

    public PointWithFeatures(double x, double y , double z, double[] featureVector)
    {
        this.point = new Point3D(x, y, z);
        this.featureVector = featureVector;
    }

    public PointWithFeatures(Point3D point, double[] featureVector)
    {
        this.point = point;
        this.featureVector = featureVector;
    }
}
