namespace Registration.ApplicationCode.Other;

/// <summary>
/// Class contains information regarding the path for data object with its metadata file
/// </summary>
public class FilePathDescriptor
{
    public string MHDFilePath { get; }
    public string DataFilePath { get; }

    /// <summary>
    /// Constructor taking in paths
    /// </summary>
    /// <param name="MHDFilePath">Metadata file path</param>
    /// <param name="DataFilePath">Data file path</param>
    public FilePathDescriptor(string MHDFilePath, string DataFilePath)
    {
        this.MHDFilePath = MHDFilePath;
        this.DataFilePath = DataFilePath;
    }
}
