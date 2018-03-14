using System;
using System.IO;

namespace DiscordBot.Utils
{
    public sealed class TempFile : IDisposable
    {
        public FileStream Stream => this.file;

        public TempFile(string path, FileAccess access, FileShare share)
        {
            //Open file
            this.file = File.Open(path, FileMode.CreateNew, access, share);
        }

        private void Dispose(bool disposing)
        {
            //Check if we've already run the dispose code for this instance
            if (!this.isDisposed)
            {
                //Check if the underlying handle exists
                if (this.file != null)
                {
                    //Close the stream
                    this.file.Close();

                    //Delete the file
                    File.Delete(this.file.Name);

                    //Clear handle
                    this.file = null;
                }

                //Mark as disposed
                this.isDisposed = true;
            }
        }

        ~TempFile()
        {
            Dispose(false);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private FileStream file       = null;
        private bool       isDisposed = false;
    }
}
