// Lic:
// Apollo Builder
// Engine copyer (for release building only)
// 
// 
// 
// (c) Jeroen P. Broks, 2021
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrickyUnits;

namespace ApolloBuild {
	class Engines {
		readonly string MainExe;
		readonly string ARF;
		readonly string[] DepenendenciesInSameDir;

		private Engines(string MExe, string _ARF,params string[] Dep) {
			MainExe = MExe;
			ARF = _ARF;
			DepenendenciesInSameDir = Dep;
		}

		static public Engines Get(string s) {
			Init();
			s = s.ToUpper();
			// foreach(var dbg in Register) { QCol.Doing("Got", s, " ");  QCol.Doing("Key", dbg.Key, " "); QCol.Doing("Addr", $"{ dbg.Value}"); } // debug only
			if (!Register.ContainsKey(s)) return null; else return Register[s];
		}

		public void Copy(Project Prj) {
			var ODir = Dirry.AD(MainClass.GlobConfig["Builder_Releases", Prj.GetIdentify("Engine", "Sub")]);
			var ExeTar = $"{Prj.OutputDir}/{qstr.StripDir(Prj.TrueProject)}.exe";
			var ExeOri = $"{ODir}/{MainExe}";
			var ARFTar = $"{Prj.OutputDir}/{qstr.StripDir(Prj.TrueProject)}.arf";
			var ARFOri = $"{ODir}/{ARF}";
			try {
				QCol.Doing("Copying", ExeOri, "");
				QCol.Yellow(" => ");
				QCol.Cyan($"{ExeTar}\n");
				File.Copy(ExeOri, ExeTar);
				QCol.Doing("Copying", ARFOri, "");
				QCol.Yellow(" => ");
				QCol.Cyan($"{ARFTar}\n");
				File.Copy(ARFOri, ARFTar);
				foreach (var file in DepenendenciesInSameDir) {
					QCol.Doing("Copying", $"{ODir}/{file}");
					File.Copy($"{ODir}/{file}", $"{Prj.OutputDir}/{file}");
				}
				QCol.Green("Success\n\n");
			} catch(Exception E) {
				QCol.QuickError(E.Message);
			}
		}


		static Dictionary<string, Engines> Register = new Dictionary<string, Engines>();

		private static bool doneInit = false;
		static public void Init() {
			if (doneInit) return; doneInit = true;
			Register["GAMECPSDL"] = new Engines(
				"Apollo Game Engine.exe",
				"Apollo Game Engine.arf",
				"libtiff-5.dll",
				"zlib1.dll",
				"SDL2_image.dll",
				"libwebp-7.dll",
				"libpng16-16.dll",
				"libjpeg-9.dll",
				"SDL2_mixer.dll",
				"libmodplug-1.dll",
				"libopus-0.dll",
				"libogg-0.dll",
				"libvorbis-0.dll",
				"libopusfile-0.dll",
				"libvorbisfile-3.dll",
				"libmpg123-0.dll",
				"libFLAC-8.dll",
				"SDL2_ttf.dll",
				"Lua.dll");
		}
	}
}