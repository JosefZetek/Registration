using Newtonsoft.Json;

namespace Registration.ApplicationCode.Other.FCConfiguration;

[System.Serializable]
[JsonConverter(typeof(ParameterConverter))]
public class Parameter
{
    public ParameterType Type;
    
    // Single values
    public string StringValue;
    public double DoubleValue;
    public int IntValue;
    public bool BoolValue;
    
    // Array values
    public double[] DoubleArray;
    public int[] IntArray;
    public bool[] BoolArray;

    public override string ToString()
    {
        switch (Type)
        {
            case ParameterType.String:
                return StringValue;
            case ParameterType.Double:
                return $"{DoubleValue}";
            case ParameterType.Int:
                return $"{IntValue}";
            case ParameterType.Bool:
                return $"{BoolValue}";
            case ParameterType.DoubleArray:
                return $"[{string.Join(", ", DoubleArray)}]";
            case ParameterType.IntArray:
                return $"[{string.Join(", ", IntArray)}]";
            case ParameterType.BoolArray:
                return $"[{string.Join(", ", BoolArray)}]";
            default:
                return $"Unknown Type";
        }
    }
}