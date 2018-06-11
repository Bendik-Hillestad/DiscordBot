using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DiscordBot.Modules.Raid
{
    public abstract class UploadManager
    {
        public enum LogUploadStatus
        {
            Uploading,
            Processing,
            Failed,
            Succeeded
        }

        public class UploadStatusChangedEventArgs : EventArgs
        {
            public long            UniqueID     { get; set; }
            public string          HostName     { get; set; }
            public LogUploadStatus UploadStatus { get; set; }
            public string          URL          { get; set; }
        }

        public delegate void UploadStatusChangedEventHandler(object sender, UploadStatusChangedEventArgs e);
        public event UploadStatusChangedEventHandler UploadStatusChanged;

        protected virtual void OnUploadStatusChanged(UploadStatusChangedEventArgs e)
        {
            //Check that it's not null
            if (UploadStatusChanged != null)
            {
                //Invoke the event
                UploadStatusChanged.Invoke(this, e);
            }
        }

        public abstract string Name     { get; }
        public abstract bool   Complete { get; }

        public abstract void AddItem(Stream source, string filename, long uniqueID);
        public abstract void Start  ();
    }

    public class LogUploader
    {
        public delegate void UploadStatusChangedEventHandler(object sender, UploadManager.UploadStatusChangedEventArgs e);
        public delegate void UploadsCompleteEventHandler    (object sender);

        public event UploadStatusChangedEventHandler UploadStatusChanged;

        public LogUploader()
        {
            this.managerTypes   = new List<Type>();
            this.uploadManagers = new List<UploadManager>();
            this.uploadQueue    = new List<UploadItem>();
        }

        public void RegisterUploader(Type type)
        {
            //Check that it's a valid type
            if (!type.IsSubclassOf(typeof(UploadManager)))
                throw new ArgumentException("Provided type does not derive from UploadManager.");

            //Register the type
            this.managerTypes.Add(type);
        }

        public void AddItem(MultiReader source, string name, long uniqueID)
        {
            //Add to the queue
            this.uploadQueue.Add(new UploadItem { mr = source, filename = name, id = uniqueID });
        }

        public void Start()
        {
            //Go through our manager types
            this.managerTypes.ForEach(type =>
            {
                //Instantiate the manager
                var m = (UploadManager)type.GetConstructor(Type.EmptyTypes).Invoke(null);

                //Subscribe to the manager's events
                m.UploadStatusChanged += Manager_UploadStatusChanged;

                //Go through our queue of items
                this.uploadQueue.ForEach(item =>
                {
                    //Send item to the manager
                    m.AddItem(item.mr.GetStream(), item.filename, item.id);
                });

                //Start it
                m.Start();

                //Add the manager to our list
                this.uploadManagers.Add(m);
            });

            //Clear the queue
            this.uploadQueue.Clear();

            //Wait until the managers are done
            this.WaitForExit();
        }

        private void WaitForExit()
        {
            //Loop until managers are done
            while (true)
            {
                //Wait 5 seconds
                Thread.Sleep(5000);

                //Check if they're done
                var done = (this.uploadManagers.Count(m => !m.Complete) == 0);

                //Break if they're done
                if (done) break;
            }
        }

        private void Manager_UploadStatusChanged(object sender, UploadManager.UploadStatusChangedEventArgs e)
        {
            this.UploadStatusChanged.Invoke(sender, e);
        }

        private struct UploadItem
        {
            public MultiReader mr;
            public string filename;
            public long id;
        }

        private List<Type>          managerTypes;
        private List<UploadManager> uploadManagers;
        private List<UploadItem>    uploadQueue;
    }
}
