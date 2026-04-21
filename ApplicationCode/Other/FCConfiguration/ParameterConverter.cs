using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Registration.ApplicationCode.Other.FCConfiguration;

public class ParameterConverter : JsonConverter<Parameter>
{
    public override void WriteJson(JsonWriter writer, Parameter value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("Value");
        
        switch (value.Type)
        {
            case ParameterType.String:
                writer.WriteValue(value.StringValue);
                break;
            case ParameterType.Double:
                writer.WriteValue(value.DoubleValue);
                break;
            case ParameterType.Int:
                writer.WriteValue(value.IntValue);
                break;
            case ParameterType.Bool:
                writer.WriteValue(value.BoolValue);
                break;
            case ParameterType.DoubleArray:
                serializer.Serialize(writer, value.DoubleArray);
                break;
            case ParameterType.IntArray:
                serializer.Serialize(writer, value.IntArray);
                break;
            case ParameterType.BoolArray:
                serializer.Serialize(writer, value.BoolArray);
                break;
        }
        
        writer.WriteEndObject();
    }
    
    public override Parameter ReadJson(JsonReader reader, Type objectType, Parameter existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);
        var param = new Parameter();
        
        var valueToken = obj["Value"];
        
        if (valueToken == null) return param;
        
        // Determine type based on JSON token type
        switch (valueToken.Type)
        {
            case JTokenType.Float:
                param.Type = ParameterType.Double;
                param.DoubleValue = valueToken.Value<double>();
                break;
            case JTokenType.Integer:
                param.Type = ParameterType.Int;
                param.IntValue = valueToken.Value<int>();
                break;
            case JTokenType.Boolean:
                param.Type = ParameterType.Bool;
                param.BoolValue = valueToken.Value<bool>();
                break;
            case JTokenType.Array:
                var array = (JArray)valueToken;
                if (array.Count > 0)
                {
                    var firstElement = array[0];
                    if (firstElement.Type == JTokenType.Float)
                    {
                        param.Type = ParameterType.DoubleArray;
                        param.DoubleArray = array.ToObject<double[]>();
                    }
                    else if (firstElement.Type == JTokenType.Integer)
                    {
                        param.Type = ParameterType.IntArray;
                        param.IntArray = array.ToObject<int[]>();
                    }
                    else if (firstElement.Type == JTokenType.Boolean)
                    {
                        param.Type = ParameterType.BoolArray;
                        param.BoolArray = array.ToObject<bool[]>();
                    }
                }
                break;
        }
        
        return param;
    }
}