using System.Collections.Generic;
using System;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.FeatureNormalization;

public class FeatureNormalizer
{
    // private double[] meanValues;
    // private double[] deviationValues;

    private double[] meanValuesMicro;
    private double[] meanValuesMacro;
    
    private double[] deviationValuesMicro;
    private double[] deviationValuesMacro;
    

    public FeatureNormalizer(List<FeatureVector> featureVectorsMicro, List<FeatureVector> featureVectorsMacro)
    {
        this.meanValuesMicro = CalculateMeanValues(featureVectorsMicro);
        this.meanValuesMacro = CalculateMeanValues(featureVectorsMacro);
        
        this.deviationValuesMicro = CalculateDeviationValues(featureVectorsMicro, meanValuesMicro);
        this.deviationValuesMacro = CalculateDeviationValues(featureVectorsMacro, meanValuesMacro);
    }

    private double[] CalculateMeanValues(List<FeatureVector> featureVectors)
    {
        int numberOfFeatures = featureVectors[0].GetNumberOfFeatures;

        double[] meanValues = new double[numberOfFeatures];

        for(int i = 0; i<numberOfFeatures; i++)
        {
            for (int j = 0; j < featureVectors.Count; j++)
                meanValues[i] += featureVectors[j].Features[i]/featureVectors.Count;
        }

        return meanValues;
    }

    private double[] CalculateDeviationValues(List<FeatureVector> featureVectors, double[] meanValues)
    {
        int numberOfFeatures = featureVectors[0].GetNumberOfFeatures;
        
        double[] deviationValues = new double[numberOfFeatures];
        
        for (int i = 0; i < numberOfFeatures; i++)
        {
            double sum = 0;
            for (int j = 0; j < featureVectors.Count; j++)
                sum += Math.Pow(featureVectors[j].Features[i] - meanValues[i], 2);
            
            sum = Math.Sqrt(sum/featureVectors.Count);
            deviationValues[i] = sum;
        }

        return deviationValues;
    }

    public FeatureVector Normalize(FeatureVector featureVector, bool isMicro)
    {
        double[] features = new double[featureVector.GetNumberOfFeatures];
        
        double[] meanValues = isMicro ? meanValuesMicro : meanValuesMacro;  
        double[] deviationValues = isMicro ? deviationValuesMicro : deviationValuesMacro;
        
        for (int i = 0; i < features.Length; i++)
        {
            if(deviationValues[i] == 0)
                features[i] = 0;
            
            else
                features[i] = (featureVector.Features[i] - meanValues[i]) / deviationValues[i];
            
            
            
        }

        return new FeatureVector(featureVector.Point, features);
    }

    public List<FeatureVector> NormalizeList(List<FeatureVector> featureVectors, bool isMicro)
    {
        List<FeatureVector> normalizedList = new List<FeatureVector>(featureVectors.Count);

        for (int i = 0; i < featureVectors.Count; i++)
            normalizedList.Add(Normalize(featureVectors[i], isMicro));

        return normalizedList;
    }

}