using System;
using System.Collections.Generic;
using System.IO;

using DiscordBot.Utils;

namespace DiscordBot.Modules.Raid
{
    public sealed class MultiReader : IDisposable
    {
        public MultiReader(string path)
        {
            //Open a stream that will delete the file when we're done
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Delete, 2 << 16, FileOptions.DeleteOnClose))
            {
                //Allocate our buffer
                this.data  = new byte[fs.Length];
                var offset = 0;

                //Read data from the file
                int read;
                while ((read = fs.Read(this.data, offset, (int)(fs.Length - offset))) > 0)
                    offset += read;
            }

            //Create a list to hold our open streams
            this.streams = new List<MemoryStream>();
        }

        public Stream GetStream()
        {
            //Check that we have data
            Debug.Assert(this.data != null,    "Data is null!");
            Debug.Assert(this.data.Length > 0, "Data is empty!");

            //Create a new stream to our data and store it
            var stream = new MemoryStream(this.data, false);
            this.streams.Add(stream);

            //Return the stream
            return stream;
        }

        public void Dispose()
        {
            //Run the dispose method
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            //Avoid redundant calls
            if (!this.disposed)
            {
                //Check if we're disposing managed state
                if (disposing)
                {
                    //Go through all the streams
                    foreach (var stream in this.streams)
                    {
                        //Dispose of the stream
                        stream.Dispose();
                    }

                    //Release our list
                    this.streams = null;

                    //Release the underlying data
                    this.data = null;
                }

                //Mark as disposed
                this.disposed = true;
            }
        }

        private List<MemoryStream> streams;
        private byte[]             data;
        private bool               disposed = false;
    }
}
