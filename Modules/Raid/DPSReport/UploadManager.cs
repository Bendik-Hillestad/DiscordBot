using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBot.Modules.Raid.DPSReport
{
    public sealed class ReportUploadManager : UploadManager
    {
        public override string Name     => "dps.report";
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

                //Go through all entries
                foreach (var kv in tmp)
                {
                    //Get the item
                    var item = kv.Value;

                    //Upload it
                    var ret = Client.UploadLog(item.source, item.filename);

                    //Check if it was successful
                    var err = (ret == null) || !string.IsNullOrEmpty(ret.error);

                    //Trigger event
                    this.OnUploadStatusChanged(new UploadStatusChangedEventArgs
                    {
                        UniqueID     = kv.Key,
                        HostName     = this.Name,
                        UploadStatus = err ? LogUploadStatus.Failed : LogUploadStatus.Succeeded,
                        URL          = ret?.permalink
                    });
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

        private bool                   isDone = false;
        private Dictionary<long, Item> items  = new Dictionary<long, Item>();
    }
}
