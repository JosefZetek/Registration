using System;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Approximation;

/// <summary>
/// Approximation strategy using elliptical interpolation weighting.
/// This is the weighting that was previously hard-coded in
/// <c>FeatureComputerISOCurvature2</c> / <c>FeatureComputerGradient2</c>.
/// </summary>
public class ApproximationComputerElliptical : AApproximationComputer
{
    public ApproximationComputerElliptical(double borderValue) : base(borderValue)
    {
    }

    protected override double GetSamplingWeight(Point3D surroundingPoint, Spacing spacing)
    {
        double unitsX = surroundingPoint.X / spacing.X;
        double unitsY = surroundingPoint.Y / spacing.Y;
        double unitsZ = surroundingPoint.Z / spacing.Z;

        double eucledeanDistance = Math.Sqrt((surroundingPoint.X * surroundingPoint.X) + (surroundingPoint.Y * surroundingPoint.Y) + (surroundingPoint.Z * surroundingPoint.Z));
        double unitDistance = Math.Sqrt((unitsX * unitsX) + (unitsY * unitsY) + (unitsZ * unitsZ));

        double scale = eucledeanDistance / unitDistance;
        return scale / spacing.Max;
    }
}
