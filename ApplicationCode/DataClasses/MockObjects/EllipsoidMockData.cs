using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.DataClasses.MockObjects;

public class EllipsoidMockData : AMockObject
{
	private int a;
	private int b;
	private int c;

    private int[] measures;
    private double[] spacings;

    public override int[] Measures { get => measures; }

    public override double XSpacing { get => spacings[0]; }
    public override double YSpacing { get => spacings[1]; }
    public override double ZSpacing { get => spacings[2]; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="Measures"></param>
    /// <param name="Spacings"></param>
    public EllipsoidMockData(int a, int b, int c, int[] Measures, double[] Spacings)
	{
		this.a = a;
		this.b = b;
		this.c = c;

        this.measures = Measures;
        this.spacings = Spacings;
    }

    /// <summary>
    /// Checks whether all values in array are more than 0. Made for check of measures and sizing properties.
    /// </summary>
    /// <param name="CheckedArray">Array thats going to be checked.</param>
    /// <returns>Returns false if at least one value is 0 or bellow.</returns>
    private bool CheckArrayPositiveValues(int[] CheckedArray)
    {
        for (int i = 0; i<CheckedArray.Length; i++)
        {
            if (CheckedArray[i] <= 0)
                return false;
        }

        return true;
    }

    public override double GetValue(double x, double y, double z)
    {
        double currentValue = (x * x) / (a * a) + (y * y) / (b * b) + (z * z) / (c * c);
        if (currentValue <= 1)
            return currentValue * 4000;

        return 5000;
    }

    public override double GetValue(Point3D p)
    {
        return GetValue(p.X, p.Y, p.Z);
    }
}

