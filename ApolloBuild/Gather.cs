// Lic:
// Apollo Builder
// Gatherer
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

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TrickyUnits;

namespace ApolloBuild {

    class Package {
        readonly public string Output = "";
        //readonly public Dictionary<string, List<string>> files=new Dictionary<string, List<string>>();
         public int modified = 0;
         public int deleted = 0;
         public int added = 0;
        public int forced = 0;
        private Package(string O) { Output = O; }

        static Dictionary<string, Package> Map = new Dictionary<string, Package>();
        public static Package Get(Project prnt,string p) {
            p = p.Trim().ToUpper();
            var toutput = "";
            if (p=="MAIN") {
                toutput = qstr.StripDir(prnt.TrueProject);
            } else do {                
                toutput = prnt.Ask($"PACKAGE::{p}", "Output", $"Package '{p}' should output to JCR: ",$"{p}");
                if (p.IndexOf('/') > 0 || p.IndexOf('\\') > 0) {
                    QCol.QuickError("Foldering not allowed!");
                    prnt.Kill($"PACKAGE::{p}", "Output");
                } else break;
            } while (true);
            var ret = new Package(toutput);
            var mrg = prnt.Yes($"PACKAGE::{p}", "MergeOnRelease", "Should I merge that package into the big package upon release");
            if (p != "MAIN"/* <= Must be preset or infinite loop is the result! */ && mrg && MainClass.CLIConfig.GetBool("r"))
                return Get(prnt, "MAIN");
            else
                return ret;
        }
    }

    class TGathered {
        public string OriginalFile = "";
        public string StoreAs = "";
        public string Package = "";
        public string Author = "";
        public string Notes = "";
        Project Parent;
        public TGathered(Project P) { Parent = P; }
    }

    partial class Project {

        List<TGathered> Gathered = new List<TGathered>();
        List<string> WantedLibraries = new List<string>();
        List<string> ProcessedLibraries = new List<string>();

        void Gather(string src) {
            if (src != "." && !Yes($"Dir::{src}", "Allowed", $"May I gather Directory {src}")) {
                Verbose.Doing("Skipping", src);
                return;
            }
            QCol.Doing("Gathering", src);
            var Author = Ask($"Dir::{src}", "Author", "Author:", src);
            var Notes = Ask($"Dir::{src}", "Notes", "Notes:", "", true);
            var addtopackage = "MAIN"; if (MultiDir) addtopackage = Ask($"Dir::{src}", "Package", "Packages", "MAIN").ToUpper();
            var pkg = Package.Get(this,addtopackage);
            var lijst = FileList.GetTree($"{InputDir}/{src}");
            var loglijst = ChangeLog.List($"Dir::{src}", "Files");
            var gupd = false;
            foreach (string chkdl in loglijst) {
                if (!lijst.Contains(chkdl)) {
                    pkg.deleted++;
                    Verbose.Doing("Vanished", chkdl);
                    gupd = true;
                }
            }
            var cat = $"Dir::{src}";
            foreach (string chkmd in lijst) {
            var upd = false;
                var info = new FileInfo($"{InputDir}/{src}/{chkmd}");
                var fullfilename = $"{InputDir}/{src}/{chkmd}";
                var hash = "NOT CHECKED";
                if (info.Length < 500000) { hash = qstr.md5(QuickStream.LoadString(fullfilename)); }
                if (MainClass.CLIConfig.GetBool("f")) {
                    Verbose.Doing("Forced", chkmd);
                    pkg.forced++;
                    upd = true;
                }
                if (!loglijst.Contains(chkmd)) {
                    pkg.added++;
                    Verbose.Doing("Added", chkmd);
                    upd = true;
                } else if (qstr.ToInt(ChangeLog[cat,$"SIZE::{chkmd}"])!=info.Length || ChangeLog[cat,$"TIME::{chkmd}"]!=$"{info.LastWriteTime}" || ChangeLog[cat,$"HASH::{chkmd}"]!=hash) {
                    Verbose.Doing("Modified", chkmd);
                    upd = true;
                }
                gupd = gupd || upd;
                if (upd) {
                    ChangeLog[cat, $"SIZE::{chkmd}"] = $"{info.Length}";
                    ChangeLog[cat, $"TIME::{chkmd}"] = $"{info.LastWriteTime}";
                    ChangeLog[cat, $"HASH::{chkmd}"] = hash;
                }
                // TODO: Scan for libraries if script!
            var NG = new TGathered(this);
                NG.OriginalFile = fullfilename;
                NG.StoreAs = chkmd;
                NG.Package = addtopackage;
                NG.Author = Author;
                NG.Notes = Notes;
                Gathered.Add(NG);
            }
            if (gupd) {
                loglijst.Clear();
                foreach (var f in lijst) loglijst.Add(f);
            }
        }

    }
}