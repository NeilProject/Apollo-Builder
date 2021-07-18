// Lic:
// Apollo - Builder
// Builds Apollo Project Packages with JCR6
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
using TrickyUnits;
using UseJCR6;

namespace ApolloBuild {
    class MainClass {

        static public FlagParse CLIConfig { get; private set; } = null;

        static public GINIE GlobConfig = null;

        static public bool MkRelease => CLIConfig.GetBool("r");

        static void Head() {
            MKL.Version("Apollo Builder - Main.cs","21.07.18");
            MKL.Lic    ("Apollo Builder - Main.cs","GNU General Public License 3");
            QCol.White("Apollo Builder\n");
            QCol.Doing("Version", MKL.Newest);
            QCol.Magenta($"(c) Jeroen P. Broks {MKL.CYear(2020)}, released under the terms of the GPL3\n\n");
        }

        static void ParseCLIConfig(string[] args) {
            CLIConfig = new FlagParse(args);
            CLIConfig.CrBool("v", false); // Be extremely verbose
            CLIConfig.CrBool("f", false); // Force building sub-package, even when there seem to be no changes
            CLIConfig.CrBool("r", false); // Create big package for release (meaning subpackages will be packed in the big package)
            CLIConfig.Parse();
        }

        static void ShowHelp() {
            QCol.White("Usage: "); QCol.Yellow(qstr.StripAll(MKL.MyExe)); QCol.Blue(" [flags] "); QCol.Cyan("<Project>\n");
            Console.ResetColor();
            Console.WriteLine(MKL.All());
        }



        static void Main(string[] args) {
            Dirry.InitAltDrives();
            GlobConfig = GINIE.FromFile(Dirry.C("$Home$/.Tricky__ApplicationSupport/Apollo.ini"));
            GlobConfig.AutoSaveSource = Dirry.C("$Home$/.Tricky__ApplicationSupport/Apollo.ini");
            Project.InitEngineSpecific();
            QCol.DoingTab = 20;
            JCR6_lzma.Init();
            JCR6_zlib.Init();
            JCR6_jxsrcca.Init();
            JCR6_JXSDA.Init();
            new JCR6_WAD();
            new JCR_QuakePack();
            new JCR6_RealDir();
            new JCR_QuickLink();
            Head();
            ParseCLIConfig(args);
            if (CLIConfig.Args.Length == 0) {
                ShowHelp();
            } else {
                foreach (string p in CLIConfig.Args) {
                    var P = new Project(p);
                    P.Run();
                }
            }
            Console.ResetColor();        
            TrickyDebug.AttachWait();
        }
    }
}