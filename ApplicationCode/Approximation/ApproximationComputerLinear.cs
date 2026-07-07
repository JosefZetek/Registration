using System;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Approximation;

/// <summary>
/// Approximation strategy using linear (skewness based) interpolation weighting.
/// </summary>
public class ApproximationComputerLinear : AApproximationComputer
{
    public ApproximationComputerLinear(double borderValue) : base(borderValue)
    {
    }

    protected override double GetSamplingWeight(Point3D surroundingPoint, Spacing spacing)
    {
        double coordinateSum = Math.Abs(surroundingPoint.X) + Math.Abs(surroundingPoint.Y) + Math.Abs(surroundingPoint.Z);
        if (Math.Abs(coordinateSum) < 1E-3)
            return 1;

        double xSkewness = Math.Abs(surroundingPoint.X) / coordinateSum;
        double ySkewness = Math.Abs(surroundingPoint.Y) / coordinateSum;
        double zSkewness = Math.Abs(surroundingPoint.Z) / coordinateSum;

        double samplingWeight = (spacing.X / spacing.Max) * xSkewness +
                                (spacing.Y / spacing.Max) * ySkewness +
                                (spacing.Z / spacing.Max) * zSkewness;

        return samplingWeight;
    }
}
