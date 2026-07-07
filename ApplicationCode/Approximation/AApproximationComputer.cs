using System;
using System.Threading;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Approximation;

/// <summary>
/// Computes the local quadric approximation of the volume around a point.
///
/// This used to live inside each feature computer (Curvature / Gradient) as
/// <c>GetApproximationEquation</c>.
/// The two weighting strategies (linear / elliptical) only differ in
/// <see cref="GetSamplingWeight"/>, implemented by the concrete subclasses.
/// </summary>
public abstract class AApproximationComputer
{
    private const int NUMBER_OF_VARIABLES = 10;

    private readonly double borderValue;

    /// <summary>
    /// Per-thread, single-slot cache. Feature computation runs on several threads
    /// that share the same computer instance, so the cache must be thread-local;
    /// within one thread the points are processed one at a time through all feature
    /// computers, so remembering just the last result is enough to eliminate the
    /// duplicate fit.
    /// </summary>
    private readonly ThreadLocal<CacheEntry> cache = new ThreadLocal<CacheEntry>();

    protected AApproximationComputer(double borderValue)
    {
        this.borderValue = borderValue;
    }

    /// <summary>
    /// Returns the quadric approximation of the volume around <paramref name="p"/>.
    /// The result is cached per thread, so repeated calls with the same data and
    /// point (as happens across feature computers) reuse the previous computation.
    /// </summary>
    public ApproximationResult GetApproximation(AData d, Point3D p)
    {
        CacheEntry entry = cache.Value;
        if (entry != null && ReferenceEquals(entry.Data, d)
            && entry.Point.X == p.X && entry.Point.Y == p.Y && entry.Point.Z == p.Z)
            return entry.Result;

        ApproximationResult result = ComputeApproximation(d, p);
        cache.Value = new CacheEntry(d, p, result);
        return result;
    }

    private ApproximationResult ComputeApproximation(AData d, Point3D p)
    {
        Spacing spacing = ComputeSpacing(d);
        double spreadParameter = CalculateSpreadParameter();

        Point3D centerPoint = new Point3D(
            RoundToNearestSpacingMultiplier(p.X, d.XSpacing),
            RoundToNearestSpacingMultiplier(p.Y, d.YSpacing),
            RoundToNearestSpacingMultiplier(p.Z, d.ZSpacing)
        );

        Vector<double> coeficients = GetApproximationEquation(p, centerPoint, d, spreadParameter, spacing);

        return new ApproximationResult(coeficients, centerPoint);
    }

    /// <summary>
    /// Weight of a surrounding sample. This is the only part that differs between the
    /// approximation strategies, so it is left for the concrete subclasses.
    /// </summary>
    protected abstract double GetSamplingWeight(Point3D surroundingPoint, Spacing spacing);

    private Spacing ComputeSpacing(AData d)
    {
        // Ensure the spacing is at least as large as the proximity spacing to guarantee
        // that there are no interpolated points but the points are still reasonably spaced.
        double spacingX = d.XSpacing < Constants.PROXIMITY_SPACING ? Math.Round(Constants.PROXIMITY_SPACING / d.XSpacing) * d.XSpacing : d.XSpacing;
        double spacingY = d.YSpacing < Constants.PROXIMITY_SPACING ? Math.Round(Constants.PROXIMITY_SPACING / d.YSpacing) * d.YSpacing : d.YSpacing;
        double spacingZ = d.ZSpacing < Constants.PROXIMITY_SPACING ? Math.Round(Constants.PROXIMITY_SPACING / d.ZSpacing) * d.ZSpacing : d.ZSpacing;

        return new Spacing(spacingX, spacingY, spacingZ);
    }

    private double CalculateSpreadParameter()
    {
        return -Math.Log(borderValue) / (Constants.PROXIMITY_RADIUS * Constants.PROXIMITY_RADIUS);
    }

