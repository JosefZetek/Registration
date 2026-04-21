using System;
using System.Collections.Generic;
using System.Linq;
using DataView;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Matching;

public class RelativeMatcher : IMatcher
{
    public Match[] Match(FeatureVector[] featureVectorsMicro, FeatureVector[] featureVectorsMacro, double threshold)
    {
        List<Match> matches = new List<Match>();

        foreach (var micro in featureVectorsMicro)
        {
            int dimensions = micro.Features.Length;
            int[] scores = new int[featureVectorsMacro.Length];

            for (int d = 0; d < dimensions; d++)
            {
                List<int> rankedIndices = GetDimensionOrder(micro, featureVectorsMacro, d);
                for (int rank = 0; rank < rankedIndices.Count; rank++)
                {
                    scores[rankedIndices[rank]] += rank;
                }
            }

            // Find best match with lowest score
            int bestMatchIndex = 0;
            int bestScore = scores[0];
            for (int i = 1; i < scores.Length; i++)
            {
                if (scores[i] < bestScore)
                {
                    bestScore = scores[i];
                    bestMatchIndex = i;
                }
            }

            matches.Add(new Match(micro, featureVectorsMacro[bestMatchIndex], bestScore));
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
        return Match(featureVectorsMicro, featureVectorsMacro, 0.1); // No threshold
    }

    private List<int> GetDimensionOrder(FeatureVector micro, FeatureVector[] macros, int dimension)
    {
        double refValue = micro.Features[dimension];

        List<int> indices = new List<int>();
        for (int i = 0; i < macros.Length; i++)
            indices.Add(i);

        indices.Sort((a, b) =>
            Math.Abs(macros[a].Features[dimension] - refValue)
                .CompareTo(Math.Abs(macros[b].Features[dimension] - refValue)));

        return indices;
    }
}