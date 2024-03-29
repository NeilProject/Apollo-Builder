// Lic:
// Apollo Builder
// Gatherer
// 
// 
// 
// (c) Jeroen P. Broks, 2020, 2021
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
// Version: 21.07.18
// EndLic

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TrickyUnits;
using UseJCR6;

namespace ApolloBuild {

	class Package {
		readonly public string Output = "";
		//readonly public Dictionary<string, List<string>> files=new Dictionary<string, List<string>>();
		public int modified = 0;
		public int deleted = 0;
		public int added = 0;
		public int forced = 0;
		private Package(string O) { Output = O; }

		static internal Dictionary<string, Package> Map = new Dictionary<string, Package>();
		public static Package Get(Project prnt,string p) {
			p = p.Trim().ToUpper();
			if (p != "MAIN" && !Map.ContainsKey("MAIN")) Get(prnt, "MAIN").modified++; // Be sure MAIN exists!
			string toutput;
			if (p=="MAIN") {
				toutput = qstr.StripDir(prnt.TrueProject);
			} else do {                
				toutput = prnt.Ask($"PACKAGE::{p}", "Output", $"Package '{p}' should output to JCR: ",$"{p}");
				if (p.IndexOf('/') > 0 || p.IndexOf('\\') > 0) {
					QCol.QuickError("Foldering not allowed!");
					prnt.Kill($"PACKAGE::{p}", "Output");
				} else break;
			} while (true);
			Package ret;
			if (!Map.ContainsKey(p)) {
				ret = new Package(toutput);
				Map[p] = ret;
				Debug.WriteLine($"Creating Package Map entry {p}");
			} else {
				ret = Map[p];
			}
			var mrg = p=="MAIN" || prnt.Yes($"PACKAGE::{p}", "MergeOnRelease", $"Should I merge package \"{p}\" into the big package upon release");
			if (!mrg) prnt.Yes($"PACKAGE::{p}", "Optional", "Is this package optional");
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
		public string Block = "";
		Project Parent;
		public TGathered(Project P) { Parent = P; }
	}
	

	partial class Project {
		
		List<TGathered> Gathered = new List<TGathered>();
		List<string> WantedLibraries = new List<string>();
		List<string> ProcessedLibraries = new List<string>();

		void ScanForLibs(string script,string ext) {
			var lines = QuickStream.LoadLines(script);
			string prefix;
			string suffix = "\"";
			bool casematters = true;
			switch (ext) {
				case "lua":
					prefix = "-- needapollolib \"";
					break;
				case "nil":
					prefix = "#use \"libs/";
					break;
				case "neil":
					prefix = "#use \"libs/";
					casematters = false;
					break;
				default:
					PackErrors++;
					QCol.QuickError("Scanning unknown script type for libraries");
					return;
			}
			foreach (var rline in lines) {
				var line = rline.Trim();
				var work = line;
				if (!casematters) work = work.ToLower();
				if (qstr.Prefixed(work,prefix) && qstr.Suffixed(work,suffix)) {
					var lib = work.Substring(prefix.Length); //Console.WriteLine($"-- work:{work} / lib:{lib}");
					lib = lib.Substring(0, lib.Length-(suffix.Length));
					//Console.WriteLine($"-- work:{work} / lib:{lib}");
					if (!WantedLibraries.Contains(lib) && !ProcessedLibraries.Contains(lib)) {
						Verbose.Doing("Requested", lib);
						WantedLibraries.Add(lib);
					}
				}
			}
		}