    private Vector<double> GetApproximationEquation(Point3D referencePoint, Point3D centerPoint, AData d, double spreadParameter, Spacing spacing)
    {
        /* The least-squares system (AᵀWA)x = AᵀWv is accumulated directly into a
           10x10 matrix and a 10-vector while walking the neighborhood, instead of
           materializing the N x 10 sample matrices (N can reach hundreds of
           thousands of samples for large proximity radii). */
        double[,] normalMatrix = new double[NUMBER_OF_VARIABLES, NUMBER_OF_VARIABLES];
        double[] rightSide = new double[NUMBER_OF_VARIABLES];
        double[] q = new double[NUMBER_OF_VARIABLES];

        double minValue = double.MaxValue, maxValue = double.MinValue;
        double radiusSquared = Constants.PROXIMITY_RADIUS * Constants.PROXIMITY_RADIUS;

        int desiredShiftX = (int)Math.Ceiling(Constants.PROXIMITY_RADIUS / spacing.X);
        int desiredShiftY = (int)Math.Ceiling(Constants.PROXIMITY_RADIUS / spacing.Y);
        int desiredShiftZ = (int)Math.Ceiling(Constants.PROXIMITY_RADIUS / spacing.Z);

        int minSpacingMultiplierX = MinSpacingMulitplier(centerPoint.X, spacing.X, desiredShiftX), maxSpacingMultiplierX = MaxSpacingMultiplier(centerPoint.X, d.MaxValueX, spacing.X, desiredShiftX);
        int minSpacingMulitplierY = MinSpacingMulitplier(centerPoint.Y, spacing.Y, desiredShiftY), maxSpacingMultiplierY = MaxSpacingMultiplier(centerPoint.Y, d.MaxValueY, spacing.Y, desiredShiftY);
        int minSpacingMulitplierZ = MinSpacingMulitplier(centerPoint.Z, spacing.Z, desiredShiftZ), maxSpacingMultiplierZ = MaxSpacingMultiplier(centerPoint.Z, d.MaxValueZ, spacing.Z, desiredShiftZ);

        Point3D diff = referencePoint - centerPoint;

        for (int x = -minSpacingMultiplierX; x <= maxSpacingMultiplierX; x++)
        {
            for (int y = -minSpacingMulitplierY; y <= maxSpacingMultiplierY; y++)
            {
                for (int z = -minSpacingMulitplierZ; z <= maxSpacingMultiplierZ; z++)
                {
                    double px = x * spacing.X;
                    double py = y * spacing.Y;
                    double pz = z * spacing.Z;

                    /* Restrict sampling to the sphere of PROXIMITY_RADIUS: the cube corners
                       carry a negligible Gaussian weight but make up ~48 % of the samples. */
                    if (px * px + py * py + pz * pz > radiusSquared)
                        continue;

                    Point3D surroundingPoint = new Point3D(px, py, pz);

                    double gaussianWeight = GetGaussianWeight(
                        px - diff.X,
                        py - diff.Y,
                        pz - diff.Z,
                        spreadParameter
                    );

                    double samplingWeight = gaussianWeight * GetSamplingWeight(surroundingPoint, spacing);

                    double value = d.GetValue(surroundingPoint + centerPoint);

                    minValue = Math.Min(minValue, value);
                    maxValue = Math.Max(maxValue, value);

                    q[0] = px * px;
                    q[1] = py * py;
                    q[2] = pz * pz;
                    q[3] = px * py;
                    q[4] = px * pz;
                    q[5] = py * pz;
                    q[6] = px;
                    q[7] = py;
                    q[8] = pz;
                    q[9] = 1;

                    for (int i = 0; i < NUMBER_OF_VARIABLES; i++)
                    {
                        double qiWeighted = q[i] * samplingWeight;
                        rightSide[i] += qiWeighted * value;

                        for (int j = i; j < NUMBER_OF_VARIABLES; j++)
                            normalMatrix[i, j] += qiWeighted * q[j];
                    }
                }
            }
        }

        if (Math.Abs(minValue - maxValue) < 1)
            return Vector<double>.Build.Dense(NUMBER_OF_VARIABLES);

        /* Mirror the accumulated upper triangle (the normal matrix is symmetric) */
        for (int i = 1; i < NUMBER_OF_VARIABLES; i++)
            for (int j = 0; j < i; j++)
                normalMatrix[i, j] = normalMatrix[j, i];

        Matrix<double> left = Matrix<double>.Build.DenseOfArray(normalMatrix);
        Vector<double> right = Vector<double>.Build.DenseOfArray(rightSide);

        return left.Solve(right).Map(x => double.IsNaN(x) || double.IsInfinity(x) ? 0 : x);
    }

    private double GetGaussianWeight(double x, double y, double z, double spreadParameter)
    {
        return Math.Exp(-spreadParameter * (x * x + y * y + z * z));
    }

    /// <summary>
    /// This function takes in a value and rounds it to the nearest value that is a multiplier of spacing
    /// </summary>
    /// <param name="value">Value to be rounded</param>
    /// <param name="spacing">Spacing</param>
    /// <returns>Returns rounded value</returns>
    private double RoundToNearestSpacingMultiplier(double value, double spacing)
    {
        int unitDistance = (int)(value / spacing);
        double smallerNeighborDistance = value - (unitDistance * spacing);
        double biggerNeighborDistance = ((unitDistance + 1) * spacing) - value;

        return smallerNeighborDistance < biggerNeighborDistance ? (unitDistance * spacing) : ((unitDistance + 1) * spacing);
    }

    #region SpacingMultipliers
    private static int MinSpacingMulitplier(double currentCoordinate, double spacing, int desiredShift)
    {
        return (int)Math.Min(currentCoordinate / spacing, desiredShift);
    }

    private static int MaxSpacingMultiplier(double currentCoordinate, double maxValue, double spacing, int desiredShift)
    {
        return (int)Math.Min((maxValue - currentCoordinate) / spacing, desiredShift);
    }
    #endregion

    /// <summary>
    /// Artificial grid spacing used while sampling the neighborhood, along with the
    /// largest of the three axes (used for normalization by the weighting strategies).
    /// </summary>
    protected readonly struct Spacing
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;
        public readonly double Max;

        public Spacing(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
            Max = Math.Max(x, Math.Max(y, z));
        }
    }

    private class CacheEntry
    {
        public readonly AData Data;
        public readonly Point3D Point;
        public readonly ApproximationResult Result;

        public CacheEntry(AData data, Point3D point, ApproximationResult result)
        {
            Data = data;
            Point = point;
            Result = result;
        }
    }
}
