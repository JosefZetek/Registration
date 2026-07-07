using Registration.ApplicationCode.Other;

using System.Collections.Generic;
using System.Linq;

namespace Registration.ApplicationCode.Matching;

public class PositionMatcher : IMatcher
{
    public Match[] Match(FeatureVector[] fMicro, FeatureVector[] fMacro, double threshold)
    {
        var prematches = new List<(Match LocalMatch, double SpatialVariance)>();

        for (int i = 0; i < fMicro.Length; i++)
        {
            var localMatches = FindMatchesWithVariance(fMicro[i], fMacro, 1.4);
            
            if(localMatches.Count == 0)
                continue;
            
            double sumX = 0, sumY = 0, sumZ = 0;
            foreach (var m in localMatches)
            {
                var pt = m.macroFV.Point;
                sumX += pt.X;
                sumY += pt.Y;
                sumZ += pt.Z;
            }
                
            int n = localMatches.Count;
            var avgPoint = new Point3D(sumX / n, sumY / n, sumZ / n);

            double posVariance = 0;
            foreach (var m in localMatches)
            {
                double dist = m.macroFV.Point.Distance(avgPoint);
                posVariance += dist * dist;
            }
            posVariance /= n;
            
            //At 0th index is the closest feature vector (ordered from the findmatcheswithvariance method)
            prematches.Add((localMatches[0], posVariance));
        }

        var sortedPrematches = prematches
            .OrderBy(p => p.SpatialVariance)
            .ToList();
        
        int keepCount = (int)System.Math.Ceiling(sortedPrematches.Count * threshold);

        // var spatialVariance = sortedPrematches
        //     .Take(keepCount)
        //     .Select(p => p.SpatialVariance)
        //     .ToList();
        
        // var realDistances = sortedPrematches
        //     .Take(keepCount)
        //     .Select(p => p.LocalMatch.macroFV.Point.Distance(p.LocalMatch.microFV.Point))
        //     .ToList();

        return sortedPrematches
            .Take(keepCount)
            .Select(p => p.LocalMatch)
            .ToArray();
    }

    private List<Match> FindMatchesWithVariance(FeatureVector featureVector, FeatureVector[] fMacro, double expectedVariance)
    {
        /* Upper bound on collected matches in case the variance cutoff triggers late
           (e.g. when the feature distance scale changes with the normalization method).
           A pattern repeating across the object still shows its high spatial variance
           within this many nearest matches. */
        const int MAX_LOCAL_MATCHES = 100;

        var distances = fMacro
            .Select(m => new { Macro = m, Dist = featureVector == m ? 10000 : featureVector.DistTo(m) })
            .OrderBy(x => x.Dist)
            .ToList();

        var localMatches = new List<Match>();
        double sum = 0;
        double sumSq = 0;

        for (int i = 0; i < distances.Count && localMatches.Count < MAX_LOCAL_MATCHES; i++)
        {
            double d = distances[i].Dist;
            sum += d;
            sumSq += d * d;

            int n = i + 1;
            double mean = sum / n;
            double variance = (sumSq / n) - (mean * mean);

            if (n > 1 && variance >= expectedVariance)
            {
                break;
            }

            localMatches.Add(new Match(featureVector, distances[i].Macro, Similarity(featureVector, distances[i].Macro)));
        }
        
        return localMatches;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="f1"></param>
    /// <param name="f2"></param>
    /// <returns></returns>
    public Match[] Match(FeatureVector[] f1, FeatureVector[] f2)
    {
        return Match(f1, f2, 0.1);
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