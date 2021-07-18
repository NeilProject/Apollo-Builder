// Lic:
// Apollo Builder
// Project Basis
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
using System.IO;
using TrickyUnits;
using UseJCR6;

namespace ApolloBuild {
	enum ESTType { Regular, YesNo }
	struct EngineSpecific {
		public string cat;
		public string tag;
		public string question;
		public string defaultvalue;
		public bool allownothing;
		public ESTType type;
		public EngineSpecific(string c, string t, string q, string d = "", bool an = false, ESTType tp = ESTType.Regular) {
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
		public bool AllowBlocks { private set; get; } = false;
		public string ReleaseEngineDir { private set; get; } = "";

		public string IdentifySource => Identify.ToSource();

		static Dictionary<string, List<EngineSpecific>> EngineSpecificQuestions = new Dictionary<string, List<EngineSpecific>>();

		public string GetConfig(string c, string k) => Config[c, k];
		public string GetIdentify(string c, string k) => Identify[c, k];

		public static void InitEngineSpecific() {
			MKL.Version("Apollo Builder - Project.cs","21.07.18");
			MKL.Lic    ("Apollo Builder - Project.cs","GNU General Public License 3");
			// CP SDL game engine
			var L = new List<EngineSpecific>();
			L.Add(new EngineSpecific("Window", "Width", "Window Width:", "1200"));
			L.Add(new EngineSpecific("Window", "Height", "Window Height:", "1000"));
			L.Add(new EngineSpecific("Window", "Title", "Window Title:"));
			L.Add(new EngineSpecific("Window", "FullScreen", "Play game in full screen", "", false, ESTType.YesNo));
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
				if (antwoord.Key == ConsoleKey.N) { QCol.Red("No!\n"); return false; }
			} while (true);
		}
		bool Yes(GINIE work, string cat, string tag, string question) {
			if (work == Identify) {
				Identify[cat, tag] = Config["Identify", $"{cat}::{tag}"];
			}
			if (work[cat, tag] != "") return (work[cat, tag].ToUpper() == "YES" || work[cat, tag].ToUpper() == "TRUE" || work[cat, tag].ToUpper() == "JA" || work[cat, tag].ToUpper() == "WAAR");
			if (WaitForYes(question))
				work[cat, tag] = "YES";
			else
				work[cat, tag] = "NO";
			if (work == Identify) {
				Config["Identify", $"{cat}::{tag}"] = work[cat, tag];
			}
			return work[cat, tag] == "YES";
		}
		public bool Yes(string cat, string tag, string question) => Yes(Config, cat, tag, question);

		public string Ask(GINIE work, string cat, string tag, string question, string defaultvalue = "", bool acceptnothing = false) {
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
			if (work == Identify) {
				Config["Identify", $"{cat}::{tag}"] = antwoord;
			}
			return antwoord;
		}
		public string Ask(string cat, string tag, string question, string defaultvalue = "", bool acceptnothing = false) => Ask(Config, cat, tag, question, defaultvalue, acceptnothing);

		public List<string> ListAsk(GINIE work, string cat, string tag, string question) {
			if (!work.HasList(cat, tag)) {
				QCol.Yellow($"{question}\n");
				QCol.Magenta("Now type as many as you need, and when you need no more, just hit enter without anymore input\n");
				string answer;
				do {
					QCol.Red(">");
					QCol.Cyan("");
					answer = Console.ReadLine();
					if (answer != "") work.ListAdd(cat, tag, answer);
				} while (answer != "");
			}
			if (!work.HasList(cat, tag)) return null;
			return work.List(cat, tag);
		}

		public List<string> ListAsk(string cat, string tag, string question) => ListAsk(Config, cat, tag, question);

