using Registration.ApplicationCode.Approximation;

namespace Registration.ApplicationCode.FeatureComputers;

public class FeatureComputerGradient2 : FeatureComputerGradient
{
    public FeatureComputerGradient2(double borderValue, int radius)
        : base(borderValue, new ApproximationComputerElliptical(borderValue))
    {
    }

    /// <summary>
    /// Feature Computer using an externally supplied (elliptical) approximation computer so it can
    /// be shared with a matching Curvature feature computer to avoid recomputing the approximation.
    /// </summary>
    /// <param name="borderValue">Kernel output at point distanced RADIUS units from inspected point</param>
    /// <param name="approximationComputer">Approximation computer producing the fitted quadric</param>
    public FeatureComputerGradient2(double borderValue, AApproximationComputer approximationComputer)
        : base(borderValue, approximationComputer)
    {
    }
}
