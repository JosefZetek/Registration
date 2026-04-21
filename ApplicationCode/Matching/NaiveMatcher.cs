using System;
using System.Collections.Generic;
using System.Linq;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Matching;

public class NaiveMatcher : IMatcher
{
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
            currentScore = Similarity(featureVectorMacro, featureVectorsMicro[i]);
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
    /// <param name="f1"></param>
    /// <param name="f2"></param>
    /// <returns></returns>
    private double Similarity(FeatureVector f1, FeatureVector f2)
    {
        double num = 0;
        double denom = f1.Magnitude() * f2.Magnitude();

        if (f1.Features.Length != f2.Features.Length)
            return 0;

        for (int i = 0; i < f1.Features.Length; i++)
            num += f1.Features[i] * f2.Features[i];


        double s = num / denom * 100;
        return (s < 0) ? 0 : s;
    }
}