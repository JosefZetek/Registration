using Registration.ApplicationCode.Other;

using System.Collections.Generic;
using System.Linq;

namespace Registration.ApplicationCode.Matching;

public class PositionMatcher : IMatcher
{
    public Match[] Match(FeatureVector[] fMicro, FeatureVector[] fMacro, double threshold)
    {
        var prematches = new List<(FeatureVector RefVec, List<Match> LocalMatches, double SpatialVariance)>();

        for (int i = 0; i < fMicro.Length; i++)
        {
            var localMatches = FindMatchesWithVariance(fMicro[i], fMacro, 10);
            
            if (localMatches.Count > 0)
            {
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

                prematches.Add((fMicro[i], localMatches, posVariance));
            }
        }

        var sortedPrematches = prematches
            .OrderBy(p => p.SpatialVariance)
            .ToList();
        
        int keepCount = (int)System.Math.Ceiling(sortedPrematches.Count * threshold);

        var spatialVariance = sortedPrematches
            .Take(keepCount)
            .Select(p => p.SpatialVariance)
            .ToList();

        //save into csv
        List<Point2D> variancePoints = new List<Point2D>();
        
        for (int i = 0; i<spatialVariance.Count; i++)
        {
            variancePoints.Add(new Point2D(i, spatialVariance[i]));
        }
        
        CSVWriter.WriteResult("/Users/pepazetek/spatialVariance.csv", "index", "spatialVariance", variancePoints);

        return sortedPrematches
            .Take(keepCount)
            .SelectMany(p => p.LocalMatches)
            .ToArray();
    }

    private List<Match> FindMatchesWithVariance(FeatureVector featureVector, FeatureVector[] fMacro, double expectedVariance)
    {
        var distances = fMacro
            .Select(m => new { Macro = m, Dist = featureVector.DistTo(m) })
            .OrderBy(x => x.Dist)
            .ToList();

        var localMatches = new List<Match>();
        double sum = 0;
        double sumSq = 0;

        for (int i = 0; i < distances.Count; i++)
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