using System;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;

using DiscordBot.Core;

using Timer = System.Timers.Timer;

namespace DiscordBot
{
    public sealed class Program
    {
        public static void Main(string[] args) => (new Program()).Start().GetAwaiter().GetResult();

        public async Task Start()
        {
            Raids.RaidConfig.ReadConfig().GenerateSolverLibrary();
            return;

            //Set output encoding
            Console.OutputEncoding = Encoding.Unicode;

            //Set default color
            Console.ForegroundColor = ConsoleColor.Gray;

            //Copy the invariant culture
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();

            //Change the way DateTime is displayed
            culture.DateTimeFormat.ShortDatePattern   = "dd.MM.yyyy HH:mm:ss";
            culture.DateTimeFormat.LongTimePattern    = "";

            //Apply culture
            Thread.CurrentThread.CurrentCulture       = culture;
            Thread.CurrentThread.CurrentUICulture     = culture;
            CultureInfo.DefaultThreadCurrentCulture   = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            //Workaround for GC not collecting memory that was freed ages ago, eventually resulting in OOM errors
            Timer t = new Timer(4 * 60 * 1000);
            t.Elapsed += (s, e) =>
            {
                //Read the current memory used
                var mem = GC.GetTotalMemory(false);

                //Force the GC to perform garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();

                //Find out how much was released
                var diff = mem - GC.GetTotalMemory(false);

                //Log it
                Utils.Logger.Log(Utils.LOG_LEVEL.INFO, $"Released {diff} bytes of memory.");
            };
            t.AutoReset = true;
            t.Start();

            //Run bot
            await (new Bot()).Run();
        }
    }
}
