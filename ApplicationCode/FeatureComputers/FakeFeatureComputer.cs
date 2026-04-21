
using DataView;
using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;
using Registration.ApplicationCode.Other.FCConfiguration;

namespace Registration.ApplicationCode.FeatureComputers;

/// <summary>
/// Fake Feature Computer uses point coordinates as the descriptor.
/// </summary>
public class FakeFeatureComputer : AFeatureComputer
{
    private Transform3D transformation;
    private bool transformedSampling;

    public FakeFeatureComputer(Transform3D transformation)
    {
        this.transformation = transformation;
        this.transformedSampling = false;
    }

    public override void ComputeFeatureVector(AData d, Point3D p, double[] array, int startIndex)
    {
        CheckArrayDimensions(array, startIndex);

        Point3D point = !transformedSampling ? p : p.Copy().ApplyRotationTranslation(transformation);

        array[startIndex] = point.X;
        array[startIndex + 1] = point.Y;
        array[startIndex + 2] = point.Z;
    }

    public override void GetWeights(double[] array, int startIndex)
    {
        for (int i = 0; i < NumberOfFeatures; i++)
            array[startIndex + i] = 1.0;
    }

    public override FCConfiguration GetConfiguration()
    {
        throw new System.NotImplementedException("This method is not implemented yet for FakeFeatureComputer, requires the Transform3D instance.");
    }

    public override int NumberOfFeatures => 3;

    public void SetTransformedSampling(bool transformedSampling)
    {
        this.transformedSampling = transformedSampling;
    }

        
}