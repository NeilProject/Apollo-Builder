using System;
using System.Collections.Generic;
using TrickyUnits;

namespace Tricky_Apollo {

	/// <summary>
	/// Apollo Quick Script Compiler
	/// </summary>
	class AQSCompiler {


		#region Start Compilation
		/// <summary>
		/// Compile source (in split lines) to byte code
		/// </summary>
		/// <param name="Lines"></param>
		/// <returns></returns>
		static public AQSCompiler Compile(string[] Lines,string f="") {
			return new AQSCompiler(Lines,f);
		}
		static public AQSCompiler Compile(List<string> Lines, string f ="") => Compile(Lines.ToArray(),f);
		static public AQSCompiler Compile(string Source, string f="") => Compile(Source.Replace("\r", "").Split('\n'),f);
		static public AQSCompiler CompileFile(string File) => Compile(QuickStream.LoadLines(File), File);
		#endregion

		enum AQSType { Unknown,String,Int,Label};
		
		class AQSParam {
			internal readonly AQSIntruction parent;
			internal readonly AQSType Type = AQSType.Unknown;
			internal readonly string Value = "";
			internal int IntValue {
				get {
					switch (Type) {
						case AQSType.Int: return qstr.ToInt(Value);
						case AQSType.Label: return parent.parent.Labels[Value];
						default:
							return 0;
					}
				}
			}
			internal AQSParam(AQSIntruction _p,AQSType _T,string _V) { parent = _p; Type = _T; Value = _V; }
		}
		class AQSIntruction {
			internal AQSChunk parent;
			internal byte Code = 0;
			internal readonly List<AQSParam> Parameters = new List<AQSParam>();
		}
		class AQSChunk {
			internal readonly Dictionary<string, int> Labels = new Dictionary<string, int>();
			internal readonly List<AQSIntruction> Instructions = new List<AQSIntruction>();
			internal bool Link = false;
			internal string State = "";
			internal string Function = "";
		}

		readonly Dictionary<string, AQSChunk> Chunks = new Dictionary<string, AQSChunk>();
		public delegate string Includer(string file); 
		/// <summary>
		/// Replace with an other function, if you do not wish to include from the regular file system, but from another source
		/// </summary>
		public Includer Inc = delegate (string file) { return QuickStream.LoadString(file); };
		public bool Success { get; private set; } = false;
		public struct Fout {
			readonly public string Melding;
			readonly public int Regel;
			readonly public string File;
			public Fout(string M, int R, string F) { Melding = M; Regel = R;File = F; }
		}
		public Fout Error { get; private set; } = new Fout("Ok", 0, "");

		class TCmd { }

		class TFout : Exception {
			readonly internal int Line;
			readonly internal string File;
			readonly internal string M;
			public override string Message => M;
			internal TFout(string msg,int L=0,string F = "?") {
				M = msg;
				Line = L;
				File = F;
			}
		}


		string CurrentChunk = "";
		AQSChunk Chunk {
			get {
				CurrentChunk = CurrentChunk.Trim().ToUpper();
				if (CurrentChunk == "") throw new TFout("No chunk! Yet asked for one!");
				if (!Chunks.ContainsKey(CurrentChunk)) Chunks[CurrentChunk] = new AQSChunk();
				return Chunks[CurrentChunk];
			}
		}

		class MIns {
			internal readonly static Dictionary<string, MIns> reg = new Dictionary<string, MIns>();
			internal readonly byte Code;
			internal readonly string[] WantParams;
			private MIns(byte C, string[] WP) { Code = C; WantParams = WP; }
			private static void Register(string K, byte C, params string[] WP) { reg[K.ToUpper()] = new MIns(C, WP); }

			static MIns() {
				Register("EXIT", 0);
				Register("CALL", 1, "*INF*");
				Register("JMP", 2, "LABEL");
				Register("CHK", 3, "STRING");
				Register("CMP", 4, "STRING", "AUTO");
				Register("CMPL", 5, "STRING", "AUTO");
				Register("CMPG", 6, "STRING", "AUTO");
				Register("JMPTRUE", 7, "LABEL");
				Register("JMPFALSE", 8, "LABEL");
				Register("MOV", 9, "STRING", "AUTO");
				Register("INC", 10, "STRING");
				Register("DEC", 11, "STRING");
				Register("ADD", 12, "STRING", "AUTO");
				Register("SUB", 13, "STRING", "AUTO");
				Register("MUL", 14, "STRING", "AUTO");
				Register("DIV", 15, "STRING", "AUTO");
				Register("ADD2", 16, "STRING", "STRING", "AUTO");
				Register("SUB2", 17, "STRING", "STRING", "AUTO");
				Register("MUL2", 18, "STRING", "STRING", "AUTO");
				Register("DIV2", 19, "STRING", "STRING", "AUTO");
			}
		}

