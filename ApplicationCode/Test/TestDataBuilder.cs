using System;
using System.IO;

namespace Registration.ApplicationCode.Test;

public class TestDataBuilder
{
    public void BuildData(string cylinderRotation)
    {

        double[] elementSpacing = new double[3];
        int[] dimSize = new int[3];
        //string elementDataFile = null;
        string elementType = null;//Tyhle 4 parametry jeste musim doplnit

        elementSpacing[0] = 0.053059;//xspacing
        elementSpacing[1] = 0.053059;//yspacing
        elementSpacing[2] = 0.053058;//zspacing

        dimSize[0] = 1012;
        dimSize[1] = 1024;
        dimSize[2] = 1014;

        elementType = "MET_UCHAR";

        string fileName = cylinderRotation;

        CreateDataFile(fileName, dimSize, cylinderRotation);
        CreateMetaDataFile(fileName, elementType);
    }

    public void CreateDataFile(string fileName, int[] dimSize, string cylinderRotation)
    {
        BinaryWriter bw;
        try {
             bw = new BinaryWriter(new FileStream(string.Format("{0}{1}{2}.raw", Environment.CurrentDirectory, System.IO.Path.DirectorySeparatorChar, fileName), FileMode.Create));
        }
        catch (IOException e)
        {
            Console.WriteLine(e.Message + "\n Cannot create file.");
            return;
        }

        //WriteConstantToFile(bw, dimSize, 100);
        //WriteConstantsToHalves(bw, dimSize, 0, 255);
        //int[] center = { dimSize[0] / 2, dimSize[1] / 2, dimSize[2] / 2 };
        int[] center = { 50, 50, 50 };
        WriteCylinder(bw, dimSize, center, 50, 0, 127, cylinderRotation);
        bw.Close();
    }

    private void WriteCylinder(BinaryWriter bw, int[] dimSize, int[] center, double radius, byte c1, byte c2, string rotation)
    {
        double distance;

        int width = dimSize[0];//x
        int depth = dimSize[1];//y
        int height = dimSize[2];//z

        try
        {
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < depth; y++)
                    {
                        if (rotation.Equals("x"))
                        {
                            distance = Math.Sqrt(Math.Pow(y - center[1], 2) + Math.Pow(z - center[2], 2));
                            
                        }
                        else if(rotation.Equals("y"))
                        {
                            distance = Math.Sqrt(Math.Pow(x - center[0], 2) + Math.Pow(z - center[2], 2));

                        }
                        else if (rotation.Equals("z"))
                        {
                            distance = Math.Sqrt(Math.Pow(x - center[0], 2) + Math.Pow(y - center[1], 2));

                        }
                        else
                        {
                            throw new ArgumentException();
                        }

                        if (distance < radius)
                            bw.Write(c1);
                        else
                            bw.Write(c2);
                  
                    }
                }
            }
        }
        catch (IOException e)
        {
            Console.WriteLine(e.Message + "\n Cannot write to file.");
            return;
        }
    }

    /// <summary>
    /// Writes a circle of colour c1, everything else is c2
    /// </summary>
    /// <param name="bw"></param>
    /// <param name="dimSize"></param>
    /// <param name="c1"></param>
    /// <param name="c2"></param>
    private void WriteCircle(BinaryWriter bw, int[] dimSize, int[] center, double diameter, byte c1, byte c2)
    {
        double distance;
        double thickness = 10;
        try
        {
            for (int i = 0; i < dimSize[0]; i++)
            {
                for (int j = 0; j < dimSize[1]; j++)
                {
                    for (int k = 0; k < dimSize[2]; k++)
                    {

                        
                        distance = Math.Sqrt(Math.Pow(i - center[0], 2) + Math.Pow(j - center[1], 2) + Math.Pow(k - center[2], 2));
                        if ((Math.Abs(distance - diameter) < thickness) && (dimSize[2]/2 -5 < k) && (k < dimSize[2] / 2 + 5)) 
                            bw.Write(c1);
                        else
                            bw.Write(c2);
                            
                    }
                }
            }
        }
        catch (IOException e)
        {
            Console.WriteLine(e.Message + "\n Cannot write to file.");
        }
    }

    private void WriteConstantsToHalves(BinaryWriter bw, int[] dimSize, byte c1, byte c2)
    {
        try
        {
            for (int i = 0; i < dimSize[0]; i++)
            {
                for (int j = 0; j < dimSize[1]; j++)
                {
                    for (int k = 0; k < dimSize[2]; k++)
                    {
                        if (i < dimSize[0] / 2)
                            bw.Write(c1);
                        else
                            bw.Write(c2);
                    }
                }
            }
        }
        catch (IOException e)
        {
            Console.WriteLine(e.Message + "\n Cannot write to file.");
        }
    }

    /// <summary>
    /// Sets the whole image to a constant color
    /// </summary>
    /// <param name="c"></param>
    private void WriteConstantToFile(BinaryWriter bw, int[] dimSize, byte c)
    {
        try
        {
            for (int i = 0; i < dimSize[0]; i++)
            {
                for (int j = 0; j < dimSize[1]; j++)
                {
                    for (int k = 0; k < dimSize[2]; k++)
                    {
                        bw.Write(c);
                    }
                }
            }
        }
        catch (IOException e)
        {
            Console.WriteLine(e.Message + "\n Cannot write to file.");
        }
    }

    private void CreateMetaDataFile(string fileName, string elementType)
    {
        string[] lines =
            {
            "ObjectType = Image", "NDims = 3",
            "BinaryData = True", "BinaryDataByteOrderMSB = False",
            "CompressedData = False", "TransformMatrix = 1 0 0 0 1 0 0 0 1",
            "Offset = 0 0 0", "CenterOfRotation = 0 0 0",
            "AnatomicalOrientation = RAI", "ElementSpacing = 0.053059 0.053059 0.053058",
            "DimSize = 1012 1024 1014", string.Format("ElementType = {0}", elementType),
            string.Format("ElementDataFile = {0}.raw", fileName) 
        };
        File.WriteAllLines(string.Format("{0}{1}{2}.mhd", Environment.CurrentDirectory, System.IO.Path.DirectorySeparatorChar, fileName), lines);

    }

}