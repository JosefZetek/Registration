using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.Matching;

namespace Registration.ApplicationCode.Other;

/// <summary>
/// Refines the transformation picked by the density filter with a least-squares rigid
/// fit (Kabsch algorithm) over the matches consistent with it.
///
/// The density filter returns the transformation derived from a single match, so its
/// accuracy is limited by the noise of that one correspondence (and of the PCA basis
/// at those two points). Re-estimating the rotation and translation jointly from all
/// inlier correspondences averages that noise out. The fit runs several rounds with a
/// shrinking inlier tolerance so late rounds are driven only by accurate matches.
/// </summary>
public static class TransformationRefiner
{
    /* Inlier tolerances as fractions of the macro volume diagonal, applied in order. */
    private static readonly double[] RELATIVE_TOLERANCES = { 0.10, 0.05, 0.025 };

    /* A rigid transformation needs at least 3 point pairs (non-collinear). */
    private const int MIN_INLIERS = 3;

    /// <summary>
    /// Returns the refined transformation, or the passed transformation unchanged when
    /// there are not enough consistent matches to fit a more accurate one.
    /// </summary>
    /// <param name="matches">All matches the initial transformation was voted from</param>
    /// <param name="initialTransformation">Winner of the density filtering</param>
    /// <param name="macroDiagonal">Diagonal of the macro volume (tolerance scale)</param>
    public static Transform3D Refine(Match[] matches, Transform3D initialTransformation, double macroDiagonal)
    {
        if (initialTransformation == null || matches == null || matches.Length == 0)
            return initialTransformation;

        Transform3D currentTransformation = initialTransformation;

        foreach (double relativeTolerance in RELATIVE_TOLERANCES)
        {
            List<Match> inliers = CollectInliers(matches, currentTransformation, relativeTolerance * macroDiagonal);

            if (inliers.Count < MIN_INLIERS)
                return currentTransformation;

            Transform3D refinedTransformation = FitRigidTransformation(inliers);

            if (refinedTransformation == null)
                return currentTransformation;

            currentTransformation = refinedTransformation;

            Console.WriteLine($"Refinement: {inliers.Count}/{matches.Length} inliers within {relativeTolerance * macroDiagonal:0.##} units.");
        }

        return currentTransformation;
    }

    /// <summary>
    /// Matches whose micro point, transformed by the current estimate, lands within
    /// tolerance of their macro point.
    /// </summary>
    private static List<Match> CollectInliers(Match[] matches, Transform3D transformation, double tolerance)
    {
        List<Match> inliers = new List<Match>();

        foreach (Match match in matches)
        {
            Point3D transformedMicroPoint = match.microFV.Point.ApplyRotationTranslation(transformation);

            if (transformedMicroPoint.Distance(match.macroFV.Point) <= tolerance)
                inliers.Add(match);
        }

        return inliers;
    }

    /// <summary>
    /// Kabsch algorithm: the rigid transformation minimizing the sum of squared
    /// distances between transformed micro points and their macro points.
    /// </summary>
    private static Transform3D FitRigidTransformation(List<Match> inliers)
    {
        Vector<double> microCentroid = Vector<double>.Build.Dense(3);
        Vector<double> macroCentroid = Vector<double>.Build.Dense(3);

        foreach (Match match in inliers)
        {
            microCentroid += match.microFV.Point.ToVector();
            macroCentroid += match.macroFV.Point.ToVector();
        }

        microCentroid /= inliers.Count;
        macroCentroid /= inliers.Count;

        /* Cross-covariance H = sum of (micro - microCentroid) * (macro - macroCentroid)^T */
        Matrix<double> crossCovariance = Matrix<double>.Build.Dense(3, 3);

        foreach (Match match in inliers)
        {
            Vector<double> microCentered = match.microFV.Point.ToVector() - microCentroid;
            Vector<double> macroCentered = match.macroFV.Point.ToVector() - macroCentroid;

            crossCovariance += microCentered.OuterProduct(macroCentered);
        }

        var svd = crossCovariance.Svd(true);
        Matrix<double> u = svd.U;
        Matrix<double> v = svd.VT.Transpose();

        /* Sign correction keeps the result a proper rotation (det +1), not a reflection. */
        double determinantSign = Math.Sign((v * u.Transpose()).Determinant());

        if (determinantSign == 0)
            return null;

        Matrix<double> signCorrection = Matrix<double>.Build.DenseOfDiagonalArray(new double[] { 1, 1, determinantSign });
        Matrix<double> rotationMatrix = v * signCorrection * u.Transpose();

        foreach (double value in rotationMatrix.Enumerate())
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return null;
        }

        Vector<double> translationVector = macroCentroid - rotationMatrix.Multiply(microCentroid);

        return new Transform3D(rotationMatrix, translationVector);
    }
}
