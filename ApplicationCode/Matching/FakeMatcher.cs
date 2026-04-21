using System;
using System.Collections.Generic;
using System.Linq;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Matching;

public class FakeMatcher: IMatcher
{
    private Transform3D expectedTransformation;

    public FakeMatcher(Transform3D expectedTransformation)
    {
        this.expectedTransformation = expectedTransformation;
    }

    private Match FindBestMatchMicro(FeatureVector featureVectorMicro, FeatureVector[] featureVectorsMacro)
    {
        double bestScore = Double.MinValue;
        double currentScore;
        FeatureVector bestFeatureVector = null;

        for (int i = 0; i<featureVectorsMacro.Length; i++)
        {
            currentScore = Similarity(featureVectorMicro, featureVectorsMacro[i]);
            if (currentScore > bestScore)
            {
                bestFeatureVector = featureVectorsMacro[i];
                bestScore = currentScore;
            }
        }

        return new Match(featureVectorMicro, bestFeatureVector, bestScore);
    }

    private Match FindBestMatchMacro(FeatureVector featureVectorMacro, FeatureVector[] featureVectorsMicro)
    {
        double bestScore = Double.MinValue;
        double currentScore;
        FeatureVector bestFeatureVector = null;

        for (int i = 0; i < featureVectorsMicro.Length; i++)
        {
            currentScore = Similarity(featureVectorsMicro[i], featureVectorMacro);
            if (currentScore > bestScore)
            {
                bestScore = currentScore;
                bestFeatureVector = featureVectorsMicro[i];
            }
        }

        return new Match(bestFeatureVector, featureVectorMacro, bestScore);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="microFV"></param>
    /// <param name="macroFV"></param>
    /// <returns></returns>
    private double Similarity(FeatureVector microFV, FeatureVector macroFV)
    {
        return -microFV.Point.ApplyRotationTranslation(expectedTransformation).Distance(macroFV.Point);
    }

    public Match[] Match(FeatureVector[] featureVectorsMicro, FeatureVector[] featureVectorsMacro, double threshold)
    {
        List<Match> matches = new List<Match>();

        if (featureVectorsMicro.Length > featureVectorsMacro.Length)
        {
            for (int i = 0; i < featureVectorsMacro.Length; i++)
                matches.Add(FindBestMatchMacro(featureVectorsMacro[i], featureVectorsMicro));
        }

        else
        {
            for (int i = 0; i < featureVectorsMicro.Length; i++)
                matches.Add(FindBestMatchMicro(featureVectorsMicro[i], featureVectorsMacro));
        }

        matches.Sort((x, y) => x.Similarity.CompareTo(y.Similarity));
        int numberOfMatches = (int)(matches.Count / 100.0 * threshold); //takes top [threshold] %

        Match[] matchesReturn = new Match[numberOfMatches];
        int j = 0;
        for (int i = matches.Count - 1; i > matches.Count - 1 - numberOfMatches; i--) //takes top [threshold] % from back (adscending order)
        {
            matchesReturn[j] = matches.ElementAt(i);
            j++;
        }

        return matchesReturn;
    }

    public Match[] Match(FeatureVector[] featureVectorsMicro, FeatureVector[] featureVectorsMacro)
    {
        return Match(featureVectorsMicro, featureVectorsMacro, 10);
    }
}