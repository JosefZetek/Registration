using System;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.Approximation;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.Other.FCConfiguration;

namespace Registration.ApplicationCode.FeatureComputers;

/// <summary>
/// Feature Computer computing the Koenderink shape index and curvedness of the
/// ISO-surface passing through the inspected point.
///
/// The principal curvatures are derived from the Gaussian (K) and mean (H) curvature
/// of the fitted quadric. The shape index S = (2/pi) * atan2(k1 + k2, k1 - k2) is
/// bounded to [-1, 1] and describes the local surface type (cup, rut, saddle, ridge,
/// cap) independently of how strongly it is curved; the curvedness
/// C = sqrt((k1^2 + k2^2) / 2) captures the magnitude. Compared to raw K and H this
/// pair is much better behaved under normalization since K and H have heavy tails.
/// </summary>
public class FeatureComputerShapeIndex : AFeatureComputer
{
    private double borderValue;

    private readonly AApproximationComputer approximationComputer;

    public override int NumberOfFeatures => 2;

    /// <summary>
    /// Feature Computer using its own linear approximation computer.
    /// </summary>
    /// <param name="borderValue">Kernel output at point distanced RADIUS units from inspected point</param>
    /// <param name="radius">Number of units distanced from center at which kernel function drops to 'borderPercentage'</param>
    public FeatureComputerShapeIndex(double borderValue, int radius)
        : this(borderValue, new ApproximationComputerLinear(borderValue))
    {
    }

    /// <summary>
    /// Feature Computer using an externally supplied approximation computer. Sharing the same
    /// instance with other feature computers (e.g. Curvature / Gradient) lets the expensive
    /// approximation be computed once per point and reused.
    /// </summary>
    /// <param name="borderValue">Kernel output at point distanced RADIUS units from inspected point</param>
    /// <param name="approximationComputer">Approximation computer producing the fitted quadric</param>
    public FeatureComputerShapeIndex(double borderValue, AApproximationComputer approximationComputer)
    {
        this.borderValue = borderValue;
        this.approximationComputer = approximationComputer;
    }

    public override void ComputeFeatureVector(AData d, Point3D p, double[] array, int startIndex)
    {
        CheckArrayDimensions(array, startIndex);

        ApproximationResult approximation = approximationComputer.GetApproximation(d, p);

        Vector<double> coeficients = approximation.Coeficients;

        Matrix<double> hessianMatrix = ConstructHessianMatrix(coeficients);
        Vector<double> functionGradient = GetFunctionGradient(p - approximation.CenterPoint, coeficients);
        Matrix<double> adjointHessian = ConstructAdjointMatrix(hessianMatrix);

        double functionGradientNorm = functionGradient.L2Norm();

        /* Vanishing gradient means the iso-surface is undefined here. NaN marks the
           vector as invalid so it gets dropped before normalization. */
        if (functionGradientNorm == 0)
        {
            array[startIndex] = double.NaN;
            array[startIndex + 1] = double.NaN;
            return;
        }

        double gaussianCurvature = adjointHessian.LeftMultiply(functionGradient).DotProduct(functionGradient) / Math.Pow(functionGradientNorm, 4);
        double meanCurvature = (hessianMatrix.LeftMultiply(functionGradient).DotProduct(functionGradient) -
             Math.Pow(functionGradientNorm, 2) * hessianMatrix.Trace()) /
            (2 * Math.Pow(functionGradientNorm, 3));

        /* Principal curvatures k1 >= k2; the discriminant H^2 - K can dip slightly
           below zero numerically, so it is clamped. */
        double discriminant = Math.Sqrt(Math.Max(0, meanCurvature * meanCurvature - gaussianCurvature));
        double k1 = meanCurvature + discriminant;
        double k2 = meanCurvature - discriminant;

        /* Atan2 handles the umbilic case (k1 == k2) and yields +-1 there. */
        double shapeIndex = (2 / Math.PI) * Math.Atan2(k1 + k2, k1 - k2);
        double curvedness = Math.Sqrt((k1 * k1 + k2 * k2) / 2);

        array[startIndex] = shapeIndex;
        array[startIndex + 1] = curvedness;
    }

    public override void GetWeights(double[] array, int startIndex)
    {
        for (int i = 0; i < NumberOfFeatures; i++)
            array[startIndex + i] = 1.0;
    }

    public override FCConfiguration GetConfiguration()
    {
        var config = new FCConfiguration();
        config.FeatureComputerType = FCType.SHAPE_INDEX_FEATURE_COMPUTER;
        config.AddParameter("BorderPercentage", borderValue);
        config.AddParameter("Radius", 0);
        config.AddParameter("Comment", "ShapeIndex");

        return config;
    }

    private Matrix<double> ConstructAdjointMatrix(Matrix<double> hessianMatrix)
    {
        return Matrix<double>.Build.DenseOfArray(new double[,]
        {
            {
                hessianMatrix[1, 1] * hessianMatrix[2, 2] - hessianMatrix[1, 2] * hessianMatrix[2, 1],
                hessianMatrix[1, 2] * hessianMatrix[2, 0] - hessianMatrix[0, 1] * hessianMatrix[2, 2],
                hessianMatrix[1, 0] * hessianMatrix[2, 1] - hessianMatrix[1, 1] * hessianMatrix[2, 0],
            },
            {
                hessianMatrix[0, 2] * hessianMatrix[2, 1] - hessianMatrix[0, 1] * hessianMatrix[2, 2],
                hessianMatrix[0, 0] * hessianMatrix[2, 2] - hessianMatrix[0, 2] * hessianMatrix[2, 0],
                hessianMatrix[0, 1] * hessianMatrix[2, 0] - hessianMatrix[0, 0] * hessianMatrix[2, 1],

            },
            {
                hessianMatrix[0, 1] * hessianMatrix[1, 2] - hessianMatrix[0, 2] * hessianMatrix[1, 1],
                hessianMatrix[1, 0] * hessianMatrix[0, 2] - hessianMatrix[0, 0] * hessianMatrix[1, 2],
                hessianMatrix[0, 0] * hessianMatrix[1, 1] - hessianMatrix[0, 1] * hessianMatrix[1, 0]
            }
        });
    }

    private Matrix<double> ConstructHessianMatrix(Vector<double> coeficients)
    {
        return Matrix<double>.Build.DenseOfArray(new double[,]
        {
            { 2*coeficients[0], coeficients[3], coeficients[4] },
            { coeficients[3], 2*coeficients[1], coeficients[5] },
            { coeficients[4], coeficients[5], 2*coeficients[2] },
        });
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
}