		//void Gather(string src, string storeprefix="") {
		void Gather(string src, bool lib=false, string storeprefix = "") {
			var FullSrc = $"{InputDir}/{src}"; if (lib) FullSrc = src;
			Verbose.Doing("Analysing", src);
			if (lib==false && src != "." && !Yes($"Dir::{src}", "Allowed", $"May I gather Directory {src}")) {
				Verbose.Doing("Skipping", src);
				return;
			}
			QCol.Doing("Gathering", src);
			bool useinlib = false;
			string Author;
			string Notes;
			if (lib) {
				var libfile = $"{src}/.ApolloGather.ini";
				useinlib = File.Exists(libfile);
				if (!useinlib) {
					useinlib = Yes($"LIBRARY::{src}", "CreateIfNoDataIsThere", $"A library({src}) is requested without any data ({libfile}) in it! Create it");

				}
				if (useinlib) {
					GINIE LibIni = GINIE.FromFile(libfile);
					LibIni.AutoSaveSource = libfile;
					Author = Ask(LibIni,$"Meta", "Author", "LIB: Lib Author:");
					Notes = Ask(LibIni,$"Meta", "Notes", "LIB: Lib Notes:", "", true);
				} else {
					Author = Ask($"LIBRARY::{src}", "Author", "PRJ: Lib Author:"); 
					Notes = Ask($"LIBRARY::{src}", "Notes", "PRJ: Lib Notes:", "", true); 
				}
			} else {
				Author = Ask($"Dir::{src}", "Author", "Author:", src);
				Notes = Ask($"Dir::{src}", "Notes", "Notes:", "", true);
			}
			var addtopackage = "MAIN"; 
			if (lib) addtopackage = Ask("Libraries", "Package", "Which package do you want to use for libraries? ").ToUpper();
			else if (MultiDir) addtopackage = Ask($"Dir::{src}", "Package", "Packages", "MAIN").ToUpper();
			//Console.WriteLine(addtopackage);
			var pkg = Package.Get(this,addtopackage);
			var lijst = FileList.GetTree($"{FullSrc}");
			List<string> loglijst; if (lib) loglijst = ChangeLog.List($"LIBRARY::{src}","Files"); else loglijst = ChangeLog.List($"Dir::{src}", "Files");
			var gupd = false;
			foreach (string chkdl in loglijst) {
				if (!lijst.Contains(chkdl)) {
					pkg.deleted++;
					Verbose.Doing("Vanished", chkdl);
					gupd = true;
				}
			}
			var cat = $"Dir::{src}";
			if (lib) cat = $"LIBRARY::{src}";
			foreach (string chkmd in lijst) {
				var upd = false;
				var info = new FileInfo($"{FullSrc}/{chkmd}");
				var fullfilename = $"{FullSrc}/{chkmd}";
				var hash = "NOT CHECKED";
				var toblock = "";
				if (qstr.ExtractExt(chkmd.ToLower()) == "lua" || qstr.ExtractExt(chkmd.ToLower()) == "nil" || qstr.ExtractExt(chkmd.ToLower()) == "neil") {
					ScanForLibs($"{FullSrc}/{chkmd}", qstr.ExtractExt(chkmd.ToLower()));
					if (AllowBlocks && qstr.ExtractExt(chkmd.ToLower()) == "neil" && qstr.ExtractExt(qstr.ExtractDir(chkmd.ToLower())) == "neilbundle" && (!File.Exists($"{FullSrc}/{qstr.ExtractDir(chkmd)}/_neilbundle.neil"))) toblock = qstr.ExtractDir(chkmd).ToUpper();
				}
				if (AllowBlocks && qstr.ExtractExt(chkmd.ToLower()) == "png" && qstr.ExtractExt(qstr.ExtractDir(chkmd.ToLower())) == "jpbf" && (!File.Exists($"{FullSrc}/{qstr.ExtractDir(chkmd)}/_neilbundle.neil"))) toblock = qstr.ExtractDir(chkmd).ToUpper();
				if (AllowBlocks && qstr.ExtractExt(chkmd.ToLower()) == "jpbf") toblock = $"PICTURE BUNDLE: {chkmd}";
				else if (AllowBlocks && JCR6.Recognize($"{FullSrc}/{chkmd}") == "JCR6") {
					var J = JCR6.Dir($"{FullSrc}/{chkmd}");
					if (J.Exists("Data") && J.Exists("Objects") && Yes("Kthura_Blocks",fullfilename,$"File {fullfilename} has been recognized as a possible Kthura Map.\nHave it block?")) toblock = $"Kthura Map: {chkmd}";
				}
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
				} else if (qstr.ToInt(ChangeLog[cat, $"SIZE::{chkmd}"]) != info.Length || ChangeLog[cat, $"TIME::{chkmd}"] != $"{info.LastWriteTime}" || ChangeLog[cat, $"HASH::{chkmd}"] != hash) {
					Verbose.Doing("Modified", chkmd);
					pkg.modified++;
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
				NG.StoreAs = $"{storeprefix}{chkmd}";
				NG.Package = addtopackage;
				NG.Author = Author;
				NG.Notes = Notes;
				NG.Block = toblock;
				Gathered.Add(NG);
			}
			if (gupd) {
				loglijst.Clear();
				foreach (var f in lijst) loglijst.Add(f);
			}
			//Verbose.Doing($"Number of entries gatherd in '{src}'",$"{Gathered.Count}");
		}
	}
}