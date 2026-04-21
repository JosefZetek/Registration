namespace Registration.ApplicationCode.Other;

public class PointRated: PointWithFeatures
{
    public double rating;
    public PointRated(PointWithFeatures point, double rating) : base(point.X, point.Y, point.Z, point.featureVector)
    {
        this.rating = rating;
    }
}
