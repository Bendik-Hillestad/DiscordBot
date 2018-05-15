using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace DiscordBot.Music
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Samples
    {
        [FieldOffset(0)]
        public Byte[] raw;

        [FieldOffset(0)]
        public Int16[] real;
    }

    public static class Audio
    {
        public static void AdjustVolume(ref Samples samples, int volume)
        {
            //Get references to the underlying buffer
            ref var raw_buffer  = ref samples.raw;
            ref var real_buffer = ref samples.real;

            //Calculate the number of elements we have
            var elems = raw_buffer.Length / sizeof(Int16);

            //Calculate the vectors we use in our calculation
            var factor  = new Vector<Int16>((Int16)((volume * 4) / 100));
            var divisor = new Vector<Int16>(4);

            //Iterate over the buffer
            for (int i = 0; i < elems; i += Vector<Int16>.Count)
            {
                //Create a vector from the buffer
                var vector = new Vector<Int16>(real_buffer, i);

                //Adjust the volume and write it back
                ((vector * factor) / divisor).CopyTo(real_buffer, i);
            }
        }
    }
}