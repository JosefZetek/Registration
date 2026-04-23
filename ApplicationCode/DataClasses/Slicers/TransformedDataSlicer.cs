using System;
using System.Drawing;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.DataClasses.Slicers;

public class TransformedDataSlicer: ADataSlicer
{
	private AData macroData;
    private AData microData;

    private Transform3D transformation;

    private const int DIMENSIONS = 3;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="macroData">Macro data</param>
    /// <param name="microData">Micro data</param>
    /// <param name="transformation">Transformation that aligns microData onto macroData by calculating (Rx + t)</param>
	public TransformedDataSlicer(AData macroData, AData microData, Transform3D transformation)
	{
        this.referenceData = macroData;
		this.macroData = macroData;
        this.microData = microData;
        this.transformation = transformation;
	}

    private int NormalizeValue(double value)
    {
        double normalizedValue = (value - this.macroData.MinValue) / (this.macroData.MaxValue - this.macroData.MinValue);
        int result = (int)(normalizedValue * 255);
        return Math.Max(0, Math.Min(255, result));
    }

    public override Color[][] Cut(double t, int axis, CutResolution resolution)
    {
        Point3D microDataPoint;

        /* Constraining t to be within range */
        t = Math.Min(Math.Max(0, t), 1);

        double cutPosition = t * macroData.Bounds[axis];

        Color[][] cutData = new Color[resolution.Height][];
        double[] coordinates = new double[DIMENSIONS];
        coordinates[axis] = cutPosition;

        /* Assigning index for axes that are going to vary in each iteration */
        int firstVariableIndex = (axis == 0) ? 1 : 0, secondVariableIndex = (axis == 2) ? 1 : 2;

        int currentNormalizedValue;

        for (int i = 0; i < resolution.Height; i++)
        {
            cutData[i] = new Color[resolution.Width];

            double secondDimensionProgress = ((double)i / ((double)resolution.Height - 1)) * macroData.Bounds[secondVariableIndex];
            coordinates[secondVariableIndex] = secondDimensionProgress;

            for (int j = 0; j < resolution.Width; j++)
            {
                double firstDimensionProgress = ((double)j / ((double)resolution.Width - 1)) * macroData.Bounds[firstVariableIndex];
                coordinates[firstVariableIndex] = firstDimensionProgress;


                microDataPoint = new Point3D(coordinates[0], coordinates[1], coordinates[2]);
                microDataPoint = microDataPoint.ApplyRotationTranslation(transformation.GetInverseTransformation());

                if (microData.PointWithinBounds(microDataPoint))
                {
                    //currentNormalizedValue = (float)microData.GetNormalizedValue(microDataPoint);
                    currentNormalizedValue = NormalizeValue(microData.GetValue(microDataPoint));
                    cutData[i][j] = Color.FromArgb(255, 0, currentNormalizedValue, 0);
                }
                else
                {
                    /* If micro point isnt within bounds, macro point is used */
                    currentNormalizedValue = (int)(macroData.GetNormalizedValue(coordinates[0], coordinates[1], coordinates[2]) * 255);
                    cutData[i][j] = Color.FromArgb(255, currentNormalizedValue, currentNormalizedValue, currentNormalizedValue);
                }
            }
        }

        return cutData;
    }
}