		void C(string[] Lines, string f) {
			for (int i = 0; i < Lines.Length; i++) {
				var plist = new List<string>();
				string Regel = Lines[i].Trim();
				if (Regel.Length > 0 && Regel[0] != ';') {
					int RegelNummer = i + 1;
					var space1st = Regel.IndexOf(' ');
					string cmd = "";
					string param = "";
					if (space1st >= 0) {
						cmd = Regel.Substring(0, space1st).ToUpper();
						param = Regel.Substring(space1st + 1).Trim();
					} else {
						cmd = Regel.ToUpper().Trim();
					}
					if (Regel[0] == ':') {
						var L = qstr.Right(Regel, Regel.Length - 1).Trim().ToUpper();
						if (L == "") throw new TFout("Label syntax error", RegelNummer, f);
						if (CurrentChunk == "") throw new TFout("Label requires chunk", RegelNummer, f);
						if (Chunk.Labels.ContainsKey(L)) throw new TFout($"Duplicate label {L}", RegelNummer, f);
						Chunk.Labels[L] = Chunk.Instructions.Count;
					} else {
						switch (cmd) {
							case "INC":
							case "INCLUDE": {
									if (param == "") throw new TFout("Include without file", RegelNummer, f);
									var incf = param.Replace("\\", "/");
									if (!(param[0] == '/' || (param.Length > 2 && param[1] == ':'))) incf = $"{qstr.ExtractDir(f)}/{incf}";
									C(QuickStream.LoadLines(incf), incf);
								}
								break;
							case "CHUNK": {
									if (param == "") throw new TFout("Nameless chunk", RegelNummer, f);
									CurrentChunk = param.ToUpper();
									Chunk.Link = false; // Not needed, but forces the compiler to create the chunk record
								}
								break;
							case "SCHUNK": // State chunk
								{
									if (param == "") throw new TFout("Dataless State Chunk", RegelNummer, f);
									var p = param.Split(',');
									if (p.Length != 3) throw new TFout("State Chunk Syntax Error", RegelNummer, f);
									var chnk = p[0].ToUpper();
									var state = p[1];
									var func = p[2];
									if (Chunks.ContainsKey(chnk)) throw new TFout("Cannot make a state chunk out of an existing chunk");
									Chunks[chnk] = new AQSChunk();
									Chunks[chnk].Link = true;
									Chunks[chnk].State = state;
									Chunks[chnk].Function = func;
									CurrentChunk = "";
								}
								break;
							case "CSCHUNK": // Current State chunk
							case "CCHUNK": {
									if (param == "") throw new TFout("Dataless Current State Chunk", RegelNummer, f);
									var p = param.Split(',');
									if (p.Length != 2) throw new TFout("State Current Chunk Syntax Error", RegelNummer, f);
									var chnk = p[0].ToUpper();
									var func = p[1];
									if (Chunks.ContainsKey(chnk)) throw new TFout("Cannot make a state chunk out of an existing chunk");
									Chunks[chnk] = new AQSChunk();
									Chunks[chnk].Link = true;
									Chunks[chnk].State = "*CALLEDFROM*";
									Chunks[chnk].Function = func;
									CurrentChunk = "";
								}
								break;
							default: {
									var cp = "";
									if (CurrentChunk == "") throw new TFout("Chunkless instruction", RegelNummer, f);
									uint haakjes = 0;
									uint haakjesh = 0;
									uint accolades = 0;
									bool instring = false;
									bool escape = false;
									for (int j = 0; j < param.Length; j++) {
										if (instring) {
											if (escape) {
												switch (param[j]) {
													case 'n': cp += "\n"; break;
													case 'r': cp += '\r'; break;
													case '\\':
													case '"':
														cp += param[j];
														break;
													default:
														throw new TFout("Unknown escape");
												}
												escape = false;
											} else {
												switch (param[j]) {
													case '"':
														cp += '"';
														instring = false;
														break;
													case '\\':
														escape = true;
														break;
													default:
														cp += param[j];
														break;
												}
											}
										} else {
											switch (param[j]) {
												case '"':
													instring = true;
													cp += '"';
													break;
												case '(':
													haakjes++;
													cp += "(";
													break;
												case ')':
													if (haakjes == 0) throw new TFout(") without (", RegelNummer, f);
													haakjes--;
													cp += ")";
													break;
												case '[':
													haakjesh++;
													cp += "[";
													break;
												case ']':
													if (haakjesh == 0) throw new TFout("] without [", RegelNummer, f);
													haakjesh--;
													cp += "]";
													break;
												case '{':
													accolades++;
													cp += "{";
													break;
												case '}':
													if (accolades == 0) throw new TFout("} without {", RegelNummer, f);
													accolades--;
													cp += "}";
													break;
												case ',':
													if (haakjes == 0 && accolades == 0 && haakjesh == 0) {
														plist.Add(cp);
														cp = "";
													}
													break;
												case ';':
													goto the_end;
												default:
													cp += param[j];
													break;
											}
										}
									}
								the_end:
									if (haakjes > 0) throw new TFout("( without )", RegelNummer, f);
									if (haakjesh > 0) throw new TFout("[ without ]", RegelNummer, f);
									if (accolades > 0) throw new TFout("{ without }", RegelNummer, f);
									if (instring) throw new TFout("Unfinished string", RegelNummer, f);
									if (cp.Length > 0) plist.Add(cp);
									if (!MIns.reg.ContainsKey(cmd)) throw new TFout($"Unknown instruction {cmd}", RegelNummer, f);
									var INS = new AQSIntruction();
									INS.parent = Chunk;
									INS.Code = MIns.reg[cmd].Code;
									Chunk.Instructions.Add(INS);
									int pi = 0;
									foreach (var pprs in MIns.reg[cmd].WantParams) {
										if (pi >= plist.Count) throw new TFout($"<{pprs}> expected for parameter #{pi + 1}, but no more data is given!", RegelNummer, f);
										switch (pprs) {
											case "STRING":
												INS.Parameters.Add(new AQSParam(INS, AQSType.String, plist[pi++]));
												break;
											case "INT":
											case "INTEGER":
												INS.Parameters.Add(new AQSParam(INS, AQSType.Int, plist[pi++]));
												break;
											case "LABEL":
												INS.Parameters.Add(new AQSParam(INS, AQSType.Label, plist[pi++]));
												break;
											case "AUTO":
												if (qstr.IsInt(plist[pi]))
													INS.Parameters.Add(new AQSParam(INS, AQSType.Int, plist[pi++]));
												else
													INS.Parameters.Add(new AQSParam(INS, AQSType.String, plist[pi++]));
												break;
											case "*INF*":
											case "*INFINITY*":
												while (pi < plist.Count) {
													if (qstr.IsInt(plist[pi]))
														INS.Parameters.Add(new AQSParam(INS, AQSType.Int, plist[pi++]));
													else
														INS.Parameters.Add(new AQSParam(INS, AQSType.String, plist[pi++]));
												}
												break;
											default:
												throw new TFout($"Internal error!\n\nUnknown parameter type {pprs} in instrunction {cmd}", RegelNummer, f);
										}
									}
								}
								break;
						}

					}
				}
			}
			// Label check
			foreach (var Ch in Chunks) {
				foreach (var Inst in Ch.Value.Instructions) {
					foreach(var Para in Inst.Parameters) {
						if (Para.Type == AQSType.Label && (!Ch.Value.Labels.ContainsKey(Para.Value))) throw new TFout($"Reference to non-existent label '{Para.Value} in chunk {Ch.Key}", 0, f);
					}
				}
			}
		}

