using System;
using System.IO;
using System.Text;

namespace DiscordBot.Modules.Raid.EVTC
{
    public struct EVTCHeader
    {
        public string build_date;
        public short  encounter_id;
    }

    public sealed class EVTCReader : IDisposable
    {
        private static readonly int    HEADER_LENGTH = 16;
        private static readonly byte[] HEADER_MAGIC  = Encoding.UTF8.GetBytes("EVTC");

        public EVTCReader(string path)
        {
            this.stream   = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Delete);
            this.keepOpen = false;
        }

        public EVTCReader(Stream stream, bool keepOpen = false)
        {
            this.stream   = stream;
            this.keepOpen = keepOpen;
        }

        public EVTCHeader ReadHeader()
        {
            //Read the header
            var header = new byte[HEADER_LENGTH];
            this.stream.Read(header, 0, HEADER_LENGTH);

            //Split into the different pieces
            var magic     = new ReadOnlySpan<byte>(header,  0, 4);
            var build     = new ReadOnlySpan<byte>(header,  4, 8);
            var unknown   = header[12];
            var encounter = new ReadOnlySpan<byte>(header, 13, 2);
            var pad       = header[15];

            //Check that the first 4 bytes are 'EVTC'
            if (!magic.SequenceEqual(HEADER_MAGIC))
            {
                //Magic number missing; not a valid EVTC file.
                throw new EVTCMagicMissingException();
            }

            //Check that the padding is a NUL byte
            if (pad != 0)
            {
                //The header is corrupt
                throw new EVTCBadHeaderException();
            }

            //Return the values
            return new EVTCHeader
            {
                build_date   = Encoding.UTF8.GetString(build),
                encounter_id = BitConverter.ToInt16(encounter)
            };
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
                    //Check if we should dispose of the stream
                    if (!this.keepOpen)
                    {
                        //Dispose of the stream
                        this.stream.Dispose();
                        this.stream = null;
                    }
                }

                //Mark as disposed
                this.disposed = true;
            }
        }

        private Stream stream;
        private bool keepOpen;
        private bool disposed = false;
    }

    public sealed class EVTCMagicMissingException : Exception
    {
        public EVTCMagicMissingException() 
            : base("This is not a valid EVTC log file.")
        { }
    }

    public sealed class EVTCBadHeaderException : Exception
    {
        public EVTCBadHeaderException()
            : base("The header is corrupt.")
        { }
    }
}
