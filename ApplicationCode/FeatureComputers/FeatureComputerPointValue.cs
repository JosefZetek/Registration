using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.Other.FCConfiguration;

namespace Registration.ApplicationCode.FeatureComputers;

public class FeatureComputerPointValue : AFeatureComputer
{
    public override int NumberOfFeatures => 1;

    public override void ComputeFeatureVector(AData d, Point3D p, double[] array, int startIndex)
    {
        CheckArrayDimensions(array, startIndex);
        array[startIndex] = d.GetValue(p);
    }

    public override void GetWeights(double[] array, int startIndex)
    {
        for (int i = 0; i < NumberOfFeatures; i++)
            array[startIndex + i] = 1.0;
    }

    public override FCConfiguration GetConfiguration()
    {
        var config = new FCConfiguration();
        config.FeatureComputerType = FCType.POINT_VALUE_FEATURE_COMPUTER;
        config.AddParameter("Comment", "Point Value");
        return config;
    }
}