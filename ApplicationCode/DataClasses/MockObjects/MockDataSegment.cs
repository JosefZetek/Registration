using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.DataClasses.MockObjects;

public class MockDataSegment : AMockObject
{
    private const int OUT_OF_BOUNDS = 5000;

    private AMockObject referenceObject;
    private Transform3D transformation;

    private int[] measures;
    private double[] spacings;

    public MockDataSegment(AMockObject referenceObject, int[] measures, double[] spacings)
    {
        this.referenceObject = referenceObject;
        this.measures = measures;
        this.spacings = spacings;
        TransformObject(new Transform3D());
    }

    public void TransformObject(Transform3D expectedTransformation)
    {
        this.transformation = expectedTransformation;
    }


    public override double GetValue(double x, double y, double z)
    {
        return GetValue(new Point3D(x, y, z));
    }

    public override double GetValue(Point3D p)
    {
        Point3D currentPoint = p.ApplyRotationTranslation(transformation);

        if (referenceObject.PointWithinBounds(currentPoint))
            return referenceObject.GetValue(currentPoint);

        return OUT_OF_BOUNDS;
    }

    public override double XSpacing => spacings[0];
    public override double YSpacing => spacings[1];
    public override double ZSpacing => spacings[2];

    public override int[] Measures => measures;
}

