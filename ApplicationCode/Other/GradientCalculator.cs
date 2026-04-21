using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.DataClasses.Data;

namespace Registration.ApplicationCode.Other;

public class GradientCalculator
{
    public static Vector<double> GetFunctionGradient(Point3D p, AData d)
    {
        Point3D nearestGridPoint = new Point3D(
            RoundToNearestSpacingMultiplier(p.X, d.XSpacing),
            RoundToNearestSpacingMultiplier(p.Y, d.YSpacing),
            RoundToNearestSpacingMultiplier(p.Z, d.ZSpacing)
        );

        List<Point3D> surroundingPoints = GetSurroundingPoints(nearestGridPoint, d, 3);
        SpreadParameters parameters = CalculateSpreadParameter(d, 0.4);
        Vector<double> coeficients = GetApproximationEquation(surroundingPoints, p, nearestGridPoint, d, parameters);
        Vector<double> functionGradient = GetFunctionGradient(p, coeficients);

        return functionGradient;
    }

    private static List<Point3D> GetSurroundingPoints(Point3D nearestGridPoint, AData d, int radius)
    {
        List<Point3D> surroundingPoints = new List<Point3D>();

        int minSpacingMulitplierX = MinSpacingMulitplier(nearestGridPoint.X, d.XSpacing, radius),
            maxSpacingMultiplierX = MaxSpacingMultiplier(nearestGridPoint.X, d.MaxValueX, d.XSpacing, radius);
        int minSpacingMulitplierY = MinSpacingMulitplier(nearestGridPoint.Y, d.YSpacing, radius),
            maxSpacingMultiplierY = MaxSpacingMultiplier(nearestGridPoint.Y, d.MaxValueY, d.YSpacing, radius);
        int minSpacingMulitplierZ = MinSpacingMulitplier(nearestGridPoint.Z, d.ZSpacing, radius),
            maxSpacingMultiplierZ = MaxSpacingMultiplier(nearestGridPoint.Z, d.MaxValueZ, d.ZSpacing, radius);

        for (int x = -minSpacingMulitplierX; x <= maxSpacingMultiplierX; x++)
        {
            for (int y = -minSpacingMulitplierY; y <= maxSpacingMultiplierY; y++)
            {
                for (int z = -minSpacingMulitplierZ; z <= maxSpacingMultiplierZ; z++)
                {
                    surroundingPoints.Add(
                        new Point3D(
                            nearestGridPoint.X + x * d.XSpacing,
                            nearestGridPoint.Y + y * d.YSpacing,
                            nearestGridPoint.Z + z * d.ZSpacing
                        )
                    );
                }
            }
        }

        return surroundingPoints;
    }

    private static SpreadParameters CalculateSpreadParameter(AData d, double borderPercentage)
    {
        double spreadParameter = -Math.Log(borderPercentage) / 2;

        return new SpreadParameters(
            spreadParameter / d.XSpacing,
            spreadParameter / d.YSpacing,
            spreadParameter / d.ZSpacing
        );
    }

    private static double GetGaussianWeight(double x, double y, double z, SpreadParameters spreadParameters)
    {
        return Math.Exp(-(spreadParameters.spreadParameterX * x * x + spreadParameters.spreadParameterY * y * y +
                          spreadParameters.spreadParameterZ * z * z));
    }

