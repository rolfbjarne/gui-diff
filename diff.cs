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
using System.Text;

namespace gui_diff
{
	public abstract class Diff
	{
		public static string PREFIX = string.Empty;
		public static Diff Instance;

		public List<Entry> entries = new List<Entry> ();
		public bool list_dirty = true;
		public Entry selected;
		static bool verbose;

		public Diff ()
		{
		}

		public string GetDiff (Entry selected, bool? staged)
		{
			string diff = null;
			string color = "--no-color";

			if (selected == null) {
				if (staged.HasValue && staged.Value) {
					diff = Execute ("git", "diff --staged " + color, true);
				} else {
					diff = Execute ("git", "diff " + color, true);
				}
			} else {
				if (selected.untracked) {
					diff = File.ReadAllText (selected.filename);
				} else if (((selected.staged_whole || selected.staged) && !(staged.HasValue && !staged.Value)) || (staged.HasValue && staged.Value)) {
					diff = Execute ("git", "diff --staged " + color + " -- " + selected.filename, true);
				} else {
					diff = Execute ("git", "diff " + color + " -- " + selected.filename, true);
				}
			}

			return diff;
		}

		public void RefreshList ()
		{
			int Ac = 0, ADc = 0, DAc = 0, Dc = 0;
			List<string> diff = ExecuteToLines ("git", "diff --name-only");
			List<string> staged = ExecuteToLines ("git", "diff --name-only --staged");
			List<string> untracked = ExecuteToLines ("git", "ls-files --other --exclude-standard");
			List<string> all = new List<string> (diff);

			selected = null;
			foreach (var s in staged) {
				if (!all.Contains (s))
					all.Add (s);
			}
			all.Sort ();

			entries.Clear ();
			foreach (var file in all) {
				if (PREFIX.Length > 1 && !file.StartsWith (PREFIX))
					continue;
				Entry entry = new Entry ();
				entry.filename = file;
				entry.staged = staged.Contains (file);
				entry.staged_whole = !diff.Contains (file);
				if (Directory.Exists (file)) {
					entry.is_directory = true;
				} else if (!File.Exists (file)) {
					entry.deleted = true;
				} else {
					entry.messed_up_eol = IsEolMessedUp (file, out Ac, out ADc, out DAc, out Dc, ref entry.is_binary);
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
				if (PREFIX.Length > 1 && !file.StartsWith (PREFIX))
					continue;
				Entry entry = new Entry ();
				entry.filename = file;
				entry.untracked = true;
				entries.Add (entry);
			}

			list_dirty = false;
		}

		public List<string> ExecuteToLines (string cmd, string args)
		{
			return new List<string> (Execute (cmd, args).Split (new char [] { (char) 10, (char) 13 }, StringSplitOptions.RemoveEmptyEntries));
		}
	
		public string Execute (string cmd, string args)
		{
			return Execute (cmd, args, true);
		}

		public string Execute (string cmd, string args, bool capture_stdout)
		{
			return Execute (cmd, args, capture_stdout, true);
		}

		public void Stage (string path)
		{
			Execute ("git", "apply --index --cached --ignore-whitespace --ignore-space-change \"" + path + "\"", true, true);
			list_dirty = true;
		}

		public string Execute (string cmd, string args, bool capture_stdout, bool wait_for_exit, bool use_shell_execute = false)
		{
			StringBuilder std = new StringBuilder ();
			
			using (System.Threading.ManualResetEvent stderr_event = new System.Threading.ManualResetEvent (false)) {
				using (System.Threading.ManualResetEvent stdout_event = new System.Threading.ManualResetEvent (false)) {

					using (Process p = new Process ()) {
						p.StartInfo.UseShellExecute = use_shell_execute;
						p.StartInfo.RedirectStandardOutput = capture_stdout;
						p.StartInfo.RedirectStandardError = capture_stdout;
						p.StartInfo.FileName = cmd;
						p.StartInfo.Arguments = args;
						p.StartInfo.CreateNoWindow = true;

						p.ErrorDataReceived += (object o, DataReceivedEventArgs ea) =>
							{
								lock (std) {
									if (ea.Data == null) {
										stderr_event.Set ();
									} else {
										std.AppendLine (ea.Data);
									}
								}
							};

						p.OutputDataReceived += (object o, DataReceivedEventArgs ea) =>
						{
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
							if (verbose) {
								Console.WriteLine ("export PWD={0}", string.IsNullOrEmpty (p.StartInfo.WorkingDirectory) ? Environment.CurrentDirectory : p.StartInfo.WorkingDirectory	);
								Console.WriteLine ("{0} {1}", cmd, args);
								Console.WriteLine (std.ToString ());
							}
							return std.ToString ();
						} else {
							return null;
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
			int A = 0, AD = 0, DA = 0, D = 0;
			int i = 0;

			byte [] contents = File.ReadAllBytes (filename);

			Ac = 0;
			ADc = 0;
			DAc = 0;
			Dc = 0;

			while (i < contents.Length) {
				byte a = contents [i];
				bool two = i < contents.Length - 1;

				switch (a) {
				case 10:
					if (two && contents [i + 1] == 13) {
						AD = 1;
						ADc++;
						i++;
					} else {
						A = 1;
						Ac++;
					}
					break;
				case 13:
					if (two && contents [i + 1] == 10) {
						DA = 1;
						DAc++;
						i++;
					} else {
						D = 1;
						Dc++;
					}
					break;
				case 0:
					is_binary = true;
					return false;
				}

				i++;
			}

			return A + AD + DA + D > 1;
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

		private void Convert (string filename, string eol)
		{
			string line;
			bool last_line_empty = false;

			using (MemoryStream buffer = new MemoryStream ()) {
				using (StreamReader reader = new StreamReader (filename, System.Text.Encoding.GetEncoding (1252), true)) {
					using (StreamWriter writer = new StreamWriter (buffer, reader.CurrentEncoding)) {
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
					string dir = Path.GetDirectoryName (Environment.CurrentDirectory);
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
					Instance = new WinFormsDiff ();
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
		public string filename;
		public bool is_binary;
		public string eol;
		public bool messed_up_eol;
		public bool staged;
		public bool staged_whole;
		public bool deleted;
		public bool edited_changelog;
		public bool added;
		public bool untracked;
		public bool is_directory;
		
		public string QuotedFileName {
			get { return "\"" + filename + "\""; }
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

