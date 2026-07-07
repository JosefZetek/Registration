using System;
using System.Collections.Generic;
using ExCSS;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.Other.FCConfiguration;
using Registration.ApplicationCode.Samplers;

namespace Registration.ApplicationCode.FeatureComputers;

public class FeatureComputerQuantiles : AFeatureComputer
{
    private double[] quantileValues;

    public FeatureComputerQuantiles(double[] quantileValues)
    {
        this.quantileValues = quantileValues;
    }

    private static List<double> CalculateValues(List<Point3D> points, AData d)
    {
        List<double> values = new List<double>();
        double min = double.MaxValue, max = double.MinValue;
        
        for (int i = 0; i < points.Count; i++)
        {
            values.Add(d.GetValue(points[i]));
            min = Math.Min(min, values[values.Count - 1]);
            max = Math.Max(max, values[values.Count - 1]);
        }

        if (Math.Abs(min - max) < Double.Epsilon)
        {
            Console.WriteLine("Basis cannot be calculated because all sampled values in the point surrounding are the same.");
            return null;
        }

        return values;
    }

    public override int NumberOfFeatures => quantileValues.Length;

    public override void ComputeFeatureVector(AData d, Point3D p, double[] array, int startIndex)
    {
        List<Point3D> points = UniformSphereSampler.GetDistributedPoints(d, p);
        List<double> values = CalculateValues(points, d);
        if(values == null)
        {
            /* NaN marks the vector as invalid; such vectors are dropped before
               normalization so a single flat neighborhood cannot poison the statistics. */
            for (int i = 0; i < NumberOfFeatures; i++)
                array[startIndex + i] = double.NaN;

            return;
        }
        
        /* Threshold to filter insignificant  values */
        QuickSelectClass quickSelectClass = new QuickSelectClass();
        for (int i = 0; i < quantileValues.Length; i++)
        {
            int k = (int)(quantileValues[i] * (values.Count - 1));
            double quantileValue = quickSelectClass.QuickSelect(values, k);
            array[startIndex + i] = quantileValue;
        }
    }

    public override void GetWeights(double[] array, int startIndex)
    {
        for (int i = 0; i < NumberOfFeatures; i++)
            array[startIndex + i] = 1.0/NumberOfFeatures;
    }

    public override FCConfiguration GetConfiguration()
    {
        var config = new FCConfiguration();
        config.FeatureComputerType = FCType.QUANTILES_FEATURE_COMPUTER;
        config.AddParameter("QuantileValues", quantileValues);
        config.AddParameter("Comment", "Quantiles");
        return config;
    }
}