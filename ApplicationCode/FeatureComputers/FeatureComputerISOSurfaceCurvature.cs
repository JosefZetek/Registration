using System;
using System.Collections.Generic;
using System.Diagnostics;
using DataView;
using ExCSS;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.Other.FCConfiguration;
using Registration.ApplicationCode.RegistrationLaunchers;

namespace Registration.ApplicationCode.FeatureComputers;

public class FeatureComputerISOCurvature : AFeatureComputer
{
    private double BORDER_VALUE;

    public override int NumberOfFeatures => 2;
    
    /// <summary>
    /// Feature Computer computing Gaussian and Mean Curvature based on ISO-Surface approximation.
    /// </summary>
    /// <param name="BORDER_VALUE">Kernel output at point distanced RADIUS units from inspected point</param>
    /// <param name="RADIUS">Number of units distanced from center at which kernel function drops to 'borderPercentage'</param>
    public FeatureComputerISOCurvature(double BORDER_VALUE, int RADIUS)
    {
        this.BORDER_VALUE = BORDER_VALUE;
    }

    public override void ComputeFeatureVector(AData d, Point3D p, double[] array, int startIndex)
    {
        CheckArrayDimensions(array, startIndex);
        double spreadParameter = CalculateSpreadParameter();

        Point3D nearestGridPoint = new Point3D(
            RoundToNearestSpacingMultiplier(p.X, d.XSpacing),
            RoundToNearestSpacingMultiplier(p.Y, d.YSpacing),
            RoundToNearestSpacingMultiplier(p.Z, d.ZSpacing)
        );

        Curvature curvature = ComputeCurvature(p, d, nearestGridPoint, spreadParameter);
        
        array[startIndex] = curvature.GaussianCurvature;
        array[startIndex + 1] = curvature.MeanCurvature;
    }

    public override void GetWeights(double[] array, int startIndex)
    {
        for (int i = 0; i < NumberOfFeatures; i++)
            array[startIndex + i] = 1.0;
    }

    public override FCConfiguration GetConfiguration()
    {
        var config = new FCConfiguration();
        config.FeatureComputerType = FCType.CURVATURE_FEATURE_COMPUTER;
        config.AddParameter("BorderPercentage", BORDER_VALUE);
        config.AddParameter("Radius", 0);
        config.AddParameter("Comment", "Curvature");

        return config;
    }

    private double CalculateSpreadParameter()
    {
        return -Math.Log(BORDER_VALUE) / Constants.PROXIMITY_RADIUS;
    }

    private Matrix<double> ConstructAdjointMatrix(Matrix<double> hessianMatrix)
    {
        return Matrix<double>.Build.DenseOfArray(new double[,]
        {
            {
                hessianMatrix[1, 1] * hessianMatrix[2, 2] - hessianMatrix[1, 2] * hessianMatrix[2, 1],
                hessianMatrix[1, 2] * hessianMatrix[2, 0] - hessianMatrix[0, 1] * hessianMatrix[2, 2],
                hessianMatrix[1, 0] * hessianMatrix[2, 1] - hessianMatrix[1, 1] * hessianMatrix[2, 0],
            },
            {
                hessianMatrix[0, 2] * hessianMatrix[2, 1] - hessianMatrix[0, 1] * hessianMatrix[2, 2],
                hessianMatrix[0, 0] * hessianMatrix[2, 2] - hessianMatrix[0, 2] * hessianMatrix[2, 0],
                hessianMatrix[0, 1] * hessianMatrix[2, 0] - hessianMatrix[0, 0] * hessianMatrix[2, 1],

            },
            {
                hessianMatrix[0, 1] * hessianMatrix[1, 2] - hessianMatrix[0, 2] * hessianMatrix[1, 1],
                hessianMatrix[1, 0] * hessianMatrix[0, 2] - hessianMatrix[0, 0] * hessianMatrix[1, 2],
                hessianMatrix[0, 0] * hessianMatrix[1, 1] - hessianMatrix[0, 1] * hessianMatrix[1, 0]
            }
        });
    }

    private Matrix<double> ConstructHessianMatrix(Vector<double> coeficients)
    {
        return Matrix<double>.Build.DenseOfArray(new double[,]
        {
            { 2*coeficients[0], coeficients[3], coeficients[4] },
            { coeficients[3], 2*coeficients[1], coeficients[5] },
            { coeficients[4], coeficients[5], 2*coeficients[2] },
        });
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

    private Curvature ComputeCurvature(Point3D point, AData d, Point3D centerPoint, double spreadParameter)
    {
        List<Point3D> surroundingPoints = CalculateSurroundingPoints(point, d);
        Vector<double> coeficients = GetApproximationEquation(surroundingPoints, point, centerPoint, d, spreadParameter);

        Matrix<double> hessianMatrix = ConstructHessianMatrix(coeficients);
        Vector<double> functionGradient = GetFunctionGradient(point - centerPoint, coeficients);
        Matrix<double> adjointHessian = ConstructAdjointMatrix(hessianMatrix);

        double functionGradientNorm = functionGradient.L2Norm();

        if (functionGradientNorm == 0)
            return new Curvature(
                1000,
                1000
            );

        double gaussianCurvature = adjointHessian.LeftMultiply(functionGradient).DotProduct(functionGradient) /
                                   Math.Pow(functionGradientNorm, 4); /* Gaussian curvature */
        double meanCurvature = (hessianMatrix.LeftMultiply(functionGradient).DotProduct(functionGradient) -
             Math.Pow(functionGradientNorm, 2) * hessianMatrix.Trace()) /
            (2 * Math.Pow(functionGradientNorm, 3)); /* Mean curvature */

        //meanCurvature = Math.Abs(meanCurvature);
        //gaussianCurvature = Math.Abs(gaussianCurvature);

        return new Curvature(
            meanCurvature,
            gaussianCurvature
        );
    }

    private Vector<double> GetApproximationEquation(List<Point3D> surroundingPoints, Point3D referencePoint, Point3D centerPoint, AData d, double spreadParameter)
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
                surroundingPoints[i].Z - centeredPoint.Z,
                spreadParameter
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

    private double GetGaussianWeight(double x, double y, double z, double spreadParameter)
    {
        return Math.Exp(-spreadParameter * (x * x + y * y + z * z));
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

    #region SpacingMultipliers
    public static int MinSpacingMulitplier(double currentCoordinate, double spacing, int desiredShift)
    {
        return (int)Math.Min(currentCoordinate / spacing, desiredShift);
    }

    public static int MaxSpacingMultiplier(double currentCoordinate, double maxValue, double spacing, int desiredShift)
    {
        return (int)Math.Min((maxValue - currentCoordinate) / spacing, desiredShift);
    }
    #endregion

    private class Curvature
    {
        private double gaussianCurvature;
        private double meanCurvature;

        public Curvature(double gaussianCurvature, double meanCurvature)
        {
            this.gaussianCurvature = gaussianCurvature;
            this.meanCurvature = meanCurvature;
        }

        public double GaussianCurvature { get => gaussianCurvature; }
        public double MeanCurvature { get => meanCurvature; }
    }
}