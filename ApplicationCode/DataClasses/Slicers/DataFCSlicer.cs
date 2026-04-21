using System;
using System.Collections.Generic;
using System.Drawing;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.FeatureComputers;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.DataClasses.Slicers;

public class DataFCSlicer : ADataSlicer
{
    private FeatureComputerISOCurvature featureComputer;

    private const int DIMENSIONS = 3;

    public DataFCSlicer(AData data)
    {
        this.referenceData = data;
        this.featureComputer = new FeatureComputerISOCurvature(0.8, 5);
    }

    public override Color[][] Cut(double t, int axis, CutResolution resolution)
    {
        /* Constraining t to be within range */
        t = Math.Min(Math.Max(0, t), 1);

        double cutPosition = t * referenceData.Bounds[axis];

        double[][] cutData = new double[resolution.Height][];
        double[] coordinates = new double[DIMENSIONS];
        coordinates[axis] = cutPosition;

        /* Assigning index for axes that are going to vary in each iteration */
        int firstVariableIndex = (axis == 0) ? 1 : 0, secondVariableIndex = (axis == 2) ? 1 : 2;

        FeatureVector featureVector;

        for (int i = 0; i < resolution.Height; i++)
        {
            cutData[i] = new double[resolution.Width];

            double secondDimensionProgress = ((double)i / ((double)resolution.Height - 1)) * referenceData.Bounds[secondVariableIndex];
            coordinates[secondVariableIndex] = secondDimensionProgress;

            for (int j = 0; j < resolution.Width; j++)
            {
                double firstDimensionProgress = ((double)j / ((double)resolution.Width - 1)) * referenceData.Bounds[firstVariableIndex];
                coordinates[firstVariableIndex] = firstDimensionProgress;

                featureVector = featureComputer.ComputeFeatureVector(referenceData, new Point3D(coordinates[0], coordinates[1], coordinates[2]));
                cutData[i][j] = featureVector.Features[1];
            }
        }

        NormalizeArray(cutData);

        Color[][] cutDataColors = new Color[cutData.Length][];

        for(int i = 0; i<cutData.Length; i++)
        {
            cutDataColors[i] = new Color[cutData[i].Length];

            for(int j = 0; j < cutData[i].Length; j++)
                cutDataColors[i][j] = Color.FromArgb(255, (int)(cutData[i][j] * 255), (int)(cutData[i][j] * 255), (int)(cutData[i][j] * 255));
        }

        return cutDataColors;
    }

    private List<double> Flatten(double[][] array)
    {
        List<double> flattenedArray = new List<double>();

        for(int i = 0; i<array.Length; i++)
        {
            for (int j = 0; j < array[i].Length; j++)
                flattenedArray.Add(array[i][j]);
        }

        return flattenedArray;
    }

    private void NormalizeArray(double[][] array)
    {
        List<double> flattenedArray = Flatten(array);

        var quickSelect = new QuickSelectClass();
        double lowerThreshold = quickSelect.QuickSelect(flattenedArray, (int)(flattenedArray.Count * 0.05));
        double upperThreshold = quickSelect.QuickSelect(flattenedArray, (int)(flattenedArray.Count * 0.7));

        for(int i = 0; i<array.Length; i++)
        {
            for (int j = 0; j < array[i].Length; j++)
                array[i][j] = Normalize(array[i][j], lowerThreshold, upperThreshold);
        }
    }

    private double Constrain(double constrainedValue, double minValue, double maxValue)
    {
        return Math.Max(Math.Min(constrainedValue, maxValue), minValue);
    }

    private double Normalize(double valueToNormalize, double lowerThreshold, double upperThreshold)
    {
        return Constrain((valueToNormalize - lowerThreshold) / (upperThreshold - lowerThreshold), 0, 1);
    }
}