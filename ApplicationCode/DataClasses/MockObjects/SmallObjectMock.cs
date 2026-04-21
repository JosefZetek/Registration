using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.DataClasses.MockObjects;

public class SmallMockObject : AMockObject
{
    private int[] measures = new int[] { 2, 2, 2 };

    public override double XSpacing => 1;

    public override double YSpacing => 1;

    public override double ZSpacing => 1;

    public override int[] Measures => measures;

    public override double GetValue(double x, double y, double z)
    {
        return x + 2 * y + 4 * z;
    }

    public override double GetValue(Point3D p)
    {
        return GetValue(p.X, p.Y, p.Z);
    }
}