    private static Vector<double> GetApproximationEquation(List<Point3D> surroundingPoints, Point3D referencePoint,
        Point3D centerPoint, AData d, SpreadParameters spreadParameters)
    {
        int NUMBER_OF_VARIABLES = 10;

        Matrix<double> qMatrixT = Matrix<double>.Build.Dense(NUMBER_OF_VARIABLES, surroundingPoints.Count);
        Matrix<double> qMatrix = Matrix<double>.Build.Dense(surroundingPoints.Count, NUMBER_OF_VARIABLES);
        Vector<double> values = Vector<double>.Build.Dense(surroundingPoints.Count);
        Vector<double> weightedValues = Vector<double>.Build.Dense(surroundingPoints.Count);
        Matrix<double> rightSide = Matrix<double>.Build.Dense(qMatrix.ColumnCount, 1);


        Point3D currentCenteredPoint;
        for (int i = 0; i < surroundingPoints.Count; i++)
        {
            double weight = GetGaussianWeight(
                surroundingPoints[i].X - referencePoint.X,
                surroundingPoints[i].Y - referencePoint.Y,
                surroundingPoints[i].Z - referencePoint.Z,
                spreadParameters
            );

            currentCenteredPoint = surroundingPoints[i] - centerPoint;

            qMatrixT[0, i] = Math.Pow(currentCenteredPoint.X, 2);
            qMatrixT[1, i] = Math.Pow(currentCenteredPoint.Y, 2);
            qMatrixT[2, i] = Math.Pow(currentCenteredPoint.Z, 2);
            qMatrixT[3, i] = currentCenteredPoint.X * currentCenteredPoint.Y;
            qMatrixT[4, i] = currentCenteredPoint.X * currentCenteredPoint.Z;
            qMatrixT[5, i] = currentCenteredPoint.Y * currentCenteredPoint.Z;
            qMatrixT[6, i] = currentCenteredPoint.X;
            qMatrixT[7, i] = currentCenteredPoint.Y;
            qMatrixT[8, i] = currentCenteredPoint.Z;
            qMatrixT[9, i] = 1;

            for (int j = 0; j < qMatrixT.RowCount; j++)
                qMatrix[i, j] = qMatrixT[j, i] * weight;

            values[i] = d.GetValue(surroundingPoints[i]);
            weightedValues[i] = values[i] * weight;
        }

        if (CheckSameValues(values))
            return Vector<double>.Build.Dense(NUMBER_OF_VARIABLES);

        /* Constructing right side */
        for (int i = 0; i < rightSide.RowCount; i++)
            rightSide[i, 0] = qMatrixT.Row(i).DotProduct(weightedValues);

        Matrix<double> left = qMatrixT.Multiply(qMatrix);
        return left.Solve(rightSide).Column(0).Map(x => double.IsNaN(x) ? 0 : x);
    }

    private static bool CheckSameValues(Vector<double> values)
    {
        double minValue = double.MaxValue, maxValue = double.MinValue;

        for (int i = 0; i < values.Count; i++)
        {
            if (minValue > values[i])
                minValue = values[i];

            if (maxValue < values[i])
                maxValue = values[i];
        }

        return Math.Abs(minValue - maxValue) == 0;
    }

    private static Vector<double> GetFunctionGradient(Point3D p, Vector<double> coeficients)
    {
        return Vector<double>.Build.DenseOfArray(new double[]
        {
            2 * coeficients[0] * p.X + coeficients[3] * p.Y + coeficients[4] * p.Z + coeficients[6],
            2 * coeficients[1] * p.Y + coeficients[3] * p.X + coeficients[5] * p.Z + coeficients[7],
            2 * coeficients[2] * p.Z + coeficients[4] * p.X + coeficients[5] * p.Y + coeficients[8]
        });
    }

    /// <summary>
    /// This function takes in a value and rounds it to the nearest value that is a multiplier of spacing
    /// </summary>
    /// <param name="value">Value to be rounded</param>
    /// <param name="spacing">Spacing</param>
    /// <returns>Returns rounded value</returns>
    private static double RoundToNearestSpacingMultiplier(double value, double spacing)
    {

        int unitDistance = (int)(value / spacing);
        double smallerNeighborDistance = value - (unitDistance * spacing);
        double biggerNeighborDistance = ((unitDistance + 1) * spacing) - value;

        return smallerNeighborDistance < biggerNeighborDistance
            ? (unitDistance * spacing)
            : ((unitDistance + 1) * spacing);
    }

    private static int MinSpacingMulitplier(double currentCoordinate, double spacing, int desiredShift)
    {
        return (int)Math.Min(currentCoordinate / spacing, desiredShift);
    }

    private static int MaxSpacingMultiplier(double currentCoordinate, double maxValue, double spacing, int desiredShift)
    {
        return (int)Math.Min((maxValue - currentCoordinate) / spacing, desiredShift);
    }

    private class SpreadParameters
    {
        public double spreadParameterX { get; }
        public double spreadParameterY { get; }
        public double spreadParameterZ { get; }

        public SpreadParameters(double spreadParameterX, double spreadParameterY, double spreadParameterZ)
        {
            this.spreadParameterX = spreadParameterX;
            this.spreadParameterY = spreadParameterY;
            this.spreadParameterZ = spreadParameterZ;
        }
    }
}