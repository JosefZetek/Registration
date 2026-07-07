using MathNet.Numerics.LinearAlgebra;

using Registration.ApplicationCode.Approximation;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.Other.FCConfiguration;

namespace Registration.ApplicationCode.FeatureComputers;

public class FeatureComputerGradient : AFeatureComputer
{
    private double borderValue;

    private readonly AApproximationComputer approximationComputer;

    public override int NumberOfFeatures => 1;

    public FeatureComputerGradient(double borderValue, int radius)
        : this(borderValue, new ApproximationComputerLinear(borderValue))
    {
    }

    /// <summary>
    /// Feature Computer using an externally supplied approximation computer. Sharing the same
    /// instance with other feature computers (e.g. Curvature) lets the expensive approximation
    /// be computed once per point and reused instead of recomputed.
    /// </summary>
    /// <param name="borderValue">Kernel output at point distanced RADIUS units from inspected point</param>
    /// <param name="approximationComputer">Approximation computer producing the fitted quadric</param>
    public FeatureComputerGradient(double borderValue, AApproximationComputer approximationComputer)
    {
        this.borderValue = borderValue;
        this.approximationComputer = approximationComputer;
    }

    public override void ComputeFeatureVector(AData d, Point3D p, double[] array, int startIndex)
    {
        CheckArrayDimensions(array, startIndex);

        ApproximationResult approximation = approximationComputer.GetApproximation(d, p);

        double gradientNorm = ComputeGradientNorm(p, approximation);

        array[startIndex] = gradientNorm;
    }

    public override void GetWeights(double[] array, int startIndex)
    {
        for (int i = 0; i < NumberOfFeatures; i++)
            array[startIndex + i] = 1.0;
    }

    public override FCConfiguration GetConfiguration()
    {
        var config = new FCConfiguration();
        config.FeatureComputerType = FCType .GRADIENT_FEATURE_COMPUTER;
        config.AddParameter("BorderPercentage", borderValue);
        config.AddParameter("Radius", 0);
        config.AddParameter("Comment", "Gradient");
        return config;
    }

    private Vector<double> GetFunctionGradient(Point3D p, Vector<double> coeficients)
    {
        return Vector<double>.Build.DenseOfArray(new double[]
        {
            2*coeficients[0]*p.X + coeficients[3] * p.Y + coeficients[4] * p.Z + coeficients[6],
            2*coeficients[1]*p.Y + coeficients[3] * p.X + coeficients[5] * p.Z + coeficients[7],
            2*coeficients[2]*p.Z + coeficients[4] * p.X + coeficients[5] * p.Y + coeficients[8]
        });
    }

    private double ComputeGradientNorm(Point3D point, ApproximationResult approximation)
    {
        Vector<double> functionGradient = GetFunctionGradient(point - approximation.CenterPoint, approximation.Coeficients);
        return functionGradient.L2Norm();
    }
}
