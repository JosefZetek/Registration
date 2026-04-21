using System;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.DataClasses.MockObjects;

public class SphereMockData : AMockObject
{
    public override double GetValue(double x, double y, double z)
    {
        return GetValue(new Point3D(x, y, z));
    }

    public override double GetValue(Point3D p)
    {
        double radius = 2.5;
        double distance = p.Distance(new Point3D(radius, radius, radius));

        return Math.Min(distance / radius, 1) * 1000;
    }

    public override double XSpacing => 0.2;

    public override double YSpacing => 0.2;

    public override double ZSpacing => 0.2;

    public override int[] Measures => new int[] { (int) (5 * 1/XSpacing), (int)(5 * 1 / YSpacing), (int)(5 * 1 / ZSpacing) };
}

