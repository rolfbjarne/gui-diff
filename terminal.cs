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
using System.Net.NetworkInformation;

namespace gui_diff
{
	class TerminalDiff : Diff
	{
		protected override void Run ()
		{
			Do ();
		}

		void SelectNextFile ()
		{
			int idx;
			if (selected is null) {
				idx = 0;
			} else {
				idx = entries.IndexOf (selected);
				if (idx + 1 == entries.Count) {
					idx = 0;
				} else {
					idx++;
				}
			}
			selected = entries [idx];
		}

		// returns true if any file was selected
		bool SelectNextFileWithMergeConflict ()
		{
			var idx = selected is null ? -1 : entries.IndexOf (selected);
			var nextMergeConflict = entries.FindIndex ((idx == -1 || idx >= entries.Count - 1) ? 0 : idx + 1, (v) => v.conflict);
			if (nextMergeConflict == -1 && idx > -1)
				nextMergeConflict = entries.FindIndex ((v) => v.conflict);
			if (nextMergeConflict == -1)
				return false;
			selected = entries[nextMergeConflict];
			return true;
		}

		void ShowDiff (bool? staged)
		{
			string diff = string.Empty;
			string color;
				Console.Clear ();
				color = "--color";
			if (selected == null) {
				if (staged.HasValue && staged.Value) {
					Console.ForegroundColor = ConsoleColor.Magenta;
					Console.WriteLine ("STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF");
					Console.ResetColor ();
				   diff = Execute ("git", new[] { "diff", "--staged", color }, false);
						Console.ForegroundColor = ConsoleColor.Magenta;
						Console.WriteLine ("STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF");
						Console.ResetColor ();
				} else {
					diff = Execute ("git", ["diff", color], false);
				}
			} else {
				if (selected.untracked) {
					diff = File.ReadAllText (selected.filename);
					Console.WriteLine (diff);
				} else if (((selected.staged_whole || selected.staged) && !(staged.HasValue && !staged.Value)) || (staged.HasValue && staged.Value)) {
					Console.ForegroundColor = ConsoleColor.Magenta;
					Console.WriteLine ("STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF");
					Console.ResetColor ();
					diff = Execute ("git", ["diff", "--staged", color, "--", selected.filename], false);
					Console.ForegroundColor = ConsoleColor.Magenta;
					Console.WriteLine ("STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF STAGED DIFF");
					Console.ResetColor ();
				} else {
					diff = Execute ("git", ["diff", color, "--", selected.filename], false);
				}
			}
		}

		public void FixDate (string filename)
		{
			string diff = Execute ("git", ["diff", "--staged", "--no-color", "--", filename], true);
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
			Execute ("git", ["reset", "HEAD", "--", filename]);
			Execute ("git", ["co", "HEAD", "--", filename]);
			string tmp = Path.GetTempFileName ();
			File.WriteAllText (tmp, result);
			Execute ("git", ["apply", tmp]);
			File.Delete (tmp);
			Execute ("git", ["add", filename]);

			Console.WriteLine ("Fixed date in file: {0}", filename);
		}

		Commands? cmds;

		void Add (IEnumerable<Entry> values)
		{
			var not_staged_deleted = values.Where ((v) => !(v.deleted && v.staged));

			foreach (var entry_batch in not_staged_deleted.Batch (20)) {
				Execute ("git", [ "add", "-f", "--", ..entry_batch.Select ((e) => e.filename)]);
				Console.WriteLine ("Added " + string.Join (", ", entry_batch.Select ((e) => e.filename)));
			}
		}

		Entry? GetNullableSelectedFile ()
		{
			return GetSelectedFileOrNot (true);
		}

		Entry GetSelectedFile ()
		{
			return GetSelectedFileOrNot (false)!;
		}

		Entry? GetSelectedFileOrNot (bool allowUnselected  = false)
		{
			if (selected != null)
				return selected;

			if (entries.Count == 1)
				return entries [0];

			if (!allowUnselected)
				throw new DiffException ("You need to select a file first.");

			return null;
		}

