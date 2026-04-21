using System;
using System.Data;
using System.IO;
using System.Linq;
using DataView;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.DataClasses.Data;

/// <summary>
/// This class represents the data
/// </summary>
public class VolumetricData : AData
{
    /* Data itself */
    private ushort[][,] vData;

    /* Spacings */
    private double xSpacing;
    private double ySpacing;
    private double zSpacing;

    /* Data information */
    private Data data;
    private VolumetricDataDistribution dataDistribution;
    
    public VolumetricData(FilePathDescriptor filePathDescriptor)
    {
        LoadMetadata(filePathDescriptor);
        ReadData(filePathDescriptor);
    }

    private void LoadMetadata(FilePathDescriptor filePathDescriptor)
    {
        Data = new Data(filePathDescriptor.MHDFilePath);

        xSpacing = this.Data.ElementSpacing[0];
        ySpacing = this.Data.ElementSpacing[1];
        zSpacing = this.Data.ElementSpacing[2];
    }

    /// <summary>
    /// Reads the raw data from a file
    /// </summary>
    /// <returns>Returns array with the data</returns>
    private ushort[][,] ReadData(FilePathDescriptor filePathDescriptor)
    {
        using (BinaryReader br = new BinaryReader(new FileStream(filePathDescriptor.DataFilePath, FileMode.Open)))
        {
            int width = Data.DimSize[0];
            int depth = Data.DimSize[1];
            int height = Data.DimSize[2];

            VData = new ushort[height][,];
            dataDistribution = new VolumetricDataDistribution();

            switch (Data.ElementType.ToUpper())
            {
                case "MET_USHORT":
                    LoadUshortFile(height, width, depth, br);
                    break;
                case "MET_UCHAR":
                    LoadUcharFile(height, width, depth, br);
                    break;
                default:
                    throw new DataException("Wrong element type specified in the Meta File.");
            }

            br.Close();
            return VData;
        }
    }

    private void LoadUcharFile(int height, int width, int depth, BinaryReader br)
    {
        ushort c;
        for (int k = 0; k < height; k++)
        {
            VData[k] = new ushort[width, depth];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < depth; j++)
                {
                    c = br.ReadByte();
                    VData[k][i, j] = c;
                    dataDistribution.AddValue(c);
                }
            }
        }
    }
    private void LoadUshortFile(int height, int width, int depth, BinaryReader br)
    {
        ushort c;
        for (int k = 0; k < height; k++)
        {
            VData[k] = new ushort[width, depth];
            for (int j = 0; j < depth; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    byte a = br.ReadByte();
                    byte b = br.ReadByte();
                    c = (ushort)(256 * b + a);

                    VData[k][i, j] = c;

                    dataDistribution.AddValue(c);
                }
            }
        }
    }

    public override double GetValue(double x, double y, double z) // Interpolation3D in real coordinates 
    {
        return GetValue(new Point3D(x, y, z));
    }

    public override double GetValue(Point3D point)
    {
        int xIndexLower = ConstrainIndex((int)(point.X / XSpacing), Data.DimSize[0] - 1);
        int yIndexLower = ConstrainIndex((int)(point.Y / YSpacing), Data.DimSize[1] - 1);
        int zIndexLower = ConstrainIndex((int)(point.Z / ZSpacing), Data.DimSize[2] - 1);

        int zIndexHigher = ConstrainIndex(zIndexLower + 1, Data.DimSize[2] - 1);

        double interpolatedXYLowerZ = InterpolationXYPlane(point.X, point.Y, zIndexLower, xIndexLower, yIndexLower);
        double interpolatedXYHigherZ = InterpolationXYPlane(point.X, point.Y, zIndexHigher, xIndexLower, yIndexLower);

        return InterpolationReal(interpolatedXYLowerZ, interpolatedXYHigherZ, point.Z, zIndexLower*ZSpacing, ZSpacing);
    }

    /// <summary>
    /// Method constrain given index for array to satisfy
    ///  index >= 0 && index <= maxIndex
    /// </summary>
    /// <param name="currentIndex">Current index</param>
    /// <param name="maxIndex">Maximum index</param>
    /// <returns></returns>
    private int ConstrainIndex(int currentIndex, int maxIndex)
    {
        return Math.Max(Math.Min(currentIndex, maxIndex), 0);
    }

    /// <summary>
    /// 1D Interpolation between valueA and valueB
    /// </summary>
    /// <param name="valueA">The value at the closest sampled coordinate that is smaller</param>
    /// <param name="valueB">The value at the closest sampled coordinate that is higher</param>
    /// <param name="interpolationCoordinate">Interpolated point's X/Y/Z coordinate<param>
    /// <param name="aPosition">Position of A (coordinates)</param>
    /// <param name="spacing">Spacing between A and B</param>
    /// <returns>Returns interpolated value</returns>
    private double InterpolationReal(double valueA, double valueB, double interpolationCoordinate, double aPosition, double spacing)
    {
        double ratio = (interpolationCoordinate - aPosition) / spacing;
        return ratio * valueB + (1 - ratio) * valueA;
    }

    /// <summary>
    /// Interpolation for X,Y plane
    /// </summary>
    /// <param name="xInterpolationCoordinate">Interpolated point's X coordinate</param>
    /// <param name="yInterpolationCoordinate">Interpolated point's Y coordinate</param>
    /// <param name="zIndex">Z index - constant (higher or lower zIndex)</param>
    /// <param name="xIndexLower">Lower X index</param>
    /// <param name="yIndexLower">Lower Y index</param>
    /// <returns>Returns interpolated value at given X, Y coordinates</returns>
    private double InterpolationXYPlane(double xInterpolationCoordinate, double yInterpolationCoordinate, int zIndex, int xIndexLower, int yIndexLower)
    {
        int xIndexHigher = ConstrainIndex(xIndexLower + 1, Data.DimSize[0] - 1);
        int yIndexHigher = ConstrainIndex(yIndexLower + 1, Data.DimSize[1] - 1);

        int valueA = VData[zIndex][xIndexLower, yIndexLower];
        int valueB = VData[zIndex][xIndexHigher, yIndexLower];

        double interpolationLowerY = InterpolationReal(valueA, valueB, xInterpolationCoordinate, xIndexLower*XSpacing, XSpacing);

        int valueC = VData[zIndex][xIndexLower, yIndexHigher];
        int valueD = VData[zIndex][xIndexHigher, yIndexHigher];

        double interpolationHigherY = InterpolationReal(valueC, valueD, xInterpolationCoordinate, xIndexLower*XSpacing, XSpacing);

        return InterpolationReal(interpolationLowerY, interpolationHigherY, yInterpolationCoordinate, yIndexLower*YSpacing, YSpacing);
    }

    public override double GetPercentile(double value)
    {
        return dataDistribution.GetDistributionPercentage(value);
    }

    public override int[] Measures { get => Data.DimSize; }

    public override double XSpacing { get => xSpacing; }
    public override double YSpacing { get => ySpacing; }
    public override double ZSpacing { get => zSpacing; }

    internal Data Data { get => data; set => data = value; }

    public ushort[][,] VData { get => vData; set => vData = value; }

    public override double MinValue { get => dataDistribution.MinValue; }
    public override double MaxValue { get => dataDistribution.MaxValue; }

    
}