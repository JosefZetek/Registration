using System;
using System.Drawing;
using DataView;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.DataClasses.Slicers;

public class DataSlicer : ADataSlicer
{
    private const int DIMENSIONS = 3;

    public DataSlicer(AData data)
    {
        this.referenceData = data;
    }

    public override Color[][] Cut(double t, int axis, CutResolution resolution)
    {
        /* Constraining t to be within range */
        t = Math.Min(Math.Max(0, t), 1);

        double cutPosition = t * referenceData.Bounds[axis];

        Color[][] cutData = new Color[resolution.Height][];
        double[] coordinates = new double[DIMENSIONS];
        coordinates[axis] = cutPosition;

        /* Assigning index for axes that are going to vary in each iteration */
        int firstVariableIndex = (axis == 0) ? 1 : 0, secondVariableIndex = (axis == 2) ? 1 : 2;

        int currentNormalizedValue;

        for (int i = 0; i < resolution.Height; i++)
        {
            cutData[i] = new Color[resolution.Width];

            double secondDimensionProgress = ((double)i / ((double)resolution.Height - 1)) * referenceData.Bounds[secondVariableIndex];
            coordinates[secondVariableIndex] = secondDimensionProgress;

            for (int j = 0; j < resolution.Width; j++)
            {
                double firstDimensionProgress = ((double)j / ((double)resolution.Width - 1)) * referenceData.Bounds[firstVariableIndex];
                coordinates[firstVariableIndex] = firstDimensionProgress;

                currentNormalizedValue = (int)(referenceData.GetNormalizedValue(coordinates[0], coordinates[1], coordinates[2]) * 255);
                cutData[i][j] = Color.FromArgb(255, currentNormalizedValue, currentNormalizedValue, currentNormalizedValue);
            }
        }

        return cutData;
    }
}

