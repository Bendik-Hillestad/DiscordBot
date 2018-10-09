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
            
            //Get only the files that match the .evtc or .zip extension (or evtc.tmp)
            var regex = new Regex(@"^[A-Za-z0-9\-_\.]+(\.evtc\.tmp|\.evtc|\.zip)$");
            var files = zip.Entries.Where(e => regex.IsMatch(e.Name));

            //Eliminate any large files (>50MB)
            files = files.Where(e => e.Length <= 5e+7);

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
                    //Workaround for arcdps
                    var outputName = file.Name;
                    if (outputName.EndsWith(".tmp"))
                    {
                        outputName = outputName.Substring(0, outputName.Length - 4);
                    }

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
