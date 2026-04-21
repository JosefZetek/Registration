using System;
using System.Drawing;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.Samplers;

namespace Registration.ApplicationCode.DataClasses.Slicers;

public class DataSamplerSlicer : ADataSlicer
{
    private ISampler sampler;
    private Point3D[] sampledPoints;

    private const int DIMENSIONS = 3;

    public DataSamplerSlicer(AData data, ISampler sampler)
    {
        this.referenceData = data;
        this.sampler = sampler;
        this.sampledPoints = sampler.Sample(data, 1000);
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

                double maxDifference = Math.Max(referenceData.Bounds[firstVariableIndex] / resolution.Width, referenceData.Bounds[secondVariableIndex] / resolution.Height);


                if(IsSampledPoint(new Point3D(coordinates[0], coordinates[1], coordinates[2]), maxDifference))
                {
                    cutData[i][j] = Color.FromArgb(255, 255, 0, 0);
                    continue;
                }
                currentNormalizedValue = (int)(referenceData.GetNormalizedValue(coordinates[0], coordinates[1], coordinates[2]) * 255);
                cutData[i][j] = Color.FromArgb(255, currentNormalizedValue, currentNormalizedValue, currentNormalizedValue);
            }
        }

        return cutData;
    }

    private bool IsSampledPoint(Point3D point, double threshold)
    {
        for (int i = 0; i < sampledPoints.Length; i++)
        {
            if (Math.Abs(sampledPoints[i].X - point.X) > threshold)
                continue;

            if (Math.Abs(sampledPoints[i].Y - point.Y) > threshold)
                continue;

            if (Math.Abs(sampledPoints[i].Z - point.Z) > threshold)
                continue;

            return true;
        }

        return false;
    }

    private float Constrain(float constrainedValue, float minValue, float maxValue)
    {
        return MathF.Max(MathF.Min(constrainedValue, maxValue), minValue);
    }

    private float Normalize(float valueToNormalize)
    {
        return (valueToNormalize - 0.00625f) / (0.1f - 0.00625f);
    }
}

