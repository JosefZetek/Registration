using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Matching;

public interface IMatcher
{
    /// <summary>
    /// Method finds pairs of micro and macro feature vectors, keeps top threshold * number of total
    /// matches with the highest similarity and returns them as an array of Match objects.
    /// </summary>
    /// <param name="featureVectorsMicro">Micro Feature Vectors</param>
    /// <param name="featureVectorsMacro">Macro Feature Vectors</param>
    /// <param name="threshold">Value ranging from 0 to 1 representing share of matches to keep (ordered by similarity)</param>
    /// <returns>Returns array of matches (pairs)</returns>
    Match[] Match(FeatureVector[] featureVectorsMicro, FeatureVector[] featureVectorsMacro, double threshold);
    Match[] Match(FeatureVector[] featureVectorsMicro, FeatureVector[] featureVectorsMacro);
}