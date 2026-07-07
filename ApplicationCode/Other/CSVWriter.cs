using System.Collections.Generic;
using System.IO;

namespace Registration.ApplicationCode.Other;

public class CSVWriter
{
    /// <summary>
    /// Writes 2D array into a CSV file
    /// </summary>
    /// <param name="fileName">Name of a file (including path and extension)</param>
    /// <param name="labelX">Header for the X axis</param>
    /// <param name="labelY">Header for the Y axis</param>
    /// <param name="listOfPoints">List of points to be printed out</param>
    public static void WriteResult(string fileName, string labelX, string labelY, List<Point2D> listOfPoints)
    {
        StreamWriter writer = new StreamWriter(fileName);
        writer.WriteLine("{0}; {1}", labelX, labelY);

        foreach (Point2D point in listOfPoints)
        {
            writer.WriteLine("{0}; {1}", point.X, point.Y);
        }

        writer.Flush();
        writer.Close();
    }

    /// <summary>
    /// Writes 2D array into a CSV file
    /// </summary>
    /// <param name="fileName">Name of a file (including path and extension)</param>
    /// <param name="labelX">Header for the X axis</param>
    /// <param name="labelY">Header for the Y axis</param>
    /// <param name="labelZ">Header for the Z axis</param>
    /// <param name="listOfPoints">List of points to be printed out</param>
    public static void WriteResult(string fileName, string labelX, string labelY, string labelZ, List<Point3D> listOfPoints)
    {
        StreamWriter writer = new StreamWriter(fileName);
        writer.WriteLine("{0}; {1}; {2}", labelX, labelY, labelZ);

        foreach (Point3D point in listOfPoints)
        {
            writer.WriteLine("{0}; {1}; {2}", point.X, point.Y, point.Z);
        }

        writer.Flush();
        writer.Close();
    }

    public static void WriteResult(string filename, string[][] values)
    {
        StreamWriter writer = new StreamWriter(filename);

        for (int i = 0; i < values.Length; i++)
        {
            for (int j = 0; j < values[i].Length; j++)
                writer.Write("{0}; ", values[i][j]);

            writer.Write("\n");
        }

        writer.Flush();
        writer.Close();
    }
}

