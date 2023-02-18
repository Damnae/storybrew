using System.IO;
using System.IO.Compression;

public static class ZipArchiveExtensions
{
    public static void ExtractToDirectoryOverwrite(this ZipArchive archive, string destinationDirectoryName)
    {
        foreach (ZipArchiveEntry file in archive.Entries)
        {
            var destinationFileName = Path.Combine(destinationDirectoryName, file.FullName);
            var directory = Path.GetDirectoryName(destinationFileName);

            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
            if (file.Name.Length > 0) file.ExtractToFile(destinationFileName, true);
        }
    }
}