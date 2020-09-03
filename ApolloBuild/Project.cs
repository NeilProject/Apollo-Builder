// Lic:
// Apollo Builder
// Project Basis
// 
// 
// 
// (c) Jeroen P. Broks, 2020
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// 
// Please note that some references to data like pictures or audio, do not automatically
// fall under this licenses. Mostly this is noted in the respective files.
// 
// Version: 20.09.03
// EndLic
???
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Net.NetworkInformation;
using TrickyUnits;

namespace ApolloBuild {
    enum ESTType { Regular, YesNo }
    struct EngineSpecific {
        public string cat;
        public string tag;
        public string question;
        public string defaultvalue;
        public bool allownothing;
        public ESTType type;
        public EngineSpecific(string c, string t, string q, string d="", bool an=false, ESTType tp = ESTType.Regular) {
            cat = c;
            defaultvalue = d;
            tag = t;
            allownothing = an;
            question = q;
            type = tp;
        }
    }

    partial class Project {

        public readonly string TrueProject;
        readonly string ProjectFile;
        readonly public string ChangeLogFile;
        GINIE Config = null;
        GINIE Identify = null;
        GINIE ChangeLog = null;
        public bool Release = MainClass.CLIConfig.GetBool("r");
        public bool MultiDir => (Config["Project", "MultiDir"].ToUpper() == "YES" || Config["Project", "MultiDir"].ToUpper() == "JA" || Config["Project", "MultiDir"].ToUpper() == "TRUE" || Config["Project", "MultiDir"].ToUpper() == "WAAR");

        public string InputDir { private set; get; } = "";
        public string OutputDir { private set; get; } = "";

        public string IdentifySource => Identify.ToSource();

        static Dictionary<string, List<EngineSpecific>> EngineSpecificQuestions = new Dictionary<string,List<EngineSpecific>>();

        public static void InitEngineSpecific() {
            MKL.Version("Apollo Builder - Project.cs","20.09.03");
            MKL.Lic    ("Apollo Builder - Project.cs","GNU General Public License 3");
            // CP SDL game engine
            var L = new List<EngineSpecific>();
            L.Add(new EngineSpecific("Window", "Width", "Window Width:","1200"));
            L.Add(new EngineSpecific("Window", "Height", "Window Height:","1000"));
            L.Add(new EngineSpecific("Window", "Title", "Window Title:"));
            L.Add(new EngineSpecific("Window", "FullScreen", "Play game in full screen","",false,ESTType.YesNo));
            EngineSpecificQuestions["GameCPSDL"] = L;
        }


        public Project(string PRJ) {
            TrueProject = Dirry.AD(PRJ);
            ProjectFile = $"{TrueProject}.ApolloProject.ini";
            ChangeLogFile = $"{TrueProject}.AChangelog.ini";
        }

        bool WaitForYes(string Question) {
            QCol.Yellow($"{Question} ");
            QCol.Cyan("? ");
            QCol.Magenta("(Y/N) ");
            do {
                var antwoord = Console.ReadKey(true);
                if (antwoord.Key == ConsoleKey.Y) { QCol.Green("Yes!\n"); return true; }
                if (antwoord.Key == ConsoleKey.N) { QCol.Red("No!\n"); return true; }
            } while (true);
        }
        bool Yes(GINIE work,string cat, string tag,string question) {
            if (work == Identify) {
                Identify[cat, tag] = Config["Identify", $"{cat}::{tag}"];
            }
            if (work[cat, tag] != "") return (work[cat, tag].ToUpper() == "YES" || work[cat, tag].ToUpper() == "TRUE" || work[cat, tag].ToUpper() == "JA" || work[cat, tag].ToUpper() == "WAAR");
            if (WaitForYes(question))
                work[cat, tag] = "YES";
            else
                work[cat, tag] = "NO";
            if (work == Identify) {
                Config["Identify", $"{cat}::{tag}"] = work[cat,tag];
            }
           return work[cat, tag] == "YES"; 
        }
        public bool Yes(string cat, string tag, string question) => Yes(Config, cat, tag, question);

