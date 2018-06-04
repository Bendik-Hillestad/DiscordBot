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
        private static readonly long   HEADER_START  = 0;
        private static readonly int    HEADER_LENGTH = 16;
        private static readonly byte[] HEADER_MAGIC  = Encoding.UTF8.GetBytes("EVTC");

        public EVTCReader(string path)
        {
            this.stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Delete);
        }

        public EVTCHeader ReadHeader()
        {
            //Seek to the header start
            this.stream.Seek(HEADER_START, SeekOrigin.Begin);

            //Read the header
            var header = new byte[HEADER_LENGTH];
            this.stream.Read(header, 0, HEADER_LENGTH);

            //Split into the different pieces
            var magic     = new ReadOnlySpan<byte>(header,  0, 4);
            var build     = new ReadOnlySpan<byte>(header,  4, 8);
            var pad1      = new ReadOnlySpan<byte>(header, 12, 1);
            var encounter = new ReadOnlySpan<byte>(header, 13, 2);
            var pad2      = new ReadOnlySpan<byte>(header, 15, 1);

            //Check that the first 4 bytes are 'EVTC'
            if (!magic.SequenceEqual(HEADER_MAGIC))
            {
                //Magic number missing; not a valid EVTC file.
                throw new EVTCMagicMissingException();
            }

            //Check that the paddings are NUL bytes
            if (!pad1.SequenceEqual(pad2) || (pad1[0] != 0))
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
                    //Dispose of the stream
                    this.stream.Dispose();
                    this.stream = null;
                }

                //Mark as disposed
                this.disposed = true;
            }
        }

        private FileStream stream;
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
