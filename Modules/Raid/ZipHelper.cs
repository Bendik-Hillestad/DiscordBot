using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

using DiscordBot.Utils;

namespace DiscordBot.Modules.Raid
{
    public static class ZipHelper
    {
        public static IEnumerable<(string filename, MultiReader stream)> GetUnzippedStreams(Stream zipFile)
        {
            //Determine the folder to unzip to
            var dst = Path.Combine(Utility.GetTempDirectory(), Path.GetRandomFileName() + Path.DirectorySeparatorChar);

            //Make sure the folder exists
            Directory.CreateDirectory(dst);

            //Open the zip archive
            var zip = new ZipArchive(zipFile, ZipArchiveMode.Read, true);

            //Get only the files with an extension we recognize
            var files = zip.Entries.Where(e =>
            {
                //Check that it's not a directory
                if (e.FullName.EndsWith('/')) return false;

                //Get extension
                string ext = Path.GetExtension(e.Name);

                //Check if the file has no extension
                if (string.IsNullOrEmpty(ext)) return true;

                //Check if it's an accepted extension
                switch (ext)
                {
                    case ".zip":
                    case ".evtc":
                    case ".zevtc":
                    case ".tmp":
                        return true;

                    default: return false;
                }
            });

            //Eliminate any large files (>50MB)
            files = files.Where(e => e.Length <= 5e+7);

            //Go through the files
            foreach (var file in files)
            {
                //Check if it's another .zip file
                if (file.Name.EndsWith(".zip") || file.Name.EndsWith(".zevtc"))
                {
                    //Recursively open
                    foreach (var nestedFile in GetUnzippedStreams(file.Open())) yield return nestedFile;
                }
                else
                {
                    //Get the file name with .evtc extension
                    var outputName = Path.ChangeExtension(file.Name, ".evtc");

                    //Extract the file
                    var path = Path.Combine(dst, outputName);
                    file.ExtractToFile(path);

                    //Open a stream to it
                    var stream = new MultiReader(path);
                    
                    //Yield the stream
                    yield return (outputName, stream);
                }
            }
        }
    }
}
