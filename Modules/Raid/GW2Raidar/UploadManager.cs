using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Raid.GW2Raidar
{
    public sealed class RaidarUploadManager : UploadManager
    {
        public override string Name     => "GW2Raidar";
        public override bool   Complete => this.isDone;

        public override void AddItem(Stream source, string filename, long uniqueID)
        {
            this.items.Add(uniqueID, new Item { source = source, filename = filename });
        }

        public override void Start()
        {
            //Start a new thread
            Task.Run(() =>
            {
                //Move entries into a temporary variable
                var tmp    = this.items;
                this.items = null;

                //Prepare our client
                this.client = new SneakyClient();

                //Prepare our container for pending uploads
                this.uploads = new Dictionary<int, long>();

                //Begin polling thread
                this.BeginPolling();

                //Go through all entries
                foreach (var kv in tmp)
                {
                    //Get the item
                    var item = kv.Value;

                    //Upload it
                    var ret = client.Upload(item.source, item.filename);

                    //Check if it was successful
                    var err = (ret == null);

                    //Trigger first event
                    this.OnUploadStatusChanged(new UploadStatusChangedEventArgs
                    {
                        UniqueID     = kv.Key,
                        HostName     = this.Name,
                        UploadStatus = err ? LogUploadStatus.Failed : LogUploadStatus.Processing,
                        URL          = null
                    });

                    //Push upload id into the dictionary
                    if (!err) uploads.Add(ret.upload_id, kv.Key);
                }

                //Signal that polling may exit when done
                this.canExit = true;
            });
        }

        private void BeginPolling()
        {
            //Start a new thread
            Task.Run(() =>
            {
                //Mark start time
                var start = DateTimeOffset.UtcNow;

                //Begin looping
                while (true)
                {
                    //Wait 5 seconds
                    Task.Delay(5000).GetAwaiter().GetResult();

                    //Poll the server
                    var ret = this.client.Poll();

                    //Break if the return value is null
                    if (ret == null) break;

                    //Check if we have any notifications
                    if ((ret.notifications?.Count ?? 0) > 0)
                    {
                        //Filter the notifications to contain the ones we care about
                        var list = ret.notifications.Where (n => this.uploads.ContainsKey(n.upload_id))
                                                    .Select(n => (n.upload_id, url: n.encounter_url_id))
                                                    .ToList();

                        //Lock to prevent race conditions
                        lock (this.uploads)
                        {
                            //Go through the notifications
                            list.ForEach(n =>
                            {
                                //Get the associated unique ID
                                var id = this.uploads[n.upload_id];

                                //Remove the entry
                                this.uploads.Remove(n.upload_id);

                                //Trigger second event
                                this.OnUploadStatusChanged(new UploadStatusChangedEventArgs
                                {
                                    UniqueID     = id,
                                    HostName     = this.Name,
                                    UploadStatus = LogUploadStatus.Succeeded,
                                    URL          = @"https://www.gw2raidar.com/encounter/" + n.url
                                });
                            });
                        }
                    }

                    //Check if we are allowed to exit
                    if (this.canExit)
                    {
                        //Lock to prevent race conditions
                        lock (this.uploads)
                        {
                            //Exit if we're done
                            if (this.uploads.Count == 0) break;
                        }

                        //Exit if we used 30 minutes or more
                        if ((DateTimeOffset.UtcNow - start).Minutes >= 30) break;
                    }
                }

                //Mark as complete
                this.isDone = true;
            });
        }

        private struct Item
        {
            public Stream source;
            public string filename;
        }

        private bool                   isDone  = false;
        private bool                   canExit = false;
        private SneakyClient           client  = null;
        private Dictionary<int, long>  uploads = null;
        private Dictionary<long, Item> items   = new Dictionary<long, Item>();
    }
}
