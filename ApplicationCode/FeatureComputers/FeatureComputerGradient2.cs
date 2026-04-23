using System;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.FeatureComputers;

public class FeatureComputerGradient2 : FeatureComputerGradient
{
    private double borderValue;
    protected double artificialSpacingX, artificialSpacingY, artificialSpacingZ, maxArtificialSpacing;


    public override int NumberOfFeatures => 1;

    public FeatureComputerGradient2(double borderValue, int radius): base(borderValue, radius)
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