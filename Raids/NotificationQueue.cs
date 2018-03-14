using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DiscordBot.Raids
{
    public struct Notification : IComparable<Notification>
    {
        public int      raidID;
        public ulong    userID;
        public DateTime time;
        public int      hours;
        public int      minutes;

        public int CompareTo(Notification obj)
        {
            return this.time.CompareTo(obj.time);
        }
    }

    public sealed class NotificationQueue
    {
        public NotificationQueue()
        {
            //Allocate the queue
            this.queue = new SortedSet<Notification>();
        }

        public void Add(Notification notification)
        {
            //Acquire the lock
            Monitor.Enter(this.lockObj);

            //Add to the queue
            this.queue.Add(notification);

            //Signal that the queue was altered
            this.signal.Set();

            //Release the lock
            Monitor.Exit(this.lockObj);
        }

        public Task WaitForItem()
        {
            //Don't wait if there is an item in the queue
            if (this.queue.Count > 0) return Task.CompletedTask;

            //Wait until the signal is set
            this.signal.WaitOne();

            //Return success
            return Task.CompletedTask;
        }

        public Task<Notification?> WaitNext()
        {
            //Check that the signal was not set
            if (signal.WaitOne(0)) goto abort;

            //Check that the queue is not empty
            if (this.queue.Count == 0) goto abort;

            //Acquire the lock
            Monitor.Enter(this.lockObj);

            //Check again that the signal was not set
            if (signal.WaitOne(0)) goto abort;

            //Check again that the queue is not empty
            if (this.queue.Count == 0) goto abort;

            //Grab the next item from the queue
            var next = this.queue.Min;

            //Loop until we're past the point
            DateTime now;
            while (next.time > (now = DateTime.UtcNow))
            {
                //Calculate difference
                var diff = next.time - now;

                //Release the lock
                Monitor.Exit(this.lockObj);

                //Try to wait
                if (this.signal.WaitOne(diff)) goto abort;

                //Reacquire the lock
                Monitor.Enter(this.lockObj);
            }

            //Check again that the signal was not set
            if (this.signal.WaitOne(0)) goto abort;

            //Remove item from queue
            this.queue.Remove(next);

            //Return success
            return Task.FromResult<Notification?>(next);

            abort: //Aborted, so release the lock if we have it and return nothing
            if (Monitor.IsEntered(this.lockObj)) Monitor.Exit(this.lockObj);
            return Task.FromResult<Notification?>(null);
        }

        public void ReleaseLock()
        {
            //Release the lock
            Monitor.Exit(this.lockObj);
        }

        private SortedSet<Notification>  queue;
        private readonly EventWaitHandle signal  = new EventWaitHandle(false, EventResetMode.AutoReset);
        private readonly object          lockObj = new object();
    }
}
