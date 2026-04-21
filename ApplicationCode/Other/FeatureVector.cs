using System;

namespace Registration.ApplicationCode.Other;

/// <summary>
/// 
/// </summary>
public class FeatureVector: IComparable<FeatureVector>
{

    public static int AXIS = 0;
    
    /// <summary>
    /// Point to which the feature vector belongs
    /// </summary>
    private Point3D point;

    /// <summary>
    /// Array of features
    /// </summary>
    private double[] features;

    /// <summary>
    /// 
    /// </summary>
    public FeatureVector()
    {
        new FeatureVector(new Point3D(0, 0, 0), 0, 0, 0, 0, 0);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="x1"></param>
    /// <param name="x2"></param>
    /// <param name="x3"></param>
    /// <param name="x4"></param>
    /// <param name="x5"></param>
    public FeatureVector(Point3D p, double x1, double x2, double x3, double x4, double x5)
    {
        this.Point = p;
        this.features = new double[] { x1, x2, x3, x4, x5 };
    }

    public FeatureVector(Point3D p, double[] features)
    {
        this.Point = p;
        this.features = features;
    }

    /// <summary>
    /// Calculates thee magnitude of a given vector
    /// </summary>
    /// <returns>Returns calculated magnitude</returns>
    public double Magnitude()
    {
        double sum = 0;

        for (int i = 0; i < features.Length; i++)
            sum += this.features[i] * this.features[i];

        return Math.Sqrt(sum);
    }

    /// <summary>
    /// Calculates the difference
    /// </summary>
    /// <param name="fv"></param>
    /// <returns>Distance between the two vectors</returns>
    public double DistTo2(FeatureVector fv)
    {
        double sum = 0;

        if (fv.features.Length != this.features.Length)
        {
            Console.WriteLine("Different number of features");
            throw new Exception("Different number of features");
        }

        
        for (int i = 0; i < this.features.Length; i++)
        {
            double d = this.features[i] - fv.features[i];
            sum += d * d;
        }

        return sum;
    }

    public double DistTo(FeatureVector fv)
    {
        return Math.Sqrt(this.DistTo2(fv));
    }

    internal Point3D Point { get => point; set => point = value; }
    public int GetNumberOfFeatures { get => features.Length; }
    public double[] Features { get => features; }

    

    public override string ToString()
    {
        string returnS = $"Point: {point}\nFeatures:";

        for (int i = 0; i < features.Length; i++)
        {
            returnS += Math.Round(features[i], 2);
            if (i != (this.features.Length - 1))
                returnS += ", ";
        }

        return returnS;
    }

    public int CompareTo(FeatureVector other)
    {
        return this.Features[AXIS].CompareTo(other.Features[AXIS]);
    }
}
