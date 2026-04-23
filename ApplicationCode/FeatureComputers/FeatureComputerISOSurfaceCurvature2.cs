using System;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.FeatureComputers;

public class FeatureComputerISOCurvature2 : FeatureComputerISOCurvature
{
    /// <summary>
    /// Feature Computer computing Gaussian and Mean Curvature based on ISO-Surface approximation.
    /// Uses Elliptical Interpolation Weighting
    /// </summary>
    /// <param name="borderValue">Kernel output at point distanced RADIUS units from inspected point</param>
    /// <param name="radius">Number of units distanced from center at which kernel function drops to 'borderPercentage'</param>
    public FeatureComputerISOCurvature2(double borderValue, int radius): base(borderValue, radius)
    {
    }


    protected override double GetSamplingWeight(Point3D surroundingPoint)
    {
        double unitsX = surroundingPoint.X / artificialSpacingX;
        double unitsY = surroundingPoint.Y / artificialSpacingY;
        double unitsZ = surroundingPoint.Z / artificialSpacingZ;


        double eucledeanDistance = Math.Sqrt((surroundingPoint.X * surroundingPoint.X) + (surroundingPoint.Y * surroundingPoint.Y) + (surroundingPoint.Z * surroundingPoint.Z));
        double unitDistance = Math.Sqrt((unitsX * unitsX) + (unitsY * unitsY) + (unitsZ * unitsZ));

        double scale = eucledeanDistance / unitDistance;
        return scale / maxArtificialSpacing;
    }
}
