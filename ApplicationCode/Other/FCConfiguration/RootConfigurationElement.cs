using System;
using System.Collections.Generic;
using System.IO;
using DataView;
using Newtonsoft.Json;
using Registration.ApplicationCode.FeatureComputers;

namespace Registration.ApplicationCode.Other.FCConfiguration;

[System.Serializable]
public class RootConfigurationElement
{
    public List<FCConfiguration> configurations;
        
    public RootConfigurationElement()
    {
        this.configurations = new List<FCConfiguration>();
    }
    
    public CompoundFeatureComputer GetCompoundFeatureComputer()
    {
        var featureComputers = new List<AFeatureComputer>();
        foreach (var config in configurations)
        {
            var fc = config.GetFeatureComputer();
            if (fc != null)
                featureComputers.Add(fc);
        }
        return new CompoundFeatureComputer(featureComputers.ToArray());
    }
        
    public void Serialize(string path, string fileName)
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
            
            string convertedJson = JsonConvert.SerializeObject(this, settings);
            Console.WriteLine(convertedJson);
            File.WriteAllText(Path.Combine(path, fileName + ".json"), convertedJson);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Serialization failed: {ex.Message}");
        }
    }
    
    public static RootConfigurationElement? Deserialize(string filePath)
    {
        try
        {
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<RootConfigurationElement>(json);
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Deserialization failed: {ex.Message}");
            return null;
        }
    }
        
        
}