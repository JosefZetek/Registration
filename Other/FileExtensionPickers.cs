using Avalonia.Platform.Storage;

namespace Registration.ApplicationCode.Other;

public class FileExtensionPickers
{
    public static FilePickerOpenOptions RAW = new FilePickerOpenOptions
    {
        AllowMultiple = false,
        FileTypeFilter = new[]
        {
            new FilePickerFileType("Raw Image Files")
            {
                Patterns = new[] { "*.raw" }
            }
        }
    };
    
    public static FilePickerOpenOptions MHD = new FilePickerOpenOptions
    {
        AllowMultiple = false,
        FileTypeFilter = new[]
        {
            new FilePickerFileType("MetaImage Files")
            {
                Patterns = new[] { "*.mhd" }
            }
        }
    };

    public static FilePickerOpenOptions TXT = new FilePickerOpenOptions
    {
        AllowMultiple = false,
        FileTypeFilter = new[]
        {
            new FilePickerFileType("Text Files")
            {
                Patterns = new[] { "*.txt" }
            }
        }
    };
}