            public string Ask(GINIE work,string cat,string tag,string question,string defaultvalue="",bool acceptnothing=false) {
            if (work == Identify) {
                Identify[cat, tag] = Config["Identify", $"{cat}::{tag}"];
            }
            if (acceptnothing && work["Nothing", $"{work}::{cat}"] == "NADA") return defaultvalue;
            if (work[cat, tag] != "") return work[cat, tag];
            var antwoord = "";
            do {
                if (defaultvalue != "")
                    QCol.Blue($"[{defaultvalue}] ");
                QCol.Yellow($"{question} ");
                QCol.Cyan("");
                antwoord = Console.ReadLine().Trim();
                if (antwoord == "") antwoord = defaultvalue;
                if (antwoord == "" && acceptnothing) { work["Nothing", $"{work}::{cat}"] = "NADA"; return ""; }
            } while (antwoord == "");
            work[cat, tag] = antwoord;
            if (work==Identify) {
                Config["Identify", $"{cat}::{tag}"] = antwoord;
            }
            return antwoord;            
        }
        public string Ask(string cat, string tag, string question, string defaultvalue = "", bool acceptnothing = false) => Ask(Config, cat, tag, question, defaultvalue, acceptnothing);
       
        void Kill(GINIE work,string cat, string tag) {
            work[cat, tag] = "";
        }
        public void Kill(string cat, string tag) => Kill(Config, cat, tag);

        bool StartProject() {
            if (!File.Exists(ProjectFile)) {
                QCol.Red("PROBLEM!\n");
                QCol.Magenta($"Project \"{TrueProject}\" has not been found!\n\n");
                if (!WaitForYes("Should I create that project")) return false;
                Directory.CreateDirectory(qstr.ExtractDir(ProjectFile));
            }
            Config = GINIE.FromFile(ProjectFile);
            Config.AutoSaveSource = ProjectFile;
            ChangeLog = GINIE.FromFile(ChangeLogFile);            
            Identify = GINIE.FromSource($"[Project]\nBuilderGenerationStart={DateTime.Now}\n");
            Ask(Identify, "Project", "Name", "What is the name of the project?", qstr.StripAll(TrueProject));
            Ask(Identify, "Project", "ID", "What is the project ID? ");
            if (Config["Identify", "Signature"] == "") {
                Config["Identify", "Signature"] = $"{qstr.md5($"{DateTime.Now}{Config.ToSource()}")}::{qstr.md5($"{Rand.Int(1, 2000)}{Identify.ToSource()}")}";
                QCol.Doing("Created signature", Config["Identify", "Signature"]);
            }
            Identify["Project", "Sig"] = Config["Identify", "Signature"];
            Identify["Engine", "Main"] = "Apollo";
            Ask(Identify, "Engine", "Sub","Which specific engine of Apollo is this project for? ", "GameCPSDL");
            Ask(Identify, "Engine", "MinVersion", "What minimal version do you require? ", "00.00.00");
            Ask(Identify, "Meta", "Title", "Meta-Data.Title: ", Identify["Project", "Name"]);
            Ask(Identify, "Meta", "Author", "Meta-Data.Author: ");
            Ask(Identify, "Meta", "Copyright", "Meta-Data.Copyright: ");
            Ask(Identify, "Meta", "License", "Meta-Data.License: ");
            if (EngineSpecificQuestions.ContainsKey(Identify["Engine", "Sub"])) {
                foreach (var q in EngineSpecificQuestions[Identify["Engine", "Sub"]]) {
                    switch (q.type) {
                        case ESTType.Regular:
                            Ask(Identify,q.cat,q.tag,q.question,q.defaultvalue,q.allownothing);
                            break;
                        case ESTType.YesNo:
                            Yes(Identify, q.cat, q.tag, q.question);
                            break;
                        default:
                            throw new Exception("Unknown question type");
                        }
                }
            }
            InputDir=Dirry.AD(Ask("Project", "InputDir", "From which directory must I create this project? "));
            Yes("Project", "MultiDir", "This this project a mult-dir project? (say 'no' if you are not sure, as that is then likely the correct answer!");
            Ask("Project", "Compression", "Preferred compression algorithm: ", "Store");
            if (Release)
                OutputDir = Dirry.AD(Ask("Project", "Output::Release", "Were must I write the release package files? "));
            else
                OutputDir = Dirry.AD(Ask("Project", "Output::Debug", "Where must I write the debug package files?"));
            return true;
        }

        public void Run() {
            if (!StartProject()) return;
            if (MultiDir) {
                var Lijst = FileList.GetDir(InputDir, 2);
                foreach (var dir in Lijst) Gather(dir);
            } else {
                Gather(".");
            }
        }



    }
}