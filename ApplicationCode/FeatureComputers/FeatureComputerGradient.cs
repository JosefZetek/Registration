using System;
using System.Collections.Generic;
using DataView;
using MathNet.Numerics.LinearAlgebra;

using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.Other.FCConfiguration;
using Registration.ApplicationCode.RegistrationLaunchers;

namespace Registration.ApplicationCode.FeatureComputers;

public class FeatureComputerGradient : AFeatureComputer
{
    private double BORDER_VALUE;

    private double spreadParameter;

    public override int NumberOfFeatures => 1;

    public FeatureComputerGradient(double BORDER_VALUE, int RADIUS)
    {
        this.BORDER_VALUE = BORDER_VALUE;
    }

    public override void ComputeFeatureVector(AData d, Point3D p, double[] array, int startIndex)
    {
        CalculateSpreadParameter();
        
        Point3D nearestGridPoint = new Point3D(
            RoundToNearestSpacingMultiplier(p.X, d.XSpacing),
            RoundToNearestSpacingMultiplier(p.Y, d.YSpacing),
            RoundToNearestSpacingMultiplier(p.Z, d.ZSpacing)
        );


        double gradientNorm = ComputeGradientNorm(p, d, nearestGridPoint);
        
        // CalculateSpreadParameter(d, 0.8);
        // double a = ComputeGradient(p, d, nearestGridPoint, 5);
        //
        // CalculateSpreadParameter(d, 0.9);
        // double b = ComputeGradient(p, d, nearestGridPoint, 7);

        array[startIndex] = gradientNorm;
    }

    public override void GetWeights(double[] array, int startIndex)
    {
        for (int i = 0; i < NumberOfFeatures; i++)
            array[startIndex + i] = 1.0;
    }

    public override FCConfiguration GetConfiguration()
    {
        var config = new FCConfiguration();
        config.FeatureComputerType = FCType .GRADIENT_FEATURE_COMPUTER;
        config.AddParameter("BorderPercentage", BORDER_VALUE);
        config.AddParameter("Radius", 0);
        config.AddParameter("Comment", "Gradient");
        return config;
    }

    private double CalculateSpreadParameter()
    {
        return -Math.Log(BORDER_VALUE) / (Constants.PROXIMITY_RADIUS * Constants.PROXIMITY_RADIUS);
    }

    private Vector<double> GetFunctionGradient(Point3D p, Vector<double> coeficients)
    {
        return Vector<double>.Build.DenseOfArray(new double[]
        {
            2*coeficients[0]*p.X + coeficients[3] * p.Y + coeficients[4] * p.Z + coeficients[6],
            2*coeficients[1]*p.Y + coeficients[3] * p.X + coeficients[5] * p.Z + coeficients[7],
            2*coeficients[2]*p.Z + coeficients[4] * p.X + coeficients[5] * p.Y + coeficients[8]
        });
    }

    private double ComputeGradientNorm(Point3D point, AData d, Point3D centerPoint)
    {
        List<Point3D> surroundingPoints = CalculateSurroundingPoints(point, d);
        Vector<double> coeficients = GetApproximationEquation(surroundingPoints, point, centerPoint, d);
        Vector<double> functionGradient = GetFunctionGradient(point - centerPoint, coeficients);
        return functionGradient.L2Norm();
    }

