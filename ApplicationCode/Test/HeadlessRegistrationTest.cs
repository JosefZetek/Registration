using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.RegistrationLaunchers;

namespace Registration.ApplicationCode.Test;

/// <summary>
/// End-to-end registration test runnable without the UI:
/// 1. loads a source volume,
/// 2. writes an artificially rotated + translated copy resampled to a different
///    (anisotropic) spacing - the exact scenario that registration should recover,
/// 3. registers the source (micro) onto the transformed copy (macro),
/// 4. compares the result against the analytically known expected transformation.
///
/// Usage: dotnet run -- --headless-registration-test [sourceMhd] [sourceRaw] [points] [workDir]
/// </summary>
public static class HeadlessRegistrationTest
{
    public static void Run(string[] args)
    {
        /* MathNet/console output with dots regardless of system locale */
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        string sourceMhd = args.Length > 1 ? args[1] : "/Users/pepazetek/Desktop/Tests/Jatra/microData1.mhd";
        string sourceRaw = args.Length > 2 ? args[2] : "/Users/pepazetek/Desktop/Tests/Jatra/microData1.raw";
        int numberOfPoints = args.Length > 3 ? int.Parse(args[3]) : 3000;
        string workDirectory = args.Length > 4 ? args[4] : Path.GetTempPath();

        Console.WriteLine($"Source volume: {sourceMhd}");
        Console.WriteLine($"Sampled points per volume: {numberOfPoints}");

        Stopwatch stopwatch = Stopwatch.StartNew();
        VolumetricData sourceData = new VolumetricData(new FilePathDescriptor(sourceMhd, sourceRaw));
        Console.WriteLine($"Source loaded in {stopwatch.Elapsed.TotalSeconds:0.0} s " +
                          $"({sourceData.Measures[0]}x{sourceData.Measures[1]}x{sourceData.Measures[2]}, " +
                          $"spacing {sourceData.XSpacing}/{sourceData.YSpacing}/{sourceData.ZSpacing})");

        /* Generation transform: a clearly non-trivial rotation about all three axes. */
        Matrix<double> generationRotation = Generator.GetRotationMatrix(
            12 * Math.PI / 180,
            8 * Math.PI / 180,
            20 * Math.PI / 180
        );

        /* Different, anisotropic target spacing. */
        double[] targetSpacing = { 0.08, 0.07, 0.09 };
        int[] targetMeasures =
        {
            (int)(sourceData.MaxValueX / targetSpacing[0]) + 1,
            (int)(sourceData.MaxValueY / targetSpacing[1]) + 1,
            (int)(sourceData.MaxValueZ / targetSpacing[2]) + 1
        };

        /* Rotate about the volume centers: t = centerSource - R * centerTarget, so the
           middle of the target grid maps onto the middle of the source volume. */
        Vector<double> sourceCenter = Vector<double>.Build.DenseOfArray(new[]
        {
            sourceData.MaxValueX / 2, sourceData.MaxValueY / 2, sourceData.MaxValueZ / 2
        });
        Vector<double> targetCenter = Vector<double>.Build.DenseOfArray(new[]
        {
            (targetMeasures[0] - 1) * targetSpacing[0] / 2,
            (targetMeasures[1] - 1) * targetSpacing[1] / 2,
            (targetMeasures[2] - 1) * targetSpacing[2] / 2
        });

        Transform3D generationTransformation = new Transform3D(
            generationRotation,
            sourceCenter - generationRotation.Multiply(targetCenter)
        );

        stopwatch.Restart();
        string targetName = "headlessTransformedTarget";
        string directoryWithSeparator = workDirectory.EndsWith(Path.DirectorySeparatorChar) ? workDirectory : workDirectory + Path.DirectorySeparatorChar;
        new TransformedFileSaver(directoryWithSeparator, targetName, sourceData, generationTransformation, targetMeasures, targetSpacing).MakeFiles();
        Console.WriteLine($"Transformed target written in {stopwatch.Elapsed.TotalSeconds:0.0} s " +
                          $"({targetMeasures[0]}x{targetMeasures[1]}x{targetMeasures[2]}, " +
                          $"spacing {targetSpacing[0]}/{targetSpacing[1]}/{targetSpacing[2]})");

        VolumetricData targetData = new VolumetricData(new FilePathDescriptor(
            directoryWithSeparator + targetName + ".mhd",
            directoryWithSeparator + targetName + ".raw"
        ));

        /* target(p) = source(R*p + t) means a micro (source) point q corresponds to the
           macro (target) point R^-1*(q - t): the expected estimate is the inverse. */
        Transform3D expectedTransformation = generationTransformation.GetInverseTransformation();
        RegistrationLauncher.EXPECTED_TRANSFORMATION = expectedTransformation;

        Constants.NUMBER_OF_POINTS_MICRO = numberOfPoints;
        Constants.NUMBER_OF_POINTS_MACRO = numberOfPoints;

        stopwatch.Restart();
        Transform3D result = new RegistrationLauncher().RunRegistration(sourceData, targetData);
        Console.WriteLine($"Registration finished in {stopwatch.Elapsed.TotalSeconds:0.0} s");

        if (result == null)
        {
            Console.WriteLine("Registration produced no transformation (no matches survived).");
            return;
        }

        Console.WriteLine($"\nExpected rotation:\n{expectedTransformation.RotationMatrix}");
        Console.WriteLine($"Computed rotation:\n{result.RotationMatrix}");
        Console.WriteLine($"Expected translation: {expectedTransformation.TranslationVector}");
        Console.WriteLine($"Computed translation: {result.TranslationVector}");

        /* Angle of the residual rotation between computed and expected. */
        Matrix<double> residualRotation = result.RotationMatrix.Transpose() * expectedTransformation.RotationMatrix;
        double angleErrorDegrees = Math.Acos(Math.Clamp((residualRotation.Trace() - 1) / 2, -1, 1)) * 180 / Math.PI;
        double translationError = (result.TranslationVector - expectedTransformation.TranslationVector).L2Norm();

        /* Worst displacement error over the source volume corners - a single number
           combining rotation and translation error in physical units. */
        double maxCornerError = 0;
        foreach (double cornerX in new[] { 0, sourceData.MaxValueX })
        {
            foreach (double cornerY in new[] { 0, sourceData.MaxValueY })
            {
                foreach (double cornerZ in new[] { 0, sourceData.MaxValueZ })
                {
                    Point3D corner = new Point3D(cornerX, cornerY, cornerZ);
                    double cornerError = corner.ApplyRotationTranslation(result)
                        .Distance(corner.ApplyRotationTranslation(expectedTransformation));
                    maxCornerError = Math.Max(maxCornerError, cornerError);
                }
            }
        }

        double sourceDiagonal = Math.Sqrt(
            sourceData.MaxValueX * sourceData.MaxValueX +
            sourceData.MaxValueY * sourceData.MaxValueY +
            sourceData.MaxValueZ * sourceData.MaxValueZ);

        Console.WriteLine($"\nRotation error: {angleErrorDegrees:0.00} degrees");
        Console.WriteLine($"Translation error: {translationError:0.000} units");
        Console.WriteLine($"Max corner displacement error: {maxCornerError:0.000} units ({100 * maxCornerError / sourceDiagonal:0.0} % of volume diagonal {sourceDiagonal:0.0})");
    }
}
