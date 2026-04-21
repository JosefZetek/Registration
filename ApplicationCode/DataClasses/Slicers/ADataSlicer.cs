using System;
using System.Drawing;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.DataClasses.Slicers;

public abstract class ADataSlicer
{
    protected AData referenceData;
    private const int NUMBER_OF_PIXELS = 250_000;

    /// <summary>
    /// Cuts image and outputs 2D array with data organized as follows
    /// [row][column], [0][0] represents bottom left corner.
    /// Data thats been used in constructor are used as a refference (black tint).
    /// Where microData overlaps with refference data, it replaces the value with green tint.
    /// </summary>
    /// <param name="t">Value between 0-1</param>
    /// <param name="axis">Number of axis [0 = x, 1 = y, 2 = z]</param>
    /// <param name="transformation">Transformation that aligns microData onto macroData</param>
    /// <returns>Returns array of columns of a cut</returns>
    public abstract Color[][] Cut(double t, int axis, CutResolution resolution);

    public CutResolution GetRecommendedResolution(int axis)
    {
        int firstVariableIndex = (axis == 0) ? 1 : 0;
        int secondVariableIndex = (axis == 2) ? 1 : 2;

        double firstAxisNumber = this.referenceData.Measures[firstVariableIndex] * this.referenceData.Spacings[firstVariableIndex];
        double secondAxisNumber = this.referenceData.Measures[secondVariableIndex] * this.referenceData.Spacings[secondVariableIndex];

        double ratio = secondAxisNumber / firstAxisNumber;

        int pixelSize = (int)(Math.Sqrt(NUMBER_OF_PIXELS / ratio));

        return new CutResolution(pixelSize, (int)(ratio * pixelSize));
    }
}

