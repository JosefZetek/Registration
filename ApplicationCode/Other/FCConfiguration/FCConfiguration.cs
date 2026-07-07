using System;
using System.Collections.Generic;
using Registration.ApplicationCode.FeatureComputers;

namespace Registration.ApplicationCode.Other.FCConfiguration;

[Serializable]
public class FCConfiguration
{
    public FCType FeatureComputerType;
    public Dictionary<string, Parameter> Parameters;
    
    public FCConfiguration()
    {
        this.Parameters = new Dictionary<string, Parameter>();
    }

    public AFeatureComputer GetFeatureComputer()
    {
        switch (FeatureComputerType)
        {
            case FCType.GRADIENT_FEATURE_COMPUTER:
                
                if (!Parameters.ContainsKey("BorderPercentage") || 
                    !Parameters.ContainsKey("Radius"))
                    return null;
                
                double borderPercentage = Parameters["BorderPercentage"].DoubleValue;
                int radius = Parameters["Radius"].IntValue;
                
                return new FeatureComputerGradient(borderPercentage, radius);
                
            case FCType.CURVATURE_FEATURE_COMPUTER:
                
                if (!Parameters.ContainsKey("BorderPercentage") || 
                    !Parameters.ContainsKey("Radius"))
                    return null;
                
                borderPercentage = Parameters["BorderPercentage"].DoubleValue;
                radius = Parameters["Radius"].IntValue;
                
                return new FeatureComputerISOCurvature(borderPercentage, radius);
                    
            
            case FCType.QUANTILES_FEATURE_COMPUTER:
                if (!Parameters.ContainsKey("QuantileValues"))
                    return null;
                
                double[] quantileValues = Parameters["QuantileValues"].DoubleArray;
                
                if (quantileValues == null || quantileValues.Length == 0)
                    return null;

                return new FeatureComputerQuantiles(quantileValues);
            
            case FCType.SHAPE_INDEX_FEATURE_COMPUTER:

                if (!Parameters.ContainsKey("BorderPercentage") ||
                    !Parameters.ContainsKey("Radius"))
                    return null;

                borderPercentage = Parameters["BorderPercentage"].DoubleValue;
                radius = Parameters["Radius"].IntValue;

                return new FeatureComputerShapeIndex(borderPercentage, radius);

            case FCType.PCA_FEATURE_COMPUTER:
                return new FeatureComputerPCALength();
            
            case FCType.POINT_VALUE_FEATURE_COMPUTER:
                return new FeatureComputerPointValue();
            
            case FCType.SPHERICAL_HARMONICS_FEATURE_COMPUTER:
                if (!Parameters.ContainsKey("lMax") ||
                    !Parameters.ContainsKey("radius") ||
                    !Parameters.ContainsKey("sampleCount"))
                    return null;

                int lMax = Parameters["lMax"].IntValue;
                double sphericalRadius = Parameters["radius"].DoubleValue;
                int sampleCount = Parameters["sampleCount"].IntValue;

                return new FeatureComputerSphericalHarmonics(lMax, sphericalRadius, sampleCount);
        }

        return null;
    }

    #region Setters

    public void AddParameter(string key, string value)
    {
        Parameters.Add(key, new Parameter { Type = ParameterType.String, StringValue = value });
    }
    public void AddParameter(string key, double value)
    {
        Parameters.Add(key, new Parameter { Type = ParameterType.Double, DoubleValue = value });
    }
    
    public void AddParameter(string key, int value)
    {
        Parameters.Add(key, new Parameter { Type = ParameterType.Int, IntValue = value });
    }
    
    public void AddParameter(string key, bool value)
    {
        Parameters.Add(key, new Parameter { Type = ParameterType.Bool, BoolValue = value });
    }
    
    public void AddParameter(string key, double[] values)
    {
        Parameters.Add(key, new Parameter { Type = ParameterType.DoubleArray, DoubleArray = values });
    }
    
    public void AddParameter(string key, int[] values)
    {
        Parameters.Add(key, new Parameter { Type = ParameterType.IntArray, IntArray = values });
    }
    
    public void AddParameter(string key, bool[] values)
    {
        Parameters.Add(key, new Parameter { Type = ParameterType.BoolArray, BoolArray = values });
    }

    #endregion

    #region Getters
    public double GetDouble(string key)
    {
        return Parameters[key].DoubleValue;
    }
    
    public int GetInt(string key)
    {
        return Parameters[key].IntValue;
    }
    
    public bool GetBool(string key)
    {
        return Parameters[key].BoolValue;
    }
    
    public double[] GetDoubleArray(string key)
    {
        return Parameters[key].DoubleArray;
    }
    
    public int[] GetIntArray(string key)
    {
        return Parameters[key].IntArray;
    }
    
    public bool[] GetBoolArray(string key)
    {
        return Parameters[key].BoolArray;
    }
    #endregion
    
}
