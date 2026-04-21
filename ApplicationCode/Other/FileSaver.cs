using System;
using System.IO;
using Registration.ApplicationCode.DataClasses.Data;

namespace Registration.ApplicationCode.Other;

public class FileSaver
{

    private const int DIMENSIONS = 3;

    private BinaryWriter binaryWriter;
    private StreamWriter streamWriter;

    private string fileName;
    private AMockObject d;

    /// <summary>
    /// Class saves artificial data to a given directory
    /// </summary>
    /// <param name="directory">Directory where data are going to be saved</param>
    /// <param name="fileName">Name of a file (without extension)</param>
    /// <param name="d">Data to be saved</param>
    /// <exception cref="ArgumentException"></exception>
    public FileSaver(string directory, string fileName, AMockObject d)
    {
        this.fileName = fileName;
        this.d = d;

        if (d == null)
            throw new ArgumentException("No data were passed");

        if (d.Measures == null)
            throw new ArgumentException("Measures are not specified");

        if (d.Measures.Length != DIMENSIONS)
            throw new ArgumentException("Data need to be " + DIMENSIONS + " dimensional");

        if (d.Measures[0] <= 0 || d.Measures[1] <= 0 || d.Measures[2] <= 0)
            throw new ArgumentException("None of the dimensions can be negative or zero");

        try
        {
            binaryWriter = new BinaryWriter(new FileStream(directory + fileName + ".raw", FileMode.Create));
            streamWriter = new StreamWriter(directory + fileName + ".mhd");
        }
        catch (IOException e) { throw e; }
    }

    public void MakeFiles()
    {
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
        streamWriter.WriteLine("ElementSpacing = {0} {1} {2}", d.XSpacing, d.YSpacing, d.ZSpacing);
        streamWriter.WriteLine("DimSize = {0} {1} {2}", d.Measures[0], d.Measures[1], d.Measures[2]);
        streamWriter.WriteLine("ElementType = MET_USHORT");
        streamWriter.WriteLine("ElementDataFile = " + fileName + ".raw");

        streamWriter.Close();
    }

    private void MakeBinaryFile()
    {
        ushort currentValue;
        double currentX, currentY, currentZ = 0;
        int numberX = 0, numberY = 0, numberZ = 0;

        // const int BYTES_PER_POINT = 2;

        // byte[] buffer = GetDataBuffer(d.Measures[0], d.Measures[1], d.Measures[2], numberX, numberY, numberZ, BYTES_PER_POINT);
        // int index = 0;

        for (numberZ = 0; numberZ < d.Measures[2]; numberZ++, currentZ += d.ZSpacing)
        {
            currentY = 0;
            for (numberY = 0; numberY < d.Measures[1]; numberY++, currentY += d.YSpacing)
            {
                currentX = 0;
                for (numberX = 0; numberX < d.Measures[0]; numberX++, currentX += d.XSpacing)
                {
                    //USHORT is used, thus 2^16-1 is used for max value
                    currentValue = (ushort)Math.Min(d.GetValue(currentX, currentY, currentZ), ushort.MaxValue);
                    
                    binaryWriter.Write((byte)(currentValue & 0xFF));
                    binaryWriter.Write((byte)((currentValue >> 8) & 0xFF));
                    // Convert ushort to bytes and add to buffer
                    // buffer[index++] = (byte)(currentValue & 0xFF);
                    // buffer[index++] = (byte)((currentValue >> 8) & 0xFF);
                    //
                    // if (index != buffer.Length)
                    //     continue;
                    //
                    // binaryWriter.Write(buffer);
                    // buffer = GetDataBuffer(d.Measures[0], d.Measures[1], d.Measures[2], numberX, numberY, numberZ, BYTES_PER_POINT);
                    // index = 0;
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
