using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.Samplers;

public interface ISampler
{
    Point3D[] Sample(AData d, int count);
}
