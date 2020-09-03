
using System;
using System.IO;
using TrickyUnits;

namespace ApolloBuild {
    class Project {

        readonly string TrueProject;
        readonly string ProjectFile;
        readonly string ChangeLogFile;
        GINIE Config = null;
        GINIE Identify = null;

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
                var antwoord = Console.ReadKey(false);
                if (antwoord.Key == ConsoleKey.Y) { QCol.Green("Yes!\n"); return true; }
                if (antwoord.Key == ConsoleKey.N) { QCol.Red("No!\n"); return true; }
            } while (true);
        }

        string Ask(GINIE work,string cat,string tag,string question,string defaultvalue="",bool acceptnothing=false) {
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
        string Ask(string cat, string tag, string question, string defaultvalue = "", bool acceptnothing = false) => Ask(Config, cat, tag, question, defaultvalue, acceptnothing);

        bool StartProject() {
            if (!File.Exists(ProjectFile)) {
                QCol.Red("PROBLEM!\n");
                QCol.Magenta($"Project \"{TrueProject}\" has not been found!\n\n");
                if (!WaitForYes("Should I create that project")) return false;
                Directory.CreateDirectory(qstr.ExtractDir(ProjectFile));
            }
            Config = GINIE.FromFile(ProjectFile);
            Config.AutoSaveSource = ProjectFile;
            Identify = GINIE.FromSource($"[Project]\nBuilderGenerationStart={DateTime.Now}\n");
            Ask(Identify, "Project", "Name", "What is the name of the project?", qstr.StripAll(TrueProject));
            Ask(Identify, "Project", "ID", "What is the project ID");
            if (Config["Identify", "Signature"] == "") {
                Config["Identify", "Signature"] = $"{qstr.md5($"{DateTime.Now}{Config.ToSource()}")}::{qstr.md5($"{Rand.Int(1, 2000)}{Identify.ToSource()}")}";
                QCol.Doing("Created signature", Config["Identify", "Signature"]);
            }
            Identify["Project", "Sig"] = Config["Identify", "Signature"];
            return true;
        }


        public void Run() {
            if (!StartProject()) return;
        }



    }
}
