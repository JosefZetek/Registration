using System;

using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.Other.FCConfiguration;

namespace Registration.ApplicationCode.FeatureComputers;

public abstract class AFeatureComputer
{
    public abstract int NumberOfFeatures { get; }
    public abstract void ComputeFeatureVector(AData d, Point3D p, double[] array, int startIndex);

    public abstract void GetWeights(double[] array, int startIndex);
        
    /// <summary>
    /// Creates an instance of FC Configuration with given parameters
    /// </summary>
    /// <returns>Returns instance of FC Configuration</returns>
    public abstract FCConfiguration GetConfiguration();
        
    protected void CheckArrayDimensions(double[] array, int startIndex)
    {
        if (array.Length < (startIndex + NumberOfFeatures))
            throw new ArgumentException("Invalid array size");
    }

    public FeatureVector ComputeFeatureVector(AData d, Point3D p)
    {
        double[] features = new double[NumberOfFeatures];
        ComputeFeatureVector(d, p, features, 0);

        return new FeatureVector(p, features);
    }
}
