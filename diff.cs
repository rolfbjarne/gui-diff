//
// Authors:
//  Rolf Bjarne Kvinge (rolfbjarne@gmail.com)
//
// Copyright (C) 2012 Rolf Bjarne Kvinge
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace gui_diff
{
	public abstract class Diff
	{
		public static string PREFIX = string.Empty;
		public static Diff? Instance;

		public List<Entry> entries = new List<Entry> ();
		public bool list_dirty = true;
		public Entry? selected;

		public Diff ()
		{
		}

		public string GetDiff (Entry selected, bool? staged)
		{
			string diff;
			string color = "--no-color";

			if (selected == null) {
				if (staged.HasValue && staged.Value) {
					diff = Execute ("git", ["diff", "--staged", color], true);
				} else {
					diff = Execute ("git", ["diff", color], true);
				}
			} else {
				if (selected.untracked) {
					diff = File.ReadAllText (selected.filename);
				} else if (((selected.staged_whole || selected.staged) && !(staged.HasValue && !staged.Value)) || (staged.HasValue && staged.Value)) {
					diff = Execute ("git", ["diff", "--staged", color, "--", selected.filename], true);
				} else {
					diff = Execute ("git", ["diff", color, "--", selected.filename], true);
				}
			}

			return diff;
		}

		public void RefreshList ()
		{
			RefreshListNew ();

			entries.Sort ((a, b) => {
				// Fully staged files at the top
				if (a.staged_whole != b.staged_whole)
					return a.staged_whole ? -1 : 1;

				// Then staged (unmerged)
				if (a.staged != b.staged)
					return a.staged ? -1 : 1;

				// Untracked files at the bottom
				if (a.untracked != b.untracked)
					return a.untracked ? 1 : -1;

				//Console.WriteLine ($"Sorting {a.filename} vs {b.filename}: a.staged: {a.staged} b.staged: {b.staged} a.staged_whole: {a.staged_whole} b.staged_whole: {b.staged_whole}");

				// Finally sort by filename
				return string.CompareOrdinal (a.filename, b.filename);
			});

			list_dirty = false;
		}

		public void RefreshListOld ()
		{
			int Ac = 0, ADc = 0, DAc = 0, Dc = 0;
			List<string> diff = ExecuteToLines ("git", ["diff", "--name-only", "--ignore-submodules"]);
			List<string> staged = ExecuteToLines ("git", ["diff", "--name-only", "--staged"]);
			List<string> untracked = ExecuteToLines ("git", ["ls-files", "--other", "--exclude-standard"]);

			var all = new HashSet<string> (diff);

			selected = null;
			all.UnionWith (staged);

			entries.Clear ();
			foreach (var file in all) {
				if (PREFIX.Length > 1 && !file.StartsWith (PREFIX, StringComparison.Ordinal))
					continue;
				var entry = new Entry () {
					filename = file,
					staged = staged.Contains (file),
					staged_whole = !diff.Contains (file),
				};
				if (Directory.Exists (file)) {
					entry.is_directory = true;
				} else if (!File.Exists (file)) {
					entry.deleted = true;
				} else {
					entry.messed_up_eol = IsEolMessedUp (file, out Ac, out ADc, out DAc, out Dc, out var hasConflictMarkers, ref entry.is_binary);
					if (!entry.messed_up_eol) {
						if (Ac > 0) {
							entry.eol = "LF  ";
						} else if (ADc > 0) {
							entry.eol = "LFCR";
						} else if (DAc > 0) {
							entry.eol = "CRLF";
						} else if (Dc > 0) {
							entry.eol = "CR  ";
						}
					}
				}
				entries.Add (entry);
			}
			foreach (var file in untracked) {
				if (PREFIX.Length > 1 && !file.StartsWith (PREFIX, StringComparison.Ordinal))
					continue;
				var entry = new Entry () {
					filename = file,
					untracked = true,
				};
				entries.Add (entry);
			}
		}

		public void RefreshListNew ()
		{
			var status = ExecuteToLines ("git", ["status", "--porcelain", "--ignore-submodules"]);

			selected = null;

			entries.Clear ();
			foreach (var line in status) {
				if (line.Length < 3)
					continue;

				if (line [2] != ' ')
					throw new DiffException ($"Unexpected status line: {line}");

				var file = line [3..];

				if (file.Length == 0)
					throw new DiffException ($"Unexpected status line: {line}");

				string? renamed_from = null;
				var renamed = false;
				if (file [0] == '"') {
					var sb = new StringBuilder ();
					var inQuote = false;
					for (int i = 0; i < file.Length; i++) {
						var ch = file [i];
						if (ch == '"') {
							inQuote = !inQuote;
						} else if (inQuote) {
							sb.Append (ch);
						} else if (ch == '\\') {
							sb.Append (file [i + 1]);
							i++;
						} else if (ch == ' ') {
							renamed = true;
							renamed_from = sb.ToString ();
							sb.Clear ();
						} else {
							throw new DiffException ($"Unexpected status line: {line}");
						}
					}
					file = sb.ToString ();
				} else {
					var space = file.IndexOf (' ');
					if (space >= 0) {
						if (file [space..(space + 4)] == " -> ") {
							renamed = true;
							renamed_from = file.Substring (0, space);
							file = file.Substring (space + 4);
						} else {
							throw new DiffException ($"Unexpected status line: {line}");
						}
					}
				}						

				if (PREFIX.Length > 1 && !file.StartsWith (PREFIX, StringComparison.Ordinal))
					continue;

				var staged = false;
				var staged_whole = false;
				var untracked = false;
				var deleted = false;
				var conflict = false;
				var is_directory = Directory.Exists (file);

				var x = line [0];
				var y = line [1];

				// https://git-scm.com/docs/git-status
				switch (x) {
				case '?':
					if (y != '?')
						throw new DiffException ($"Unexpected status line: {line}");
					untracked = true;
					break;
				case 'M': // modified
				case 'T': // type changed
				case 'R': // renamed
				case 'C': // copied
					staged = true;
					staged_whole = y == ' ';
					break;
				case 'A': // added
					switch (y) {
					case 'A':
					case 'U':
						conflict = true;
						break;
					case ' ':
					case 'M':
					case 'D':
					case 'T':
						staged = true;
						staged_whole = y == ' ';
						break;
					default:
						throw new DiffException ($"Unexpected status line: {line}");
					}
					break;
				case 'D': // deleted
					deleted = true;
					switch (y) {
					case 'D':
					case 'U':
						conflict = true;
						break;
					case ' ':
						staged = true;
						staged_whole = true;
						break;
					default:
						throw new DiffException ($"Unexpected status line: {line}");
					}
					break;
				case 'U': // unmerged
					conflict = true;
					break;
				case ' ':
					break;
				default:
					throw new DiffException ($"Unexpected status line: {line}");
				}
		
				var entry = new Entry () {
					filename = file,
					staged = staged,
					staged_whole = staged_whole,
					renamed = renamed,
					renamed_from = renamed_from,
					untracked = untracked,
					deleted = deleted,
					is_directory = is_directory,
					conflict = conflict,
				};

				if (!entry.is_directory && !entry.deleted) {
					entry.messed_up_eol = IsEolMessedUp (file, out var Ac, out var ADc, out var DAc, out var Dc, out var hasConflictMarkers, ref entry.is_binary);
					entry.has_conflict_marker = hasConflictMarkers;
					if (!entry.messed_up_eol) {
						if (Ac > 0) {
							entry.eol = "LF  ";
						} else if (ADc > 0) {
							entry.eol = "LFCR";
						} else if (DAc > 0) {
							entry.eol = "CRLF";
						} else if (Dc > 0) {
							entry.eol = "CR  ";
						}
					}
				}
				entries.Add (entry);
			}
		}

		public List<string> ExecuteToLines (string cmd, IEnumerable<string> args)
		{
			var output = Execute (cmd, args);
			var lines = output.Split ([(char) 10, (char) 13], StringSplitOptions.RemoveEmptyEntries);
			return [.. lines];
		}

		public string Execute (string cmd, IEnumerable<string> args)
		{
			return Execute (cmd, args, true);
		}

		public string Execute (string cmd, IEnumerable<string> args, bool capture_stdout)
		{
			return Execute (cmd, args, capture_stdout, true);
		}

		public void Stage (string path)
		{
			Execute ("git", ["apply", "--index", "--cached", "--ignore-whitespace", "--ignore-space-change", path], true, true);
			list_dirty = true;
		}

		public string Execute (string cmd, IEnumerable<string> args, bool capture_stdout, bool wait_for_exit, bool use_shell_execute = false)
		{
			var std = new StringBuilder ();

			using (var stderr_event = new ManualResetEvent (false)) {
				using (var stdout_event = new ManualResetEvent (false)) {
					using (var p = new Process ()) {
						p.StartInfo.UseShellExecute = use_shell_execute;
						p.StartInfo.RedirectStandardOutput = capture_stdout;
						p.StartInfo.RedirectStandardError = capture_stdout;
						p.StartInfo.FileName = cmd;
						foreach (var arg in args)
							p.StartInfo.ArgumentList.Add (arg);
						p.StartInfo.CreateNoWindow = true;

						p.ErrorDataReceived += (object o, DataReceivedEventArgs ea) => {
							lock (std) {
								if (ea.Data == null) {
									stderr_event.Set ();
								} else {
									std.AppendLine (ea.Data);
								}
							}
						};

						p.OutputDataReceived += (object o, DataReceivedEventArgs ea) => {
							lock (std) {
								if (ea.Data == null) {
									stdout_event.Set ();
								} else {
									std.AppendLine (ea.Data);
								}
							}
						};

						if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
							switch (p.StartInfo.FileName) {
							case "git":
								p.StartInfo.FileName = @"C:\Program Files (x86)\Git\bin\git.exe";
								break;
							}
						}

						p.Start ();

						if (capture_stdout) {
							p.BeginErrorReadLine ();
							p.BeginOutputReadLine ();
						}

						if (wait_for_exit) {
							p.WaitForExit ();
							if (capture_stdout) {
								stderr_event.WaitOne ();
								stdout_event.WaitOne ();
							}
						}

						if (capture_stdout && !use_shell_execute) {
							if (p.ExitCode != 0)
								throw new Exception ("Program execution failed (" + p.ExitCode + "): " + Environment.NewLine + std.ToString ());
							return std.ToString ();
						} else {
							return "";
						}
					}
				}
			}
		}

		public string GetEol (string filename)
		{
			int Ac = 0, ADc = 0, DAc = 0, Dc = 0;
			bool dummy = false;
			IsEolMessedUp (filename, out Ac, out ADc, out DAc, out Dc, ref dummy);

			if (Ac == 0 && ADc == 0 && DAc == 0 && Dc == 0)
				return Environment.NewLine;

			if (Ac >= ADc && Ac >= DAc && Ac >= Dc) {
				return ((char) 0xA).ToString ();
			} else if (ADc >= Ac && ADc >= DAc && ADc >= Dc) {
				return ((char) 0xA).ToString () + ((char) 0xD).ToString ();
			} else if (DAc >= Ac && DAc >= ADc && DAc >= Dc) {
				return ((char) 0xD).ToString () + ((char) 0xA).ToString ();
			} else if (Dc >= Ac && Dc >= ADc && Dc >= DAc) {
				return ((char) 0xD).ToString ();
			} else {
				return Environment.NewLine;
			}
		}

		public bool IsEolMessedUp (string filename, out int Ac, out int ADc, out int DAc, out int Dc, ref bool is_binary)
		{
			return IsEolMessedUp (filename, out Ac, out ADc, out DAc, out Dc, out var _, ref is_binary);
		}

		public bool IsEolMessedUp (string filename, out int Ac, out int ADc, out int DAc, out int Dc, out bool hasConflictMarkers, ref bool is_binary)
		{
			ProcessTextFile (filename, out Ac, out ADc, out DAc, out Dc, out hasConflictMarkers, ref is_binary);
			if (is_binary)
				return false;

			var A = Ac > 0 ? 1 : 0;
			var AD = ADc > 0 ? 1 : 0;
			var DA = DAc > 0 ? 1 : 0;
			var D = Dc > 0 ? 1 : 0;
			return A + AD + DA + D > 1;
		}
		
		public void ProcessTextFile (string filename, out int Ac, out int ADc, out int DAc, out int Dc, out bool hasConflictMarkers, ref bool is_binary)
		{
			int i = 0;

			byte [] contents = File.ReadAllBytes (filename);

			Ac = 0;
			ADc = 0;
			DAc = 0;
			Dc = 0;
			hasConflictMarkers = false;

			while (i < contents.Length) {
				byte a = contents [i];
				bool two = i < contents.Length - 1;

				switch (a) {
				case 10:
					if (two && contents [i + 1] == 13) {
						ADc++;
						i++;
					} else {
						Ac++;
					}
					break;
				case 13:
					if (two && contents [i + 1] == 10) {
						DAc++;
						i++;
					} else {
						Dc++;
					}
					break;
				case (byte) '<': 
				case (byte) '=': 
				case (byte) '>': 
					// Conflict markers:
					// <<<<<<<
					// =======
					// >>>>>>>

					// No need to do anything we've already found a conflict marker
					if (hasConflictMarkers)
						break;

					// Check if we have enough text left for a conflict marker
					if (contents.Length - i < 6)
						break;

					// Check if we have a newline (or start of file) before the conflict marker
					if (i > 0 && (contents [i - 1] != 10 && contents [i - 1] != 13))
						break;

					// '=======' is valid markdown (header marker), so we don't treat it as a conflict marker
					if (filename.EndsWith (".md", StringComparison.OrdinalIgnoreCase) && a == (byte) '=')
						break;

					// Check for the conflict marker
					var foundNonmatching = false;
					for (var j = 1; j < 7; j++) {
						var c = contents [i + j];
						if (c != a) {
							foundNonmatching = true;
							break;
						}
					}
					if (!foundNonmatching)
						hasConflictMarkers = true;

					break;
				case 0:
					is_binary = true;
					return;
				}

				i++;
			}
		}

		public void Fixeol (Entry entry)
		{
			int Ac = 0, ADc = 0, DAc = 0, Dc = 0;
			bool dummy = false;
			if (!IsEolMessedUp (entry.filename, out Ac, out ADc, out DAc, out Dc, ref dummy))
				return;

			if (Dc == 0 && ADc == 0) {
				if (DAc < Ac) { // more unix than dos eols
					Dos2Unix (entry.filename);
				} else {
					Unix2Dos (entry.filename);
				}
			} else {
				Console.WriteLine ("Don't know how to convert {0}: A: {1}, AD: {2}, DA: {3}, D: {4}", entry.filename, Ac, ADc, DAc, Dc);
			}
		}

		void Convert (string filename, string eol)
		{
			Encoding.RegisterProvider (CodePagesEncodingProvider.Instance);

			string? line;
			bool last_line_empty = false;

			using (var buffer = new MemoryStream ()) {
				using (var reader = new StreamReader (filename, System.Text.Encoding.GetEncoding (1252), true)) {
					using (var writer = new StreamWriter (buffer, reader.CurrentEncoding)) {
						while ((line = reader.ReadLine ()) != null) {
							writer.Write (line);
							writer.Write (eol);
							last_line_empty = line.Length == 0;
						}
						if (!last_line_empty)
							writer.Write (eol);
					}
				}
				File.WriteAllBytes (filename, buffer.ToArray ());
			}
		}

		public void Dos2Unix (string filename)
		{
			Convert (filename, "\n");
		}

		public void Unix2Dos (string filename)
		{
			Convert (filename, "\r\n");
		}

		[STAThread ()]
		static int Main (string [] args)
		{
			try {
				if (args.Length > 0 && args [0] [0] != '-') {
					Environment.CurrentDirectory = args [0];
				}

				// find .git directory
				string original_cd = Environment.CurrentDirectory;
				while (!Directory.Exists (Path.Combine (Environment.CurrentDirectory, ".git")) && !File.Exists (Path.Combine (Environment.CurrentDirectory, ".git"))) {
					PREFIX = Path.GetFileName (Environment.CurrentDirectory) + Path.DirectorySeparatorChar + PREFIX;
					string dir = Path.GetDirectoryName (Environment.CurrentDirectory)!;
					if (!Directory.Exists (dir)) {
						Console.WriteLine ("Could not find any .git directory starting at {0}", original_cd);
						return 1;
					}
					Environment.CurrentDirectory = dir;
				}
				Console.WriteLine (".git directory: {0}, prefix:  {1}", Environment.CurrentDirectory, PREFIX);

				bool winforms = false;

				if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
					winforms = true;
				} else if (args.Length == 1 && args [0] == "-winforms") {
					winforms = true;
				}

				if (winforms) {
					throw new NotImplementedException ();
				} else {
					Instance = new TerminalDiff ();
				}
				Instance.Run ();
			} catch (Exception ex) {
				Console.WriteLine ("Exception in gui-diff");
				Console.WriteLine (ex);
			}
			return 0;
		}

		protected abstract void Run ();
	}

	public class Entry
	{
		public required string filename;
		public bool is_binary;
		public string? eol;
		public bool messed_up_eol;
		public bool has_conflict_marker;
		public bool staged;
		public bool staged_whole;
		public bool deleted;
		public bool edited_changelog;
		public bool added;
		public bool untracked;
		public bool is_directory;
		public bool conflict;
		public bool renamed;
		public string? renamed_from;

		public bool staged_partially {
			get {
				return staged && !staged_whole;
			}
		}
	}

	class DiffException : Exception
	{
		public DiffException (string msg)
			: base (msg)
		{
		}
	}
}
