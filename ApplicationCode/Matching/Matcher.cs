using System.Collections.Generic;
using System.Linq;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Matching;

public class Matcher : IMatcher
{
    public Match[] Match(FeatureVector[] fMicro, FeatureVector[] fMacro, double threshold)
    {
        KDTree tree = new KDTree(fMacro);
        List<Match> matches = new List<Match>();

        for (int i = 0; i < fMicro.Length; i++)
        {
            var nearest = tree.FindNearest(fMicro[i],1)[0];
            if (nearest >= 0)
            {
                double s = Similarity(fMicro[i], fMacro[nearest]);
                matches.Add(new Match(fMicro[i], fMacro[nearest], s));
            }
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

    public Match[] FakeMatch(FeatureVector[] f1, FeatureVector[] f2, double threshold)
    {
        int numberOfMatches = (int)(f1.Length / 100.0 * threshold);
        Match[] matchesReturn = new Match[numberOfMatches];
        for (int i =0; i < numberOfMatches; i++)
        {
            matchesReturn[i] = new Match(f1[i],f2[i],100);
        }
        return matchesReturn;
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