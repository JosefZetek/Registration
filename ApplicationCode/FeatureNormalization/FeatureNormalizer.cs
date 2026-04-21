using System.Collections.Generic;
using System;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.FeatureNormalization;

public class FeatureNormalizer
{
    private double[] meanValues;
    private double[] deviationValues;

    public FeatureNormalizer(List<FeatureVector> featureVectorsMicro, List<FeatureVector> featureVectorsMacro)
    {
        this.meanValues = CalculateMeanValues(featureVectorsMicro, featureVectorsMacro);
        this.deviationValues = CalculateDeviationValues(featureVectorsMicro, featureVectorsMacro);
    }

    private double[] CalculateMeanValues(List<FeatureVector> featureVectorsMicro, List<FeatureVector> featureVectorsMacro)
    {
        int NUMBER_OF_FEATURES = featureVectorsMicro[0].GetNumberOfFeatures;
        int NUMBER_OF_VECTORS = featureVectorsMicro.Count + featureVectorsMacro.Count;

        double[] meanValues = new double[NUMBER_OF_FEATURES];


        for(int i = 0; i<NUMBER_OF_FEATURES; i++)
        {
            double sum = 0;
            for (int j = 0; j < featureVectorsMicro.Count; j++)
                sum += featureVectorsMicro[j].Features[i];

            for (int j = 0; j < featureVectorsMacro.Count; j++)
                sum += featureVectorsMacro[j].Features[i];

            sum /= NUMBER_OF_VECTORS;
            meanValues[i] = sum;
        }

        return meanValues;
    }

    private double[] CalculateDeviationValues(List<FeatureVector> featureVectorsMicro, List<FeatureVector> featureVectorsMacro)
    {
        int NUMBER_OF_FEATURES = featureVectorsMicro[0].GetNumberOfFeatures;
        int NUMBER_OF_VECTORS = featureVectorsMicro.Count + featureVectorsMacro.Count;

        double[] meanValues = new double[NUMBER_OF_FEATURES];

        for (int i = 0; i < NUMBER_OF_FEATURES; i++)
        {
            double sum = 0;
            for (int j = 0; j < featureVectorsMicro.Count; j++)
                sum += Math.Pow((featureVectorsMicro[j].Features[i] - meanValues[i]), 2);

            for (int j = 0; j < featureVectorsMacro.Count; j++)
                sum += Math.Pow((featureVectorsMacro[j].Features[i] - meanValues[i]), 2);


            sum = Math.Sqrt(sum/NUMBER_OF_VECTORS);
            meanValues[i] = sum;
        }

        return meanValues;
    }

    public FeatureVector Normalize(FeatureVector featureVector)
    {
        double[] features = new double[featureVector.GetNumberOfFeatures];

        for (int i = 0; i < features.Length; i++)
            features[i] = (featureVector.Features[i] - meanValues[i]) / deviationValues[i];

        return new FeatureVector(featureVector.Point, features);
    }

    public List<FeatureVector> NormalizeList(List<FeatureVector> featureVectors)
    {
        List<FeatureVector> normalizedList = new List<FeatureVector>(featureVectors.Count);

        for (int i = 0; i < featureVectors.Count; i++)
            normalizedList.Add(Normalize(featureVectors[i]));

        return normalizedList;
    }

}