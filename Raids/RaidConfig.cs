using System.IO;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace DiscordBot.Raids
{
    public sealed class CompDescription
    {
        public string       Name   { get; set; }
        public List<string> Layout { get; set; }
    }

    public sealed class RaidConfig
    {
        public static RaidConfig DefaultConfig = new RaidConfig
        {
            Compositions = new List<CompDescription>
            {
                new CompDescription
                {
                    Name   = "DEFAULT",
                    Layout = new List<string>
                    {
                        "MES", "HEAL", "DPS", "DPS", "SLAVE",
                        "MES", "HEAL", "DPS", "DPS", "DPS"
                    }
                }
            }
        };

        public List<CompDescription> Compositions { get; set; }

        public static RaidConfig ReadConfig()
        {
            //Check if the config exists
            if (File.Exists("raid_config.json"))
            {
                //Read the file
                var text = File.ReadAllText("raid_config.json");

                //Deserialize and return
                return JsonConvert.DeserializeObject<RaidConfig>(text);
            }
            else
            {
                //Load a default composition
                return RaidConfig.DefaultConfig;
            }
        }

        public void AddCompDescription(CompDescription description)
        {
            //Make sure one with this name does not already exist
            this.Compositions.RemoveAll((other) => string.Equals(description.Name, other.Name));

            //Add composition description
            this.Compositions.Add(description);
        }

        public void SaveConfig()
        {
            //Open/Create the file
            using (FileStream fs = File.Open("raid_config.json", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    //Generate JSON text
                    var json = JsonConvert.SerializeObject(this, Formatting.Indented);

                    //Write to the file
                    sw.Write(json);
                    sw.Flush();
                }
            }
        }

        public void GenerateSolverLibrary()
        {
            //Calculate unique roles
            var roles = this.GetRoles();

            //Prepare the string to hold the final code
            string code = "";

            //Emit our includes
            code += "#include \"platform.h\"\n" +
                    "#include \"typelist.h\"\n" +
                    "#include \"solver.h\"\n\n" +
                    "#include <cstring>\n\n";

            //Begin our implementation namespace
            code += "namespace billy_herrington::impl {\n";

            //Emit our roles
            code += string.Join("\n", roles.Select((r) => $"struct {r};"));
            code += "\n\n";
            code += $"using roles = tl::typelist_t<{string.Join(",", roles)}>;\n";
            code += "\n";

            //Iterate through the different compositions
            foreach (var comp in this.Compositions)
            {
                //Begin the namespace for this comp
                code += $"namespace {comp.Name} {{\n";

                //Emit the composition layout
                code += $"using composition = tl::typelist_t<{string.Join(",", comp.Layout)}>;\n";
                code += "\n";

                //Emit the template
                code += "#include \"implementation_template.h\"\n";

                //End the namespace for this comp
                code += "};\n";
            }

            //End the implementation namespace
            code += "};\n\n";

            //Begin our public API
            code += "BILLY_HERRINGTON_API void solve(unsigned int impl, void* roster, int length, void* output) {\n";
            code += "using namespace billy_herrington::impl;\n\n";

            //Begin the switch
            code += "switch (impl) {\n";

            //Iterate through the different compositions
            int n = 0;
            foreach (var comp in this.Compositions)
            {
                //Begin the case
                code += $"case {n}: {{\n";

                //Emit the function call
                code += $"{comp.Name}::solve" +
                         "(" +
                            $"static_cast<{comp.Name}::solver_config::roster_t::const_pointer>(roster), " +
                             "length, " +
                            $"static_cast<{comp.Name}::solver_config::comp_t::pointer>(output)" +
                         ");\n";

                //End the case
                code += "} break;\n\n";

                //Increment the counter
                n++;
            }

            //End the switch
            code += "default: UNREACHABLE;\n";
            code += "}\n";

            //End the public API
            code += "}\n\n";

            //Begin our compiler guard
            code += "#if defined(_MSC_VER) && !(defined(__c2__) || defined(__clang__) || defined(__GNUC__))\n";
            code += "\n";

            //Emit the entry-point
            code += "#define WIN32_LEAN_AND_MEAN\n" +
                    "#define VC_EXTRALEAN\n" +
                    "#include <windows.h>\n" +
                    "\n" +
                    "BOOL APIENTRY DllMain(HMODULE, DWORD, LPVOID) {\n" +
                        "return TRUE;\n" +
                    "}\n\n";

            //End the compiler guard
            code += "#endif\n";

            //Create the source file
            File.WriteAllText("./libherrington/dllmain.cpp", code);

            //For easier debugging, format the text
#           if DEBUG
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName               = "clang-format",
                Arguments              = "-style=file " +
                                         "-i " +
                                         "./libherrington/dllmain.cpp",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true
            });
#           endif

            //Delete the previous library just in case
            if (File.Exists("libherrington.so")) File.Delete("libherrington.so");

            //Compile the code
            var clang = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName               = "clang",
                Arguments              = "-std=c++17 -fPIC -shared -fno-exceptions -fno-rtti -O3 " +
                                         "-march=native -fvisibility=hidden -fvisibility-inlines-hidden " +
                                         "-Weverything -Wno-c++98-compat -Wno-c++98-compat-pedantic " +
                                         "-Wno-padded -Wno-missing-prototypes ./libherrington/dllmain.cpp " +
                                         "-o libherrington.so",
                UseShellExecute        = false,
                RedirectStandardOutput = false,
                RedirectStandardError  = true
            });

            //Dump output (Not reading the error stream causes trouble with outputting the shared object)
            System.Threading.Tasks.Task.Run(async () =>
            {
                using (FileStream fs = new FileStream("clang_error.txt", FileMode.Create))
                {
                    await clang.StandardError.BaseStream.CopyToAsync(fs);
                }
            }).GetAwaiter().GetResult();
        }

        public List<KeyValuePair<string, int>> GetRoleCounts(string compName)
        {
            //Find the comp
            var comp = this.Compositions.Find((c) => string.Equals(compName, c.Name)).Layout;

            //Count the occurrences of the roles in this comp
            return this.GetRoles()
                       .Select  ((r) =>
                       {
                           return new KeyValuePair<string, int>(r, comp.Count((s) => string.Equals(r, s)));
                       })
                       .ToList();
        }

        public List<string> GetRoles()
        {
            return this.Compositions
                       .Select   ((desc) => desc.Layout.Distinct())
                       .Aggregate((i, j) => i.Union(j))
                       .ToList   ();
        }

        public List<string> GetCompNames()
        {
            return this.Compositions
                       .Select((desc) => desc.Name)
                       .ToList();
        }

        public int GetCompIndex(string name)
        {
            return this.Compositions
                       .FindIndex((c) => string.Equals(c.Name, name));
        }

        public int GetUserSizeInBytes()
        {
            //First calculate the base size
            var baseSize = sizeof(ulong) + sizeof(float) * this.GetRoles().Count;

            //Add padding so that the total size is some multiple of sizeof(ulong)
            return (baseSize + sizeof(ulong)) & ~(sizeof(ulong) - 1);
        }

        public int GetOutputBlockSizeInBytes()
        {
            return 16;
        }
    }
}
