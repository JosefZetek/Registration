using System;
using System.Collections.Generic;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.FeatureNormalization;

/// <summary>
/// Rank based (empirical CDF) feature normalizer. Every feature is mapped to its
/// rank fraction within the corresponding set, so each normalized feature lies in
/// [0, 1] regardless of the original distribution. Unlike z-normalization this is
/// insensitive to outliers and heavy tails, and it aligns the micro and macro
/// feature distributions even when their value ranges differ.
/// </summary>
public class FeatureNormalizerRank
{
    /* Per-feature sorted values; index [featureIndex][sampleIndex] */
    private readonly double[][] sortedValuesMicro;
    private readonly double[][] sortedValuesMacro;

    public FeatureNormalizerRank(List<FeatureVector> featureVectorsMicro, List<FeatureVector> featureVectorsMacro)
    {
        this.sortedValuesMicro = CalculateSortedValues(featureVectorsMicro);
        this.sortedValuesMacro = CalculateSortedValues(featureVectorsMacro);
    }

    private double[][] CalculateSortedValues(List<FeatureVector> featureVectors)
    {
        int numberOfFeatures = featureVectors[0].GetNumberOfFeatures;

        double[][] sortedValues = new double[numberOfFeatures][];

        for (int i = 0; i < numberOfFeatures; i++)
        {
            sortedValues[i] = new double[featureVectors.Count];

            for (int j = 0; j < featureVectors.Count; j++)
                sortedValues[i][j] = featureVectors[j].Features[i];

            Array.Sort(sortedValues[i]);
        }

        return sortedValues;
    }

    public FeatureVector Normalize(FeatureVector featureVector, bool isMicro)
    {
        double[][] sortedValues = isMicro ? sortedValuesMicro : sortedValuesMacro;

        double[] features = new double[featureVector.GetNumberOfFeatures];

        for (int i = 0; i < features.Length; i++)
            features[i] = RankFraction(sortedValues[i], featureVector.Features[i]);

        return new FeatureVector(featureVector.Point, features);
    }

    public List<FeatureVector> NormalizeList(List<FeatureVector> featureVectors, bool isMicro)
    {
        List<FeatureVector> normalizedList = new List<FeatureVector>(featureVectors.Count);

        for (int i = 0; i < featureVectors.Count; i++)
            normalizedList.Add(Normalize(featureVectors[i], isMicro));

        return normalizedList;
    }

    /// <summary>
    /// Maps a value to its rank fraction in [0, 1] within the sorted reference values.
    /// Ties get the average of their ranks so identical values normalize identically.
    /// </summary>
    private static double RankFraction(double[] sortedValues, double value)
    {
        int firstIndex = LowerBound(sortedValues, value);
        int lastIndex = UpperBound(sortedValues, value);

        double averageRank = (firstIndex + lastIndex) / 2.0;

        return averageRank / sortedValues.Length;
    }

    /// <summary>Index of the first element >= value</summary>
    private static int LowerBound(double[] sortedValues, double value)
    {
        int low = 0, high = sortedValues.Length;

        while (low < high)
        {
            int mid = (low + high) / 2;

            if (sortedValues[mid] < value)
                low = mid + 1;
            else
                high = mid;
        }

        return low;
    }

    /// <summary>Index of the first element > value</summary>
    private static int UpperBound(double[] sortedValues, double value)
    {
        int low = 0, high = sortedValues.Length;

        while (low < high)
        {
            int mid = (low + high) / 2;

            if (sortedValues[mid] <= value)
                low = mid + 1;
            else
                high = mid;
        }

        return low;
    }
}
