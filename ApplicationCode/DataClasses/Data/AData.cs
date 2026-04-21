using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.DataClasses.Data;

public abstract class AData : AMockObject
{
    /* Additional methods */
    public abstract double MinValue { get; }
    public abstract double MaxValue { get; }

    public abstract double GetPercentile(double value);

    /// <summary>
    /// Returns value thats normalized based on MinValue and MaxValue
    /// </summary>
    /// <param name="p">Point where value is calculated and normalized</param>
    /// <returns>Returns normalized value based on min and max values</returns>
    public double GetNormalizedValue(Point3D p)
    {
        return (GetValue(p) - MinValue) / (MaxValue - MinValue);
    }

    public double GetNormalizedValue(double x, double y, double z)
    {
        return (GetValue(x, y, z) - MinValue) / (MaxValue - MinValue);
    }
}
