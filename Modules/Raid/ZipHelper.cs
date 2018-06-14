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
            
            //Get only the files that match the .evtc or .zip extension
            var regex = new Regex(@"[A-Za-z0-9\-_]+(\.evtc|\.zip)");
            var files = zip.Entries.Where(e => regex.IsMatch(e.Name));

            //Eliminate any large files (>30MB)
            files = files.Where(e => e.Length <= 3e+7);

            //Go through the files
            foreach (var file in files)
            {
                //Check if it's another .zip file
                if (file.Name.EndsWith(".zip"))
                {
                    //Recursively open
                    foreach (var nestedFile in GetUnzippedStreams(file.Open())) yield return nestedFile;
                }
                else
                {
                    //Extract the file
                    var path = Path.Combine(dst, file.Name);
                    file.ExtractToFile(path);

                    //Open a stream to it
                    var stream = new MultiReader(path);
                    
                    //Yield the stream
                    yield return (file.Name, stream);
                }
            }
        }
    }
}
