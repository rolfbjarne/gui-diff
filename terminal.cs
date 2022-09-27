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
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace gui_diff
{
	class TerminalDiff : Diff
	{
		protected override void Run ()
		{
			Do ();
		}

		void ShowDiff (bool? staged)
		{
			ShowDiff (staged, false);
		}

		void SelectNextFile ()
		{
			int idx = entries.IndexOf (selected);
			if (idx + 1 == entries.Count) {
				idx = 0;
			} else {
				idx++;
			}
			selected = entries [idx];
		}

		void ShowDiff (bool? staged, bool monoport)
		{
			string diff = null;
			string color;
			if (!monoport) {
				Console.Clear ();
				color = "--color";
			} else {
				color = "--no-color";
			}
			if (selected == null) {
				if (staged.HasValue && staged.Value) {
					if (!monoport) {
						Console.ForegroundColor = ConsoleColor.Magenta;
						Console.WriteLine ("STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF");
						Console.ResetColor ();
					}
					diff = Execute ("git", "diff --staged " + color, monoport);
					if (!monoport) {
						Console.ForegroundColor = ConsoleColor.Magenta;
						Console.WriteLine ("STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF");
						Console.ResetColor ();
					}
				} else {
					diff = Execute ("git", "diff " + color, monoport);
				}
			} else {
				if (selected.untracked) {
					diff = File.ReadAllText (selected.filename);
					if (!monoport) {
						Console.WriteLine (diff);
					}
				} else if (((selected.staged_whole || selected.staged) && !(staged.HasValue && !staged.Value)) || (staged.HasValue && staged.Value)) {
					if (!monoport) {
						Console.ForegroundColor = ConsoleColor.Magenta;
						Console.WriteLine ("STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF");
						Console.ResetColor ();
					}
					diff = Execute ("git", "diff --staged " + color + " -- " + selected.QuotedFileName, monoport);
					if (!monoport) {
						Console.ForegroundColor = ConsoleColor.Magenta;
						Console.WriteLine ("STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF");
						Console.ResetColor ();
					}
				} else {
					diff = Execute ("git", "diff " + color + " -- " + selected.QuotedFileName, monoport);
				}
			}
			if (monoport) {
				if (string.IsNullOrEmpty (diff)) {
					Console.WriteLine ("Diff is empty");
				} else {
					string tmpfile = Path.GetTempFileName ();
					File.WriteAllText (tmpfile, diff);
					Execute ("monoport", tmpfile);
					File.Delete (tmpfile);
				}
			}
		}

		static string FindChangeLog (Entry entry)
		{
			string find;
			string dir = Path.GetDirectoryName (entry.filename);
			do {
				find = Path.Combine (dir, "ChangeLog");
				if (File.Exists (find)) {
					Console.WriteLine ("Found ChangeLog in: {0}", dir);
					return find;
				}
				Console.WriteLine ("No ChangeLog in {0} (filename: {1})", dir, entry.filename);
				if (dir == "")
					break;
				dir = Path.GetDirectoryName (dir);
			} while (dir != "/");

			return null;
		}

		public void EditChangeLog (Entry selected)
		{
			if (selected == null)
				throw new ArgumentNullException ("You must select a file first");
			string changelog = FindChangeLog (selected);
			if (changelog == null)
				throw new Exception ("No changelog found");
			string sdiff = Execute ("git", "diff --staged " + changelog);
			string fn = selected.filename.Substring (Path.GetDirectoryName (changelog).Length == 0 ? 0 : Path.GetDirectoryName (changelog).Length + 1);
			string content = File.ReadAllText (changelog);

			if (string.IsNullOrEmpty (sdiff) || string.IsNullOrEmpty (content)) {
				string entry = string.Format (
	@"{0:yyyy-MM-dd}  Rolf Bjarne Kvinge  <RKvinge@novell.com>

	* {1}:

", DateTime.Now, fn);

				File.WriteAllText (changelog, entry + content);
			} else {
				string eol = GetEol (changelog);
				string l = "\t* " + fn + ":" + eol;
				int idx = 0;
				idx = content.IndexOf (eol) + eol.Length;
				idx = content.IndexOf (eol, idx) + eol.Length;
				content = content.Substring (0, idx) + l + content.Substring (idx);
				File.WriteAllText (changelog, content);
			}
			Console.WriteLine ("Opening ChangeLog for editing...");
			//Execute ("gnome-terminal", "--maximize -e \"nano -c " + changelog + "\"", false);
			Execute ("meld", Path.Combine (Environment.CurrentDirectory, changelog), false);
			Execute ("git", "add " + changelog);
			Console.WriteLine ("ChangeLog added");
			selected.edited_changelog = true;
		}

		public void FixDate (string filename)
		{
			string diff = Execute ("git", "diff --staged --no-color -- " + filename, true);
			if (string.IsNullOrEmpty (diff)) {
				Console.WriteLine ("Nothing diff staged for the file: {0}", filename);
				return;
			}

			string result = System.Text.RegularExpressions.Regex.Replace (diff, @"\+[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]  Rolf Bjarne Kvinge", string.Format ("+{0:yyyy-MM-dd}  Rolf Bjarne Kvinge", DateTime.Now));

			if (result == diff) {
				Console.WriteLine ("Fixing date didn't result in a change (possibly because all dates are correct?) for the file: {0}", filename);
				return;
			}

			//string backup = filename + string.Format (".{0:yyyyMMddHHmmss}.backup", DateTime.Now);
			//File.Copy (filename, backup);
			Execute ("git", "reset HEAD -- " + filename);
			Execute ("git", "co HEAD -- " + filename);
			string tmp = Path.GetTempFileName ();
			File.WriteAllText (tmp, result);
			Execute ("git", "apply " + tmp);
			File.Delete (tmp);
			Execute ("git", "add " + filename);

			Console.WriteLine ("Fixed date in file: {0}", filename);
		}

		Commands cmds;

		void Add (IEnumerable<Entry> values)
		{
			var not_staged_deleted = values.Where ((v) => !(v.deleted && v.staged));

			foreach (var entry_batch in not_staged_deleted.Batch (20)) {
				Execute ("git", "add -- " + string.Join (" ", entry_batch.Select ((e) => e.QuotedFileName)));
				Console.WriteLine ("Added " + string.Join (", ", entry_batch.Select ((e) => e.filename)));
			}
		}

		Entry GetSelectedFile ()
		{
			if (selected != null)
				return selected;

			if (entries.Count == 1)
				return entries [0];

			throw new DiffException ("You need to select a file first.");
		}

		bool Do ()
		{
			string last_cmd = null;
			string cmd;
			int id;

			PrintList ();

			cmds = new Commands ()
			{
				{ "h|help|?", "Show this help message", delegate (string v)
					{
						cmds.Help ();
					}
				},
				{ "e|edit", "Open file in editor", delegate (string v)
					{
						var selected = GetSelectedFile ();
						Execute ("gedit", selected.QuotedFileName);
					}
				},
				{ "gedit", "Open the file in gedit", delegate (string v)
					{
						var selected = GetSelectedFile ();
						Execute ("gedit", selected.QuotedFileName, false, false, false);
					}
				},
				{ "geditall", "Open the files in gedit", delegate (string v)
					{
						Execute ("gedit", string.Join (" ", entries.Where ((w) => !w.untracked).Select ((w) => w.QuotedFileName).ToArray ()), false, false, false);
					}
				},
				{ "nano", "Open file in nano", delegate (string v)
					{
						var selected = GetSelectedFile ();
						Execute ("nano", "-c " + selected.QuotedFileName, false);
					}
				},
				{ "c|changelog", "Edit ChangeLog for the selected file", delegate (string v)
					{
						var selected = GetSelectedFile ();
						EditChangeLog (selected);
						list_dirty = true;
						PrintList ();
					}
				},
				{ "a|add", "Add file to index", delegate (string v)
					{
						var selected = GetSelectedFile ();
						list_dirty = true;
						if (!(selected.deleted && selected.staged))
							Execute ("git", (selected.deleted ? "rm -- " : "add ") + selected.QuotedFileName);
						PrintList ();
					}
				},
				{ "add+next|an", "Add file to index and go to next file", delegate (string v)
					{
						var selected = GetSelectedFile ();
						list_dirty = true;
						Execute ("git", (selected.deleted ? "rm -- " : "add ") + selected.QuotedFileName);
						if (selected == null)
							throw new DiffException ("You need to select a file first.");
						SelectNextFile ();
						ShowDiff (null);
					}
				},
				{ "addall|add -u", "Add all changed files to the index", delegate (string v)
					{
						list_dirty = true;
						Add (entries.Where ((e) => !e.untracked));
						PrintList ();
					}
				},
				{ "addalluntracked", "Add all untracked files to the index", delegate (string v)
					{
						list_dirty = true;
						Add (entries.Where ((e) => e.untracked));
						PrintList ();
					}
				},

				{ "addall+untracked|addall+u", "Add all changed + all untracked files to the index", delegate (string v)
					{
						list_dirty = true;
						Add (entries);
						PrintList ();
					}
				},

				{ "ac|addc", "Add file to index and edit changelog", delegate (string v)
					{
						var selected = GetSelectedFile ();
						list_dirty = true;
						Execute ("git", (selected.deleted ? "rm -- " : "add -- ") + selected.QuotedFileName);
						EditChangeLog (selected);
						PrintList ();
					}
				},
				{ "and|addanddiff", "Add file to index and immediately show a diff of the staged file", delegate (string v)
					{
						var selected = GetSelectedFile ();
						list_dirty = true;
						Execute ("git", (selected.deleted ? "rm -- " : "add -- ") + selected.QuotedFileName);
						ShowDiff (true);
					}
				},
				{ "p|add -p", "Add file to index in interactive mode", delegate (string v)
					{
						var selected = GetSelectedFile ();
						list_dirty = true;
						Execute ("git", "add -p " + selected.QuotedFileName, false);
					}
				},
				{ "ci|gitci|git ci", "Commit using gitci", delegate (string v)
					{
						Execute ("gitci", "", false);
						list_dirty = true;
						PrintList ();
					}
				},
				{ "commit", "Commit using git (git commit)", delegate (string v)
					{
						list_dirty = true;
						Execute ("git", "commit", false);
						PrintList ();
					}
				},
				{ "fixdate", "Fix the date(s) in the selected ChangeLog", delegate (string v)
					{
						var selected = GetSelectedFile ();
						FixDate (selected.filename);
						ShowDiff (null);
					}
				},
				{ "fixdates", "Fix the date(s) in all the ChangeLogs", delegate (string v)
					{
						foreach (var entry in entries) {
							if (entry.filename.EndsWith ("ChangeLog", StringComparison.Ordinal))
								FixDate (entry.filename);
						}
					}
				},
				{ "d|diff", "Show the diff for the selected file", delegate (string v)
					{
						ShowDiff (false);
					}
				},
				{ "md|monoport diff", "Monoport the diff of the selected file", delegate (string v)
					{
						ShowDiff (false, true);
					}
				},
				{ "msd|monoport sdiff", "Monoport the staged diff of the selected file", delegate (string v)
					{
						ShowDiff (true, true);
					}
				},
				{ "sd|diff --staged|stageddiff|staged diff", "Show the staged diff", delegate (string v)
					{
						ShowDiff (true);
					}
				},
				{ "rm|delete", "Delete the selected files", delegate (string v)
					{
						var selected = GetSelectedFile ();
						list_dirty = true;
						if (selected.untracked) {
							Execute ("rm", "-- " + selected.QuotedFileName);
						} else {
							Execute ("git", "rm -f -- " + selected.QuotedFileName);
						}
						PrintList ();
					}
				},
				{ "n|next", "Select the next file", delegate (string v)
					{
						var selected = GetSelectedFile ();
						SelectNextFile ();
						ShowDiff (null);
					}
				},
				{ "amend", "Executes git commit --amend", delegate (string v)
					{
						list_dirty = true;
						Execute ("git", "commit --amend", false);
						PrintList ();
					}
				},
				{ "reset", "Executes git reset on the selected file", delegate (string v)
					{
						list_dirty = true;
						if (selected == null) {
							Execute ("git", "reset");
							PrintList ();
						} else {
							Execute ("git", "reset -- " + selected.QuotedFileName);
							var selected_file = selected.filename;
							RefreshList ();
							selected = entries.Single (v => v.filename == selected_file);
							ShowDiff (null);
						}
					}
				},
				{ "checkout", "Checks out the selected file (equivalent to svn revert)", delegate (string v)
					{
						var selected = GetSelectedFile ();
						list_dirty = true;
						Execute ("git", "checkout " + selected.QuotedFileName);
						PrintList ();
					}
				},
				{ "checkout+next|chn", "Checks out the selected file and advances to the next file", delegate (string v)
					{
						var selected = GetSelectedFile ();
						list_dirty = true;
						Execute ("git", "checkout " + selected.QuotedFileName);
						SelectNextFile ();
						ShowDiff (null);
					}
				},
				{ "meld", "View the selected file in meld", delegate (string v)
					{
						var selected = GetSelectedFile ();
						list_dirty = true;
						Execute ("muld", Path.Combine (Environment.CurrentDirectory, selected.QuotedFileName), false);
					}
				},
				{ "gui", "Run git gui", delegate (string v)
					{
						Execute ("git", "gui", false, false);
					}
				},
				{ "r|refresh", "Refresh the list", delegate (string v)
					{
						list_dirty = true;
						PrintList ();
					}
				},
				{ "l|list", "Show the list again", delegate (string v)
					{
						PrintList ();
					}
				},
				{ "q|quit|QUIT", "Quit", delegate (string v)
					{
						Environment.Exit (0);
					}
				},
				{ "fixeol", "Fix eol-style for the all the files which have eol-style problems", delegate (string v)
					{
						for (int i = 0; i < entries.Count; i++) {
							if (!entries [i].messed_up_eol)
								continue;
							Fixeol (entries [i]);
						}
						list_dirty = true;
						PrintList ();
					}
				},
				{ "dos2unix|dosunix", "Change the eol-style to unix for the selected file", delegate (string v)
					{
						var selected = GetSelectedFile ();
						Dos2Unix (selected.filename);
						if (selected.staged_whole)
							Execute ("git", "add " + selected.QuotedFileName);
						list_dirty = true;
						ShowDiff (null);
					}
				},
				{ "unix2dos|unixdos", "Change the eol-style to dos for the selected file", delegate (string v)
					{
						var selected = GetSelectedFile ();
						Unix2Dos (selected.filename);
						ShowDiff (null);
					}
				},
				{ "i|ignore", "Add file to .gitignore in current directory", delegate (string v)
					{
						var selected = GetSelectedFile ();
						File.AppendAllText (Path.Combine (Path.GetDirectoryName (selected.filename), ".gitignore"), Path.GetFileName (selected.filename) + '\n');
						list_dirty = true;
					}
				},
				{ "ignore-extension", "Add extension of file to .gitignore in current directory", delegate (string v)
					{
						var selected = GetSelectedFile ();
						File.AppendAllText (Path.Combine (Path.GetDirectoryName (selected.filename), ".gitignore"), "*" + Path.GetExtension (selected.filename) + '\n');
						list_dirty = true;
					}
				},
				{ "log", "Run 'git log' on the selected file", (v) =>
					{
						var selected = GetSelectedFile ();
						Execute ("git", "log -- " + selected.QuotedFileName, false, true);
					}
				},
				{ "log --oneline", "Run 'git log --oneline' on the selected file", (v) =>
					{
						var selected = GetSelectedFile ();
						Execute ("git", "log --oneline -- " + selected.QuotedFileName, false, true);
					}
				},
				{ "blame", "Run 'git blame' on the selected file", (v) =>
					{
						var selected = GetSelectedFile ();
						Execute ("git", "blame -- " + selected.QuotedFileName, false, true);
					}
				},
				{ "commit *", "Commit using the specified commit message", (v) =>
					{
						var msg = v.Substring ("commit ".Length).Trim ();
						if (msg.Length == 0)
							throw new DiffException ("Commit message is empty");

						if (selected != null)
							Execute ("git", "add -- " + selected.QuotedFileName, false, true);
						Execute ("git", $"commit -m \"{msg}\"", false, true);
						list_dirty = true;
						PrintList ();
					}
				},
				{ "z", "Amend HEAD with the current staged changes", (v) =>
					{
						if (selected != null)
							Execute ("git", "add -- " + selected.QuotedFileName, false, true);
						Execute ("git", $"commit --amend -C HEAD", false, true);
						list_dirty = true;
						PrintList ();
					}
				},
			};

			do {
				PrintCommands ();

				cmd = Console.ReadLine ();
				if (string.IsNullOrEmpty (cmd))
					cmd = last_cmd;
				last_cmd = cmd;

				if (int.TryParse (cmd, out id) && id >= 1 && id <= entries.Count) {
					selected = entries [id - 1];
					if (selected != null && selected.untracked) {
						// don't show anything, the common case is to select a file to delete it, in which case showing the entire file can be annoying
						Console.WriteLine ("Selected untracked file {0}", selected.filename);
					} else {
						ShowDiff (null);
					}
					continue;
				}

				bool executed = cmds.Execute (cmd);

				if (executed)
					continue;

				Console.WriteLine ("Unrecognized command: '{0}'", cmd);
			} while (true);
		}

		void PrintCommands ()
		{
			if (selected != null)
				Console.WriteLine ("{0} {1}", entries.IndexOf (selected) + 1, selected.filename);
			Console.Write ("Now what? ");
		}

		void PrintList ()
		{
			Console.Clear ();
			if (list_dirty)
				RefreshList ();

			bool any_eol_issues = false;
			bool any_staged = false;
			bool any_staged_partially = false;
			bool any_deleted = false;
			bool any_untracked = false;
			bool any_binaries = false;
			int max_filename_length = 0;
			ConsoleColor color = ConsoleColor.Black;
			for (int i = 0; i < entries.Count; i++) {
				any_eol_issues |= entries [i].messed_up_eol;
				any_staged_partially |= (entries [i].staged && !entries [i].staged_whole);
				any_staged |= entries [i].staged_whole;
				any_deleted |= entries [i].deleted;
				any_untracked |= entries [i].untracked;
				any_binaries |= entries [i].is_binary;
				max_filename_length = Math.Max (max_filename_length, entries [i].filename.Length);
			}

			//		Console.Clear ();
			for (int i = 0; i < entries.Count; i++) {
				color = ConsoleColor.Black;
				if (selected != null && selected == entries [i]) {
					Console.Write ("*");
				} else {
					Console.Write (" ");
				}
				if (any_staged || any_staged_partially) {
					if (entries [i].staged_whole) {
						Console.Write ("staged ");
						if (any_staged_partially)
							Console.Write ("            ");
						color = ConsoleColor.Blue;
					} else if (entries [i].staged) {
						Console.Write ("staged (partially) ");
						color = ConsoleColor.DarkBlue;
					} else {
						if (any_staged_partially) {
							Console.Write ("   -               ");
						} else {
							Console.Write ("   -   ");
						}
					}
				}
				if (any_binaries) {
					if (entries [i].is_binary) {
						Console.Write ("binary ");
						color = ConsoleColor.DarkGreen;
					} else {
						Console.Write ("       ");
					}
				}
				if (any_deleted) {
					if (entries [i].deleted) {
						Console.Write ("deleted ");
						color = ConsoleColor.Red;
					} else {
						Console.Write ("   -    ");
					}
				}
				if (any_untracked) {
					if (entries [i].untracked) {
						Console.Write ("untracked ");
						color = ConsoleColor.Yellow;
					} else {
						Console.Write ("     -    ");
					}
				}
				Console.Write (" ");

				if (any_eol_issues) {
					if (entries [i].messed_up_eol) {
						Console.Write ("EOL ");
						color = ConsoleColor.Magenta;
					} else {
						Console.Write ("    ");
					}
				}

				if (entries.Count <= 9) {
					Console.Write ("{0,-1} ", i + 1);
				} else if (entries.Count <= 99) {
					Console.Write ("{0,-2} ", i + 1);
				} else {
					Console.Write ("{0,-3} ", i + 1);
				}
				Console.Write (" ");

				Console.Write (entries [i].eol);
				Console.Write (" ");

				if (color != ConsoleColor.Black)
					Console.ForegroundColor = color;
				Console.Write (entries [i].filename.Substring (PREFIX.Length));
				Console.ResetColor ();

				if (entries [i].is_directory)
					Console.WriteLine (" [DIRECTORY]");
				Console.WriteLine ();
			}
		}
	}
}
