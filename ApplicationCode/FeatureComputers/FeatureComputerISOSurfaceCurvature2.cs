using Registration.ApplicationCode.Approximation;

namespace Registration.ApplicationCode.FeatureComputers;

public class FeatureComputerISOCurvature2 : FeatureComputerISOCurvature
{
    /// <summary>
    /// Feature Computer computing Gaussian and Mean Curvature based on ISO-Surface approximation.
    /// Uses Elliptical Interpolation Weighting
    /// </summary>
    /// <param name="borderValue">Kernel output at point distanced RADIUS units from inspected point</param>
    /// <param name="radius">Number of units distanced from center at which kernel function drops to 'borderPercentage'</param>
    public FeatureComputerISOCurvature2(double borderValue, int radius)
        : base(borderValue, new ApproximationComputerElliptical(borderValue))
    {
    }

    /// <summary>
    /// Feature Computer using an externally supplied (elliptical) approximation computer so it can
    /// be shared with a matching Gradient feature computer to avoid recomputing the approximation.
    /// </summary>
    /// <param name="borderValue">Kernel output at point distanced RADIUS units from inspected point</param>
    /// <param name="approximationComputer">Approximation computer producing the fitted quadric</param>
    public FeatureComputerISOCurvature2(double borderValue, AApproximationComputer approximationComputer)
        : base(borderValue, approximationComputer)
    {
    }
}
