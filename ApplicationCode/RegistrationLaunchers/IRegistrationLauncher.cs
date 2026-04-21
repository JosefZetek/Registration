using Registration.ApplicationCode.DataClasses.Data;
using Registration.ApplicationCode.Other;

namespace Registration.ApplicationCode.RegistrationLaunchers;

public interface IRegistrationLauncher
{
    Transform3D RunRegistration(FilePathDescriptor microDataPath, FilePathDescriptor macroDataPath);
    Transform3D RunRegistration(AData microData, AData macroData);
}