    private Vector<double> GetApproximationEquation(List<Point3D> surroundingPoints, Point3D referencePoint, Point3D centerPoint, AData d)
    {
        int NUMBER_OF_VARIABLES = 10;

        Matrix<double> qMatrixT = Matrix<double>.Build.Dense(NUMBER_OF_VARIABLES, surroundingPoints.Count);
        Matrix<double> qMatrix = Matrix<double>.Build.Dense(surroundingPoints.Count, NUMBER_OF_VARIABLES);
        Vector<double> values = Vector<double>.Build.Dense(surroundingPoints.Count);
        Vector<double> weightedValues = Vector<double>.Build.Dense(surroundingPoints.Count);
        Matrix<double> rightSide = Matrix<double>.Build.Dense(qMatrix.ColumnCount, 1);

        Point3D centeredPoint = new Point3D(
            referencePoint.X - centerPoint.X,
            referencePoint.Y - centerPoint.Y,
            referencePoint.Z - centerPoint.Z
        );

        for (int i = 0; i < surroundingPoints.Count; i++)
        {
            double weight = GetGaussianWeight(
                surroundingPoints[i].X - centeredPoint.X,
                surroundingPoints[i].Y - centeredPoint.Y,
                surroundingPoints[i].Z - centeredPoint.Z
            );

            qMatrixT[0, i] = Math.Pow(surroundingPoints[i].X, 2);
            qMatrixT[1, i] = Math.Pow(surroundingPoints[i].Y, 2);
            qMatrixT[2, i] = Math.Pow(surroundingPoints[i].Z, 2);
            qMatrixT[3, i] = surroundingPoints[i].X * surroundingPoints[i].Y;
            qMatrixT[4, i] = surroundingPoints[i].X * surroundingPoints[i].Z;
            qMatrixT[5, i] = surroundingPoints[i].Y * surroundingPoints[i].Z;
            qMatrixT[6, i] = surroundingPoints[i].X;
            qMatrixT[7, i] = surroundingPoints[i].Y;
            qMatrixT[8, i] = surroundingPoints[i].Z;
            qMatrixT[9, i] = 1;

            for (int j = 0; j < qMatrixT.RowCount; j++)
                qMatrix[i, j] = qMatrixT[j, i] * weight;

            values[i] = d.GetValue(surroundingPoints[i] + centerPoint);

            weightedValues[i] = values[i] * weight;
        }

        if (CheckSameValues(values, 1))
            return Vector<double>.Build.Dense(NUMBER_OF_VARIABLES);

        /* Constructing right side */
        for (int i = 0; i < rightSide.RowCount; i++)
            rightSide[i, 0] = qMatrixT.Row(i).DotProduct(weightedValues);

        Matrix<double> left = qMatrixT.Multiply(qMatrix);
        return left.Solve(rightSide).Column(0).Map(x => double.IsNaN(x) || double.IsInfinity(x) ? 0 : x);
    }

    private bool CheckSameValues(Vector<double> values, double threshold)
    {
        double minValue = double.MaxValue, maxValue = double.MinValue;

        for (int i = 0; i < values.Count; i++)
        {
            if (minValue > values[i])
                minValue = values[i];

            if (maxValue < values[i])
                maxValue = values[i];
        }

        return Math.Abs(minValue - maxValue) < threshold;
    }

    private double GetGaussianWeight(double x, double y, double z)
    {
        return Math.Exp(-this.spreadParameter * (x * x + y * y + z * z));
    }

    private List<Point3D> CalculateSurroundingPoints(Point3D point, AData d)
    {
        List<Point3D> surroundingPoints = new List<Point3D>();

        double r = Constants.PROXIMITY_RADIUS;
        double s = Constants.PROXIMITY_SPACING;

        for (double x = -r; x <= r; x += s)
        for (double y = -r; y <= r; y += s)
        for (double z = -r; z <= r; z += s)
            surroundingPoints.Add(new Point3D(x, y, z));

        return surroundingPoints;
    }

    /// <summary>
    /// This function takes in a value and rounds it to the nearest value that is a multiplier of spacing
    /// </summary>
    /// <param name="value">Value to be rounded</param>
    /// <param name="spacing">Spacing</param>
    /// <returns>Returns rounded value</returns>
    private double RoundToNearestSpacingMultiplier(double value, double spacing)
    {

        int unitDistance = (int)(value / spacing);
        double smallerNeighborDistance = value - (unitDistance * spacing);
        double biggerNeighborDistance = ((unitDistance + 1) * spacing) - value;

        return smallerNeighborDistance < biggerNeighborDistance ? (unitDistance * spacing) : ((unitDistance + 1) * spacing);
    }

    public static int MinSpacingMulitplier(double currentCoordinate, double spacing, int desiredShift)
    {
        return (int)Math.Min(currentCoordinate / spacing, desiredShift);
    }

    public static int MaxSpacingMultiplier(double currentCoordinate, double maxValue, double spacing, int desiredShift)
    {
        return (int)Math.Min((maxValue - currentCoordinate) / spacing, desiredShift);
    }      
}