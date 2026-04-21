using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Registration.ApplicationCode.DataClasses.Data;

public class Data
{
    private double[] elementSpacing; // three double values indicating distance between two points in concrete direction
    private int[] dimSize; // three int values indicating the "length" of the side of the cuboid in concerete direction
    private string elementDataFile; // the name of the file .raw (with data)
    private string elementType; // USHORT(2 bytes)/UCHAR(1 byte)

    /// <summary>
    /// 
    /// </summary>
    public Data(string fileName)  
    {
        elementSpacing = null;
        dimSize = null;
        elementDataFile = null;
        elementType = null;

        this.SetFeatures(fileName);
    }

    public Data(Data copy)
    {
        this.elementSpacing = copy.ElementSpacing;
        this.dimSize = copy.DimSize;
        this.elementDataFile = copy.ElementDataFile;
        this.elementType = copy.ElementType;
    }

    /// <summary>
    /// Function fetches data type information from a file passed
    /// </summary>
    /// <param name="fileName">Path to a file where to read the information</param>
    private void SetFeatures(string fileName) // method setting instance atributes
    {
        
        
        using (StreamReader sr = new StreamReader(fileName))
        {
            string newLine;

            Regex regex;
            MatchCollection matches;

            
            while ((newLine = sr.ReadLine()) != null)
            {
                if (newLine.Contains("ElementSpacing"))
                {
                    regex = new Regex(@"(\d+([,.]\d+)?)"); //Pattern for searching decimal and whole numbers within the string
                    matches = regex.Matches(newLine);

                    elementSpacing = new double[matches.Count];

                    for(int i = 0; i<elementSpacing.Length; i++)
                        ElementSpacing[i] = GetDouble(matches[i].ToString().Replace(",", "."), -1234);
                }

                else if (newLine.Contains("DimSize"))
                {
                    regex = new Regex(@"\d+"); //Patern for searching whole numbers within the string
                    matches = regex.Matches(newLine);

                    dimSize = new int[matches.Count];

                    for (int i = 0; i < dimSize.Length; i++)
                        DimSize[i] = int.Parse(matches[i].ToString());
                }

                else if (newLine.Contains("ElementType"))
                {
                    regex = new Regex(@"(?<==\s*)\S+"); //Pattern for matching words after = sign
                    matches = regex.Matches(newLine);

                    if(matches.Count == 0)
                        throw new Exception("ElementType not specified in the .mhd file");

                    elementType = matches[0].ToString();
                }

                else if (newLine.Contains("ElementDataFile"))
                {
                    regex = new Regex(@"(?<==\s*)\S+"); //Pattern for matching words after = sign
                    matches = regex.Matches(newLine);

                    if (matches.Count == 0)
                        throw new Exception("ElementDataFile not specified in the .mhd file");

                    elementDataFile = matches[0].ToString();
                }
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    private static double GetDouble(string value, double defaultValue)
    {
        // Try parsing in the current culture
        if (!double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out double result) &&

            !double.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("cs-CZ"), out result) &&

            // Then try in US english
            !double.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out result) &&
            // Then in neutral language
            !double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
        {
            result = defaultValue;
        }
        return result;
    }

    public double[] ElementSpacing { get => elementSpacing; }
    public int[] DimSize { get => dimSize; set => dimSize = value; }
    public string ElementDataFile { get => elementDataFile; }
    public string ElementType { get => elementType; }

}