		void EditSelectedFile ()
		{
			var selected = GetSelectedFile();
			Execute ("gedit", [selected.filename]);
		}

		bool Do ()
		{
			string? last_cmd = null;
			string? cmd;
			int id;

			PrintList ();

			cmds = new Commands ()
			{
				{ "h|help|?", "Show this help message", delegate (string v)
					{
						cmds!.Help ();
					}
				},
				{ "e|edit", "Open file in editor", delegate (string v)
					{
						EditSelectedFile ();
					}
				},
				{ "gedit", "Open the file in gedit", delegate (string v)
					{
						var selected = GetSelectedFile ();
						Execute ("gedit", [selected.filename], false, false, false);
					}
				},
				{ "geditall", "Open the files in gedit", delegate (string v)
					{
						Execute ("gedit", entries.Where ((w) => !w.untracked).Select ((w) => w.filename), false, false, false);
					}
				},
				{ "nano", "Open file in nano", delegate (string v)
					{
						var selected = GetSelectedFile ();
						Execute ("nano", ["-c", selected.filename], false);
					}
				},
				{ "a|add", "Add file to index", delegate (string v)
					{
						var selected = GetSelectedFile ();
						list_dirty = true;
						if (!(selected.deleted && selected.staged))
							Execute ("git", selected.deleted ? ["rm", "--", selected.filename] : ["add", "-f", "--", selected.filename]);
						PrintList ();
					}
				},
				{ "add+next|an", "Add file to index and go to next file", delegate (string v)
					{
						var selected = GetSelectedFile ();
						list_dirty = true;
						if (!(selected.deleted && selected.staged))
							Execute ("git", selected.deleted ? ["rm", "--", selected.filename] : ["add", "-f", "--", selected.filename]);
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

				{ "and|addanddiff", "Add file to index and immediately show a diff of the staged file", delegate (string v)
					{
						var selected = GetSelectedFile ();
						list_dirty = true;
						Execute ("git", selected.deleted ? ["rm", "--", selected.filename] : ["add", "-f", "--", selected.filename]);
						ShowDiff (true);
					}
				},
				{ "p|add -p", "Add file to index in interactive mode", delegate (string v)
					{
						var selected = GetSelectedFile ();
						list_dirty = true;
						Execute ("git", ["add", "-p", selected.filename], false);
					}
				},
				{ "commit", "Commit using git (git commit)", delegate (string v)
					{
						list_dirty = true;
						Execute ("git", ["commit"], false);
						PrintList ();
					}
				},
				{ "d|diff", "Show the diff for the selected file", delegate (string v)
					{
						ShowDiff (false);
					}
				},
				{ "sd|diff --staged|stageddiff|staged diff|sdiff", "Show the staged diff", delegate (string v)
					{
						ShowDiff (true);
					}
				},
				{ "rm|delete", "Delete the selected files", delegate (string v)
					{
						var selected = GetSelectedFile ();
						list_dirty = true;
						if (selected.untracked) {
							Execute ("rm", ["--", selected.filename]);
						} else {
							Execute ("git", ["rm", "-f", "--", selected.filename]);
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
						Execute ("git", ["commit", "--amend"], false);
						PrintList ();
					}
				},
				{ "reset", "Executes git reset on the selected file", delegate (string v)
					{
						list_dirty = true;
						if (selected == null) {
							Execute ("git", ["reset"]);
							PrintList ();
						} else {
							Execute ("git", ["reset", "--", selected.filename]);
							var selected_file = selected.filename;
							RefreshList ();
							selected = entries.Single (v => v.filename == selected_file);
							ShowDiff (null);
						}
					}
				},
				{ "reset-conflicts", "Executes git reset on files with conflicts", delegate (string v)
					{
						var filesWithMergeConflicts = entries.Where (v => v.conflict);
						if (filesWithMergeConflicts.Any ()) {
							list_dirty = true;
							Execute ("git", ["reset", "--", ..filesWithMergeConflicts.Select (v => v.filename)]);
							RefreshList ();
							PrintList ();
						} else {
							Console.WriteLine ("No files with merge conflicts found.");
						}
                    }
				},
				{ "checkout", "Checks out the selected file (equivalent to svn revert)", delegate (string v)
					{
						var selected = GetSelectedFile ();
						list_dirty = true;
						Execute ("git", ["checkout", selected.filename]);
						PrintList ();
					}
				},
				{ "checkout+next|chn", "Checks out the selected file and advances to the next file", delegate (string v)
					{
						var selected = GetSelectedFile ();
						list_dirty = true;
						Execute ("git", ["checkout", selected.filename]);
						SelectNextFile ();
						ShowDiff (null);
					}
				},
				{ "gui", "Run git gui", delegate (string v)
					{
						Execute ("git", ["gui"], false, false);
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
							Execute ("git", ["add", "-f", selected.filename]);
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
						File.AppendAllText (Path.Combine (Path.GetDirectoryName (selected.filename)!, ".gitignore"), Path.GetFileName (selected.filename) + '\n');
						list_dirty = true;
					}
				},
				{ "ignore-extension", "Add extension of file to .gitignore in current directory", delegate (string v)
					{
						var selected = GetSelectedFile ();
						File.AppendAllText (Path.Combine (Path.GetDirectoryName (selected.filename)!, ".gitignore"), "*" + Path.GetExtension (selected.filename) + '\n');
						list_dirty = true;
					}
				},
				{ "log", "Run 'git log' on the selected file", (v) =>
					{
						var selected = GetSelectedFile ();
						Execute ("git", ["log", "--", selected.filename], false, true);
					}
				},
				{ "log --oneline", "Run 'git log --oneline' on the selected file", (v) =>
					{
						var selected = GetSelectedFile ();
						Execute ("git", ["log", "--oneline", "--", selected.filename], false, true);
					}
				},
				{ "blame", "Run 'git blame' on the selected file", (v) =>
					{
						var selected = GetSelectedFile ();
						Execute ("git", ["blame", "--", selected.filename], false, true);
					}
				},
				{ "commit *", "Commit using the specified commit message", (v) =>
					{
						var msg = v.Substring ("commit ".Length).Trim ();
						if (msg.Length == 0)
							throw new DiffException ("Commit message is empty");

						if (selected != null)
							Execute ("git", ["add", "-f", "--", selected.filename], false, true);
						Execute ("git", ["commit", "-m", msg], false, true);
						list_dirty = true;
						PrintList ();
					}
				},
				{ "z", "Amend HEAD with the current staged changes", (v) =>
					{
						if (selected != null)
							Execute ("git", ["add", "-f", "--", selected.filename], false, true);
						Execute ("git", ["commit", "--amend", "-C", "HEAD"], false, true);
						list_dirty = true;
						PrintList ();
					}
				},
				{ "ame", "Add selected (if any), edit next file with a merge conflict", (v) =>
					{

						var selected = GetNullableSelectedFile ();
						if (selected is not null) {
							Execute ("git", selected.deleted ? ["rm", "--", selected.filename] : ["add", selected.filename]);
							RefreshList ();
						}
						list_dirty = true;
						if (SelectNextFileWithMergeConflict ()) {
							EditSelectedFile ();
						} else {
							PrintList ();
						}
					}
				},
				{ "..|cd ..", "cd ..", (v) =>
					{
						Console.WriteLine ($"Old prefix: {Diff.PREFIX}");
						Diff.PREFIX = Path.GetDirectoryName (Diff.PREFIX.TrimEnd (Path.DirectorySeparatorChar)) ?? string.Empty;
						if (Diff.PREFIX.Length > 0)
							Diff.PREFIX += Path.DirectorySeparatorChar;
						Console.WriteLine ($"New prefix: {Diff.PREFIX}");
						list_dirty = true;
						PrintList ();
					}
				},
				{ "cd *", "cd *", (v) =>
					{
						var dir = v.Substring ("cd ".Length).Trim ();
						if (string.IsNullOrEmpty (dir))
							throw new DiffException ("Directory is empty");
						if (!Directory.Exists (Path.Combine (Diff.PREFIX, dir)))
							throw new DiffException ($"Directory '{dir}' does not exist");
						Diff.PREFIX = Path.Combine (Diff.PREFIX, dir).TrimEnd (Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
						list_dirty = true;
						PrintList ();
					}
				},
				{ ".|cd", "Go to the root directory", (v) =>
					{
						Diff.PREFIX = string.Empty;
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

		string? topLevel;
		string GetTopLevel ()
		{
			if (topLevel is null) {
				topLevel = Execute ("git", ["rev-parse", "--show-toplevel"])?.Trim ();
				if (topLevel is null)
					throw new Exception ("Not a git repository");
			}
			return topLevel;
		}

		void PrintList ()
		{
			Console.Clear ();

			var relativeRoot = $"<root>/{Diff.PREFIX.TrimEnd ('/')}";
			Console.Write ($"Working directory: ");
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.WriteLine (relativeRoot);
			Console.ResetColor ();

			if (list_dirty)
				RefreshList ();

			bool any_eol_issues = false;
			bool any_conflict_markers = false;
			bool any_staged = false;
			bool any_staged_partially = false;
			bool any_conflicts = false;
			bool any_deleted = false;
			bool any_untracked = false;
			bool any_binaries = false;
			bool any_outside_file_references = false;
			int max_filename_length = 0;
			ConsoleColor color = ConsoleColor.Black;
			for (int i = 0; i < entries.Count; i++) {
				any_eol_issues |= entries [i].messed_up_eol;
				any_conflict_markers |= entries [i].has_conflict_marker;
				any_staged_partially |= entries [i].staged_partially;
				any_conflicts |= entries [i].conflict;
				any_staged |= entries [i].staged_whole;
				any_deleted |= entries [i].deleted;
				any_untracked |= entries [i].untracked;
				any_binaries |= entries [i].is_binary;
				any_outside_file_references |= entries [i].renamed_from?.StartsWith (PREFIX) != true;
				max_filename_length = Math.Max (max_filename_length, entries [i].filename.Length);
			}

			var ignorePrefix = any_outside_file_references ? string.Empty : PREFIX;

			//		Console.Clear ();
			for (int i = 0; i < entries.Count; i++) {
				color = ConsoleColor.Black;
				if (selected != null && selected == entries [i]) {
					Console.Write ("*");
				} else {
					Console.Write (" ");
				}
				if (any_staged || any_staged_partially || any_conflicts) {
					if (entries [i].staged_whole) {
						Console.Write ("staged ");
						if (any_staged_partially)
							Console.Write ("            ");
						else if (any_conflicts)
							Console.Write ("  ");
						color = ConsoleColor.Blue;
					} else if (entries [i].conflict) {
						Console.Write ("conflict ");
						if (any_staged_partially)
							Console.Write ("          ");
						color = ConsoleColor.Red;
					} else if (entries [i].staged) {
						Console.Write ("staged (partially) ");
						color = ConsoleColor.DarkBlue;
					} else {
						if (any_staged_partially) {
							Console.Write ("   -               ");
						} else if (any_conflicts) {
							Console.Write ("   -     ");
						} else {
							Console.Write ("   -   ");
						}
					}
				}

				if (any_conflict_markers) {
					if (entries [i].has_conflict_marker) {
						Console.ForegroundColor = ConsoleColor.DarkRed;
						Console.Write ("conflict markers ");
						Console.ResetColor ();
					} else {
						Console.Write ("                 ");
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
				if (entries [i].renamed) {
					Console.Write (entries [i].renamed_from! [ignorePrefix.Length..]);
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.Write (" -> ");
					Console.ForegroundColor = color;
					Console.Write (entries [i].filename [ignorePrefix.Length..]);
				} else {
					Console.Write (entries [i].filename [ignorePrefix.Length..]);
				}
				Console.ResetColor ();

				if (entries [i].is_directory)
					Console.WriteLine (" [DIRECTORY]");
				Console.WriteLine ();
			}
		}
	}
}