		public void WriteOut(QuickStream bt) {
			bt.Write($"\rAQSC{(char)26}");
			bt.Write((byte)8); // 8 bit unsigned integer used for instruction tags
			bt.Write((byte)0xff); // End of config, unused yet, but could be of use in later versions of AQL.
			foreach(var ch in Chunks) {
				if(ch.Value.Link) {
					bt.WriteByte(1);
					bt.WriteString(ch.Key);
					bt.WriteString(ch.Value.State);
					bt.WriteString(ch.Value.Function);
				} else {
					bt.WriteByte(2);
					bt.WriteString(ch.Key);
					bt.WriteLong(ch.Value.Instructions.Count);
					foreach (var Ins in ch.Value.Instructions) {
						bt.Write(Ins.Code);
						bt.WriteLong(Ins.Parameters.Count);
						foreach (var para in Ins.Parameters) {
							bt.Write((byte)para.Type);
							switch (para.Type) {
								case AQSType.String:
									bt.Write(para.Value);
									break;
								case AQSType.Int:
									bt.Write(para.IntValue);
									break;
								case AQSType.Label:
									bt.WriteLong(para.IntValue);
									break;
								default:
									throw new Exception($"Unknown parameter type {para.Type}");
							}
						}
					}
				}
			}
			bt.WriteByte(255);
		}

		public byte[] WriteOut() {
			var btms = new System.IO.MemoryStream();
			var btqs = new QuickStream(btms);
			WriteOut(btqs);
			var ret = btms.ToArray();
			btqs.Close();
			btms.Close();
			return ret;
		}

		AQSCompiler(string[] Lines,string f) {
			Success = true;
			try {
				C(Lines, f);
			} catch (TFout e) {
#if DEBUG
				Error = new Fout($"{e.Message}\n\n{e.StackTrace}", e.Line, e.File);
#else
				Error = new Fout($"{e.Message}", e.Line, e.File);
#endif
				Success = false;
			} catch(Exception e) {
				Error = new Fout($".NET Error\n{e.Message}\n{e.StackTrace}",0,f);
				Success = false;
			}
		}


	}
}