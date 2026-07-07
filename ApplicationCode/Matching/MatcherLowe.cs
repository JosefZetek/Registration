using System.Collections.Generic;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Matching;

/// <summary>
/// KD-tree matcher with Lowe's ratio test. For each micro feature vector the two nearest
/// macro vectors are found; the match is kept only when
/// d(closest) / d(secondClosest) &lt;= loweRatioThreshold, i.e. when the closest candidate
/// is clearly more similar than the runner-up. Expects samples where each salient region
/// appears only once (e.g. SamplerSegmented) - with repeated sampling the two nearest
/// candidates describe the same spot and the ratio rejects even correct matches.
/// </summary>
public class MatcherLowe : IMatcher
{
    private double loweRatioThreshold;

    public MatcherLowe(double loweRatioThreshold = 0.8)
    {
        this.loweRatioThreshold = loweRatioThreshold;
    }

    public Match[] Match(FeatureVector[] fMicro, FeatureVector[] fMacro, double threshold)
    {
        KDTree tree = new KDTree(fMacro);
        List<Match> matches = new List<Match>();

        for (int i = 0; i < fMicro.Length; i++)
        {
            int[] nearest = tree.FindNearest(fMicro[i], 2);

            if (nearest.Length == 0 || nearest[0] < 0)
                continue;

            double distanceClosest = fMicro[i].DistTo(fMacro[nearest[0]]);

            /* With a single macro vector there is no runner-up to compare against;
               the match is trivially unambiguous. */
            if (nearest.Length < 2 || nearest[1] < 0)
            {
                matches.Add(new Match(fMicro[i], fMacro[nearest[0]], 1.0));
                continue;
            }

            double distanceSecondClosest = fMicro[i].DistTo(fMacro[nearest[1]]);

            /* Two identical descriptors at zero distance - inherently ambiguous. */
            if (distanceSecondClosest <= 0)
                continue;

            double ratio = distanceClosest / distanceSecondClosest;

            if (ratio > loweRatioThreshold)
                continue;

            /* Similarity = 1 - ratio so that more distinctive matches rank higher,
               matching the "higher similarity is better" convention of Match. */
            matches.Add(new Match(fMicro[i], fMacro[nearest[0]], 1.0 - ratio));
        }

        /* Match.CompareTo orders by similarity descending, so the most distinctive
           matches come first and the top `threshold` fraction is kept. */
        matches.Sort();
        int numberOfMatches = (int)(matches.Count * threshold);
        return matches.GetRange(0, numberOfMatches).ToArray();
    }

    public Match[] Match(FeatureVector[] fMicro, FeatureVector[] fMacro)
    {
        /* The ratio test itself already filters unstable matches, keep all that pass. */
        return Match(fMicro, fMacro, 1.0);
    }
}
