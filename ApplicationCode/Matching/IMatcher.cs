using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Matching;

public interface IMatcher
{
    Match[] Match(FeatureVector[] featureVectorsMicro, FeatureVector[] featureVectorsMacro, double threshold);
    Match[] Match(FeatureVector[] featureVectorsMicro, FeatureVector[] featureVectorsMacro);
}