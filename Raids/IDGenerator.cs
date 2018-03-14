using System;
using System.Collections.Generic;

namespace DiscordBot.Raids
{
    [Serializable]
    public sealed class IDGenerator
    {
        public IDGenerator()
        {
            this.counter   = 1;
            this.freeQueue = new Queue<int>();
        }

        public int NewID()
        {
            //Check the free queue
            if (this.freeQueue.Count > 0)
            {
                //Pop ID off the queue
                return this.freeQueue.Dequeue();
            }

            //Post-Increment counter and return
            return this.counter++;
        }

        public void ReleaseID(int id)
        {
            //Push onto the queue
            this.freeQueue.Enqueue(id);
        }

        private int        counter;
        private Queue<int> freeQueue;
    }
}
