using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.TransformationDistanceMetrics;

public interface ITransformationDistance
{
    double GetTransformationsDistance(Transform3D transformation1, Transform3D transformation2);
    double GetRelativeTransformationDistance(Transform3D transformation1, Transform3D transformation2);
}

