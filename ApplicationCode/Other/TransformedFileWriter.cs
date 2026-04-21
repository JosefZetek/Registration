using System;
using System.IO;
using MathNet.Numerics.LinearAlgebra;
using Registration.ApplicationCode.DataClasses.Data;

namespace Registration.ApplicationCode.Other;

public class TransformedFileSaver
{

    private const int DIMENSIONS = 3;

    private BinaryWriter binaryWriter;
    private StreamWriter streamWriter;

    private string fileName;

    private AMockObject sourceObject;
    private Transform3D transformation;

    private int[] Measures;
    private double[] Spacing;

    private Vector<double> baseXTransformed, baseYTransformed, baseZTransformed;

    private const int OUT_OF_BOUNDS = 5000;

    /// <summary>
    /// Class saves artificial data to a given directory
    /// </summary>
    /// <param name="directory">Directory where data are going to be saved</param>
    /// <param name="fileName">Name of a file (without extension)</param>
    /// <param name="sourceObject">Data to be saved</param>
    /// <exception cref="ArgumentException"></exception>
    public TransformedFileSaver(string directory, string fileName, AMockObject sourceObject, Transform3D transformation, int[] Measures, double[] Spacing)
    {
        this.fileName = fileName;
        this.sourceObject = sourceObject;
        this.transformation = transformation;
        this.Measures = Measures;
        this.Spacing = Spacing;

        if (sourceObject == null)
            throw new ArgumentException("No data were passed");

        if (sourceObject.Measures == null)
            throw new ArgumentException("Measures are not specified");

        if (sourceObject.Measures.Length != DIMENSIONS)
            throw new ArgumentException("Data need to be " + DIMENSIONS + " dimensional");

        if (sourceObject.Measures[0] <= 0 || sourceObject.Measures[1] <= 0 || sourceObject.Measures[2] <= 0)
            throw new ArgumentException("None of the dimensions can be negative or zero");

        if (transformation == null)
            this.transformation = new Transform3D();

        try
        {
            binaryWriter = new BinaryWriter(new FileStream(directory + fileName + ".raw", FileMode.Create));
            streamWriter = new StreamWriter(directory + fileName + ".mhd");
        }
        catch (IOException e) { throw e; }
    }

    public void MakeFiles()
    {
        InitializeBases();

        try
        {
            MakeBinaryFile();
            MakeMHDFile();
        }
        catch (IOException e) { throw e; }
    }

    private void MakeMHDFile()
    {
        streamWriter.WriteLine("ObjectType = Image");
        streamWriter.WriteLine("NDims = 3");
        streamWriter.WriteLine("BinaryData = True");
        streamWriter.WriteLine("BinaryDataByteOrderMSB = False");
        streamWriter.WriteLine("CompressedData = False");
        streamWriter.WriteLine("TransformMatrix = 1 0 0 0 1 0 0 0 1");
        streamWriter.WriteLine("Offset = 0 0 0");
        streamWriter.WriteLine("CenterOfRotation = 0 0 0");
        streamWriter.WriteLine("AnatomicalOrientation = RAI");
        streamWriter.WriteLine("ElementSpacing = {0} {1} {2}", Spacing[0], Spacing[1], Spacing[2]);
        streamWriter.WriteLine("DimSize = {0} {1} {2}", Measures[0], Measures[1], Measures[2]);
        streamWriter.WriteLine("ElementType = MET_USHORT");
        streamWriter.WriteLine("ElementDataFile = " + fileName + ".raw");

        streamWriter.Close();
    }

    private void InitializeBases()
    {
        this.baseXTransformed = new Point3D(1, 0, 0)
            .Rotate(transformation.RotationMatrix)
            .ToVector();
        this.baseYTransformed = new Point3D(0, 1, 0)
            .Rotate(transformation.RotationMatrix)
            .ToVector();
        this.baseZTransformed = new Point3D(0, 0, 1)
            .Rotate(transformation.RotationMatrix)
            .ToVector();
    }

    private Vector<double> GetCurrentCoordinates(double x, double y, double z)
    {
        return x * baseXTransformed + y * baseYTransformed + z * baseZTransformed + transformation.TranslationVector;
    }

    private void MakeBinaryFile()
    {
        ushort currentValue;

        Vector<double> currentCoordinates;

        int numberX = 0, numberY = 0, numberZ = 0;

        const int BYTES_PER_POINT = 2;

        byte[] buffer = GetDataBuffer(Measures[0], Measures[1], Measures[2], numberX, numberY, numberZ, BYTES_PER_POINT);
        int index = 0;

        for (numberZ = 0; numberZ < Measures[2]; numberZ++)
        {
            for (numberY = 0; numberY < Measures[1]; numberY++)
            {
                currentCoordinates = GetCurrentCoordinates(0, numberY * Spacing[1], numberZ * Spacing[2]);

                for (numberX = 0; numberX < Measures[0]; numberX++, currentCoordinates += Spacing[0] * baseXTransformed)
                {
                    //USHORT is used, thus 2^16-1 is used for max value
                    if (sourceObject.PointWithinBounds(currentCoordinates[0], currentCoordinates[1], currentCoordinates[2]))
                        currentValue = (ushort)Math.Min(
                            sourceObject.GetValue(currentCoordinates[0], currentCoordinates[1], currentCoordinates[2]),
                            ushort.MaxValue
                        );

                    else
                        currentValue = OUT_OF_BOUNDS;

                    // Convert ushort to bytes and add to buffer
                    buffer[index++] = (byte)(currentValue & 0xFF);
                    buffer[index++] = (byte)((currentValue >> 8) & 0xFF);

                    if (index != buffer.Length)
                        continue;

                    binaryWriter.Write(buffer);
                    buffer = GetDataBuffer(Measures[0], Measures[1], Measures[2], numberX, numberY, numberZ, BYTES_PER_POINT);
                    index = 0;
                }
            }
        }

        binaryWriter.Close();
    }

    /// <summary>
    /// Method allocates highest possible buffer size for data to be saved
    /// </summary>
    /// <param name="dimensionX">Number of sample points in axis X</param>
    /// <param name="dimensionY">Number of sample points in axis Y</param>
    /// <param name="dimensionZ">Number of sample points in axis Z</param>
    /// <param name="currentX">Order of sample in axis X</param>
    /// <param name="currentY">Order of sample in axis Y</param>
    /// <param name="currentZ">Order of sample in axis Z</param>
    /// <param name="BYTES_PER_POINT">Amount of bytes to save per point</param>
    /// <returns>Returns buffer for data to save</returns>
    private byte[] GetDataBuffer(int dimensionX, int dimensionY, int dimensionZ, int currentX, int currentY, int currentZ, int BYTES_PER_POINT)
    {
        // Calculate points remaining to process
        long totalPoints = (long)dimensionX * dimensionY * dimensionZ;
        long processedPoints = ((long)currentZ * dimensionX * dimensionY) +
                               ((long)currentY * dimensionX) +
                               currentX;
        long remainingPoints = totalPoints - processedPoints;

        // Define a reasonable buffer size (e.g., 1 MB or size for remaining points, whichever is smaller)
        const int MAX_BUFFER_SIZE = 1024 * 1024; // 1 MB
        int pointsInBuffer = (int)Math.Min(remainingPoints, MAX_BUFFER_SIZE / BYTES_PER_POINT);

        // Ensure at least one point fits in the buffer
        pointsInBuffer = Math.Max(1, pointsInBuffer);

        return new byte[pointsInBuffer * BYTES_PER_POINT];
    }
}