		void Kill(GINIE work, string cat, string tag) {
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
			Ask(Identify, "Engine", "Sub", "Which specific engine of Apollo is this project for? ", "GameCPSDL");
			Ask(Identify, "Engine", "MinVersion", "What minimal version do you require? ", "00.00.00");
			if (MainClass.MkRelease) {
				ReleaseEngineDir = Ask(MainClass.GlobConfig, "Builder_Releases", Identify["Engine", "Sub"], $"This project uses the {Identify["Engine", "Sub"]} engine! In which directory is the engine itself installed?");
			}

			Ask(Identify, "Meta", "Title", "Meta-Data.Title: ", Identify["Project", "Name"]);
			Ask(Identify, "Meta", "Author", "Meta-Data.Author: ");
			Ask(Identify, "Meta", "Copyright", "Meta-Data.Copyright: ");
			Ask(Identify, "Meta", "License", "Meta-Data.License: ");
			if (EngineSpecificQuestions.ContainsKey(Identify["Engine", "Sub"])) {
				foreach (var q in EngineSpecificQuestions[Identify["Engine", "Sub"]]) {
					switch (q.type) {
						case ESTType.Regular:
							Ask(Identify, q.cat, q.tag, q.question, q.defaultvalue, q.allownothing);
							break;
						case ESTType.YesNo:
							Yes(Identify, q.cat, q.tag, q.question);
							break;
						default:
							throw new Exception("Unknown question type");
					}
				}
			}
			InputDir = Dirry.AD(Ask("Project", "InputDir", "From which directory must I create this project? "));
			Yes("Project", "MultiDir", "This this project a mult-dir project? (say 'no' if you are not sure, as that is then likely the correct answer!");
			Ask("Project", "Compression", "Preferred compression algorithm: ", "Store");
			AllowBlocks = Yes("Project", "AllowSolid", "May I pack specific directories together in blocks? ");
			if (Release) {
				Ask("Project", "Console::Release", "Console in Windows? (Release) ");
				Identify["OS.Windows", "Console"] = (Config["Project", "Console::Release"] == "YES").ToString().ToUpper();
				OutputDir = Dirry.AD(Ask("Project", "Output::Release", "WARNING!\n\nContrary to debug directories 'release' directories will always be entirely emptied prior to build.\nSo make sure you use this directory ONLY for your release builds!\n\n\nWere must I write the release package files? "));
			} else {
				Ask("Project", "Console::Debug", "Console in Windows? (Debug) ");
				Identify["OS.Windows", "Console"] = (Config["Project", "Console::Debug"] == "YES").ToString().ToUpper();
				OutputDir = Dirry.AD(Ask("Project", "Output::Debug", "Where must I write the debug package files?"));
			}
			return true;
		}


		public void Run() {
			// Did we start well?
			if (!StartProject()) return;
			// Gather
			if (MultiDir) {
				var Lijst = FileList.GetDir(InputDir, 2);
				foreach (var dir in Lijst) Gather(dir);
			} else {
				Gather(".");
			}
			if (MainClass.MkRelease) {
				QCol.Doing("Destroying old", OutputDir);
				Directory.Delete(OutputDir, true);
				QCol.Doing("Creating new", OutputDir);
				Directory.CreateDirectory(OutputDir);
				QCol.Doing("Creating Release Resource", $"{OutputDir}/{qstr.StripDir(TrueProject)}.Apollo.jcr");
				ReleaseOut = new TJCRCreate($"{OutputDir}/{qstr.StripDir(TrueProject)}.Apollo.jcr", Config["Project", "Compression"], Config["Identify", "Signature"]);
			}

			// TODO: Lib collecting
			while (WantedLibraries.Count > 0) {
				var LibDirs = ListAsk("Libraries", "Directories", "In which directories can I find external libraries for this project?");
				var Lib = WantedLibraries[0];
				var SmLib = "";
				string LibDir = "";
				foreach (var D2 in LibDirs) {
					var D = Dirry.AD(D2);
					if (Directory.Exists($"{D}/{Lib}.NeilBundle")) { LibDir = $"{D}/{Lib}.NeilBundle"; SmLib = $"{Lib}.NeilBundle/"; break; }
					if (Directory.Exists($"{D}/{Lib}.nlb")) { LibDir = $"{D}/{Lib}.nlb"; SmLib = $"{Lib}.nlb/"; break; }
				}
				if (LibDir == "") {
					PackErrors++;
					QCol.QuickError($"Library {Lib} has not been found!");
					return;
				}
				Gather(LibDir, true, $"Libs/{SmLib}");
				ProcessedLibraries.Add(Lib);
				WantedLibraries.RemoveAt(0);
				//break; // infinite loop break while stuff is not yet done
			}
			// Pack
			foreach (var p in Package.Map.Keys)
				Pack(p, MainClass.CLIConfig.GetBool("f"));
			if (PackErrors == 1) {
				QCol.Yellow("\tThere was ");
				QCol.Cyan("1");
				QCol.Yellow(" error\n");
			} else {
				QCol.Yellow("\tThere were ");
				QCol.Cyan($"{PackErrors}");
				QCol.Yellow(" errors\n");
			}
			if (MainClass.MkRelease) {
				ReleaseOut.Close();
				QCol.Yellow("\n\nOutput executables and libraries\n");
				Engines.Init();
				var Eng = Engines.Get(Identify["ENGINE", "SUB"]);
				if (Eng == null)
					QCol.QuickError($"No such data available for enigine '{Identify["ENGINE", "SUB"]}'");
				else
					Eng.Copy(this);
			}
			if (PackErrors == 0) ChangeLog.SaveSource(ChangeLogFile);
		}



	}
}