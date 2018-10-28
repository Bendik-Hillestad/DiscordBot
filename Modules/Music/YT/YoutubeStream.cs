using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Music.YT
{
    public sealed class YoutubeStream
    {
        private YoutubeStream(Stream baseStream, bool buffered, string name)
        {
            this.stream     = baseStream;
            this.exitSignal = new Semaphore(0, 1);
            this.counter    = new Semaphore(0, int.MaxValue);
            this.buffer     = (buffered ? new ConcurrentQueue<Memory<byte>>() : null);
            this.IsBuffered = buffered;
            this.Name       = name;
        }

        public CancellationTokenSource Cancellation { get; private set; } = new CancellationTokenSource();
        public bool                    IsBuffered   { get; private set; }
        public string                  Name         { get; private set; }

        public static YoutubeStream CreateBuffered(string id, string name)
        {
            //Create the YoutubeStream object
            var ytstream = new YoutubeStream(null, true, name);

            //Start a new thread
            var _ = Task.Run(() =>
            {
                //Get the base stream
                ytstream.stream = GetYouTubeStream(id);

                //Configure the block size
                const int blockSize = 3840;

                //Begin our buffering loop
                while (true)
                {
                    //Stop if we're supposed to cancel
                    ytstream.Cancellation.Token.ThrowIfCancellationRequested();

                    //Read a block
                    Memory<byte> buff = new byte[blockSize];
                    var i = ytstream.stream.Read(buff.Span);

                    //Check if we're done
                    if (i == 0)
                    {
                        //Notify the consumer and exit
                        ytstream.counter.Release();
                        break;
                    }

                    //Check if we didn't completely fill the block
                    if (i < blockSize) buff = buff.Slice(0, i);

                    //Add to buffer
                    ytstream.buffer.Enqueue(buff);
                    ytstream.counter.Release();
                }
            }, ytstream.Cancellation.Token).ContinueWith(t =>
            {
                //Signal that we're exiting
                ytstream.counter.Release();
                ytstream.exitSignal.Release();
            });

            //Return the object
            return ytstream;
        }

        public static YoutubeStream CreateUnbuffered(string id, string name)
        {
            //Create the YoutubeStream object
            return new YoutubeStream(GetYouTubeStream(id), false, name);
        }

        public int ReadBlock(Span<byte> buffer)
        {
            //Check if we're buffered
            if (this.IsBuffered)
            {
                //Wait for the reader thread
                this.counter.WaitOne();

                //Check if we're done
                if (this.buffer.IsEmpty) return 0;

                //Pop a block off of the queue
                Memory<byte> block = null;
                while (!this.buffer.TryDequeue(out block));

                //Copy into buffer
                block.Span.CopyTo(buffer);

                //Return length
                return block.Length;
            }
            else
            {
                //Read directly from the stream
                return this.stream.Read(buffer);
            }
        }

        public void WaitForExit()
        {
            //Block until it exits
            this.exitSignal.WaitOne();
        }

        private static Stream GetYouTubeStream(string id)
        {
            //Start ytdl
            var ytdl = Process.Start(new ProcessStartInfo
            {
                FileName  = $"youtube-dl",
                Arguments = $"-f 251/bestaudio --prefer-insecure -g {id}",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            }).StandardOutput;

            //Read the url from ytdl
            var url = ytdl.ReadToEndAsync().GetAwaiter().GetResult()
                          .Trim();

            //Check that it's not null
            Utils.Debug.Assert(!string.IsNullOrEmpty(url), "Couldn't get URL");

            //Return ffmpeg stream
            return Process.Start(new ProcessStartInfo
            {
                FileName  = $"ffmpeg",
                Arguments = $"-i \"{url}\" -f s16le -ar 48000 -ac 2 pipe:1",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            }).StandardOutput.BaseStream;
        }

        private Stream    stream;
        private Semaphore exitSignal;
        private Semaphore counter;
        private ConcurrentQueue<Memory<byte>> buffer;
    }
}
