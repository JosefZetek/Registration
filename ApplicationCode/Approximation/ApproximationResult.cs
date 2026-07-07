using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Approximation;

/// <summary>
/// Result of approximating the volume in the neighborhood of a point.
/// Holds the coeficients of the fitted quadric together with the grid-aligned
/// center point they were computed around (needed to evaluate the fitted
/// function's gradient at the inspected point).
/// </summary>
public class ApproximationResult
{
    private readonly Vector<double> coeficients;
    private readonly Point3D centerPoint;

    public ApproximationResult(Vector<double> coeficients, Point3D centerPoint)
    {
        this.coeficients = coeficients;
        this.centerPoint = centerPoint;
    }

    /// <summary>Coeficients of the fitted quadric (length 10).</summary>
    public Vector<double> Coeficients => coeficients;

    /// <summary>Grid-aligned point the coeficients were fitted around.</summary>
    public Point3D CenterPoint => centerPoint;
}
