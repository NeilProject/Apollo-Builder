// Lic:
// Apollo Builder
// Verbosing if asked
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using TrickyUnits;

namespace ApolloBuild {
    class Verbose {

        static bool bv => MainClass.CLIConfig.GetBool("v");

        static public void Printf(string format,params object [] stuff) {
            if (bv) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Fmt.printf(format, stuff);
            }
        }

        static public void Wln(params object[] a) {
            if (bv) {
                Console.ForegroundColor = ConsoleColor.Yellow;
                foreach (var i in a) Console.Write(a);
            }
            Console.WriteLine();
        }

        static public void Doing(string a1,string a2,string a3="\n") {
            if (bv) {
                QCol.Doing(a1, a2, a3);
            }
        }

    }
}