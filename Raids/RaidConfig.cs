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
            Roles        = new List<string> { "MES", "HEAL", "DPS", "SLAVE" },
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

        public List<string>          Roles        { get; set; }
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

            //Update role list
            this.Roles = this.Compositions
                             .Select   ((desc) => desc.Layout.Distinct())
                             .Aggregate((i, j) => i.Union(j))
                             .ToList   ();
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

        public void GenerateSolverDLL()
        {
            //TODO
        }

        public void GenerateImplementationsHeader()
        {
            //Grab the composition names
            var compNames = this.Compositions.Select((c) => c.Name);

            //Prepare the text
            var str = $"#ifndef BILLY_HERRINGTON_IMPLEMENTATIONS_H\n" +
                      $"#define BILLY_HERRINGTON_IMPLEMENTATIONS_H\n" +
                      $"#pragma once\n" +
                      $"\n" +
                      $"namespace billy_herrington\n" +
                      $"{{\n" +
                      $"    enum class Implementation : unsigned int\n" +
                      $"    {{\n" +
                      $"        {string.Join(",\n        ", compNames)},\n" +
                      $"        COUNT\n" +
                      $"    }};\n" +
                      $"}};\n" +
                      $"\n" +
                      $"#endif\n";

            //Write to file
            File.WriteAllText("implementations.h", str);
        }

        public void GenerateSolverCPP(/*CompDescription compDescription*/)
        {
            var str = "#include \"typelist.h\"\n" +
                      "#include \"solver.h\"\n" +
                      "#include \"registry.h\"\n" +
                      "#include \"implementations.h\"\n" +
                      "\n" +
                      "#include <cstring>\n" +
                      "\n" +
                      "using namespace billy_herrington;\n" +
                      "\n" +
                      "/* Configuration */\n" +
                      "\n" +
                      "struct MES;\n" +
                      "struct HEAL;\n" +
                      "struct DPS;\n" +
                      "struct SLAVE;\n" +
                      "struct KITER;\n" +
                      "\n" +
                      "using roles       = tl::typelist_t<MES, HEAL, DPS, SLAVE, KITER>;\n" +
                      "using composition = tl::typelist_t\n" +
                      "<\n" +
                      "    MES, HEAL, DPS, DPS, SLAVE,\n" +
                      "    MES, HEAL, DPS, DPS,\n" +
                      "    KITER\n" +
                      ">;\n" +
                      "\n" +
                      "using solver_config = comp_solver::solver_config<roles, composition>;\n" +
                      "using solver        = comp_solver::solver       <solver_config>;\n" +
                      "\n" +
                      "/* Specialized implementation */\n" +
                      "\n" +
                      "void solve_dhuum\n" +
                      "(\n" +
                      "    solver_config::roster_t::const_pointer roster, int length,\n" +
                      "    solver_config::comp_t::pointer output\n" +
                      ") noexcept\n" +
                      "{\n" +
                      "    //Create the solver\n" +
                      "    solver s{ roster, length };\n" +
                      "\n" +
                      "    //Solve it!\n" +
                      "    s.solve();\n" +
                      "\n" +
                      "    //Write solution to the output\n" +
                      "    std::memcpy(output, s.get_solution().data(), sizeof(solver_config::comp_t));\n" +
                      "}\n" +
                      "\n" +
                      "/* Registering */\n" +
                      "\n" +
                      "static bool r = registry::register_function(Implementation::DHUUM, reinterpret_cast<registry::solver_func>(&solve_dhuum));\n";

            //Write to file
            File.WriteAllText("solver_dhuum.cpp", str);
        }
    }
}
