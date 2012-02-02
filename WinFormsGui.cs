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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace gui_diff {
	public partial class WinFormsGui : Form {
		WinFormsDiff diff;
		bool index_processing;

		public WinFormsGui (WinFormsDiff diff)
		{
			this.diff = diff;
			this.diff.gui = this;
			InitializeComponent ();
		}

		private void gui_Load (object sender, EventArgs e)
		{
			try {
				PrintList ();
				if (lstFiles.Items.Count > 0)
					lstFiles.Items [0].Selected = true;
				lstFiles.Focus ();

				cmbRepository.Text = Environment.CurrentDirectory;

				Timer tmr = new Timer ();
				tmr.Interval = 100;
				tmr.Tick += new EventHandler (tmr_Tick);
				tmr.Start ();
			} catch (Exception ex) {
				MessageBox.Show (ex.Message);
			}
		}

		void tmr_Tick (object sender, EventArgs e)
		{
			try {
				if (MouseButtons != System.Windows.Forms.MouseButtons.None)
					return;
				if (ActiveControl == lstFiles || ActiveControl == lstUnstagedHunks || ActiveControl == cmbRepository)
					return;
				if (ActiveControl == splitContainer1 && (splitContainer1.ActiveControl == lstFiles || splitContainer1.ActiveControl == lstUnstagedHunks || splitContainer1.ActiveControl == lstStagedHunks))
					return;
				ActiveControl = lstFiles;
			} catch (Exception ex) {
				MessageBox.Show (ex.Message);
			}
		}

		public void PrintList ()
		{
			try {
				diff.UpdateList ();

				List<Entry> entries = diff.entries;
				Entry selected = diff.selected;
				int selected_index = lstFiles.SelectedIndices.Count > 0 ? lstFiles.SelectedIndices [0] : -1;

				lstFiles.Items.Clear ();
				bool any_eol_issues = false;
				bool any_staged = false;
				bool any_staged_partially = false;
				bool any_deleted = false;
				bool any_untracked = false;
				bool any_binaries = false;
				int max_filename_length = 0;

				for (int i = 0; i < entries.Count; i++) {
					any_eol_issues |= entries [i].messed_up_eol;
					any_staged_partially |= (entries [i].staged && !entries [i].staged_whole);
					any_staged |= entries [i].staged_whole;
					any_deleted |= entries [i].deleted;
					any_untracked |= entries [i].untracked;
					any_binaries |= entries [i].is_binary;
					max_filename_length = Math.Max (max_filename_length, entries [i].filename.Length);
				}

				for (int i = 0; i < entries.Count; i++) {
					StringWriter state = new StringWriter ();
					ConsoleColor color = ConsoleColor.Black;

					if (selected != null && selected == entries [i]) {
						state.Write ("*");
					} else {
						state.Write (" ");
					}
					if (any_staged || any_staged_partially) {
						if (entries [i].staged_whole) {
							state.Write ("staged ");
							if (any_staged_partially)
								state.Write ("            ");
							color = ConsoleColor.Blue;
						} else if (entries [i].staged) {
							state.Write ("staged (partially) ");
							color = ConsoleColor.DarkBlue;
						} else {
							if (any_staged_partially) {
								state.Write ("   -               ");
							} else {
								state.Write ("   -   ");
							}
						}
					}
					if (any_binaries) {
						if (entries [i].is_binary) {
							state.Write ("binary ");
							color = ConsoleColor.DarkGreen;
						} else {
							state.Write ("       ");
						}
					}
					if (any_deleted) {
						if (entries [i].deleted) {
							state.Write ("deleted ");
							color = ConsoleColor.Red;
						} else {
							state.Write ("   -    ");
						}
					}
					if (any_untracked) {
						if (entries [i].untracked) {
							state.Write ("untracked ");
							color = ConsoleColor.Yellow;
						} else {
							state.Write ("     -    ");
						}
					}
					state.Write (" ");

					if (any_eol_issues) {
						if (entries [i].messed_up_eol) {
							state.Write ("EOL ");
							color = ConsoleColor.Magenta;
						} else {
							state.Write ("    ");
						}
					}

					state.Write (entries [i].eol);
					state.Write (" ");

					ListViewItem item = new ListViewItem ();
					for (int k = 0; k < 3; k++)
						item.SubItems.Add (string.Empty);
					item.SubItems [0].Text = entries [i].deleted ? "gone" : string.Empty;
					item.SubItems [1].Text = entries [i].untracked ? "new" : string.Empty;
					item.SubItems [2].Text = entries [i].messed_up_eol ? "EOL" : entries [i].eol;
					item.SubItems [3].Text = entries [i].staged_whole ? "staged" : (entries [i].staged ? "partial" : string.Empty);
					//item.SubItems [0].Text = state.ToString ();
					item.SubItems.Add (entries [i].filename);
					item.Tag = entries [i];
					Color clr = Color.Black;
					switch (color) {
					case ConsoleColor.Blue:
						clr = Color.Blue;
						break;
					case ConsoleColor.Magenta:
						clr = Color.Magenta;
						break;
					case ConsoleColor.Yellow:
						clr = Color.DarkGoldenrod;
						break;
					case ConsoleColor.Red:
						clr = Color.Red;
						break;
					case ConsoleColor.DarkGreen:
						clr = Color.DarkGreen;
						break;
					case ConsoleColor.Green:
						clr = Color.Green;
						break;
					case ConsoleColor.DarkBlue:
						clr = Color.DarkBlue;
						break;
					}
					item.ForeColor = clr;
					lstFiles.Items.Add (item);
				}
				if (selected_index >= lstFiles.Items.Count)
					selected_index = lstFiles.Items.Count - 1;
				if (selected_index >= 0) {
					lstFiles.Items [selected_index].Selected = true;
					lstFiles.Items [selected_index].Focused = true;
					lstFiles.EnsureVisible (selected_index);
				}
			} catch (Exception ex) {
				MessageBox.Show (ex.Message);
			}
		}

		public void Advance ()
		{
			int i = lstFiles.SelectedIndices [0] + 1;
			lstFiles.SelectedItems.Clear ();
			lstFiles.Items [i].Selected = true;
			lstFiles.Items [i].Focused = true;
		}

		private void ShowDiff (Entry entry)
		{
			string staged = diff.GetDiff (entry, true);
			string unstaged = diff.GetDiff (entry, false);
			//ShowDiff (diff.GetDiff (entry, staged));

			ShowHunks (diff.GetDiff (entry, false), lstUnstagedHunks);
			if (string.IsNullOrEmpty (unstaged)) {
				if (tabDiff.TabPages.Contains (tabUnstaged))
					tabDiff.TabPages.Remove (tabUnstaged);
			} else {
				if (!tabDiff.TabPages.Contains (tabUnstaged))
					tabDiff.TabPages.Add (tabUnstaged);
			}
	
			ShowHunks (diff.GetDiff (entry, true), lstStagedHunks);
			if (string.IsNullOrEmpty (staged)) {
				if (tabDiff.TabPages.Contains (tabStaged))
					tabDiff.TabPages.Remove (tabStaged);
			} else {
				if (!tabDiff.TabPages.Contains (tabStaged))
					tabDiff.TabPages.Insert (0, tabStaged);
				if (!index_processing)
					tabDiff.SelectedTab = tabStaged;
			}
			if (!index_processing)
				ActiveControl = lstFiles;
		}

		private static void AddHunk (List<string> lines, ListView lstHunks)
		{
			if (lines.Count == 0)
				return;

			ListViewGroup group = new ListViewGroup (lines [0]);
			group.HeaderAlignment = HorizontalAlignment.Left;
			group.Tag = lines;
			group.Name = "#" + (lstHunks.Groups.Count + 1).ToString ();
			lstHunks.Groups.Add (group);
			for (int i = 1; i < lines.Count; i++) {
				ListViewItem item = new ListViewItem ();
				item.Text = lines [i].Replace ("\t", "    ");
				item.Tag = lines [i];
				if (item.Text.StartsWith ("-")) {
					item.ForeColor = Color.Red;
				} else if (item.Text.StartsWith ("+")) {
					item.ForeColor = Color.Green;
				} else {
					//item.ForeColor = Color.Blue;
				}
				group.Items.Add (item);
				lstHunks.Items.Add (item);
			}
		}

		private static void ShowHunks (string diff, ListView lstHunks)
		{
			// StringBuilder sb = new StringBuilder ();
			string line;
			List<string> current_hunk = new List<string> ();

			lstHunks.Items.Clear ();
			lstHunks.Groups.Clear ();

			if (diff.Length > 1024 * 1024 * 128) {
				MessageBox.Show ("Diff too big", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}

			using (StringReader reader = new StringReader (diff)) {
				while (null != (line = reader.ReadLine ())) {
					if (line.StartsWith ("@@")) {
						AddHunk (current_hunk, lstHunks);
						current_hunk = new List<string> ();
					}
					current_hunk.Add (line);
				}
				AddHunk (current_hunk, lstHunks);
			}
		}

		private string ColorizeDiff (string diff)
		{
			StringBuilder sb = new StringBuilder ();
			string line;

			if (diff.Length > 1024 * 1024 * 128) {
				MessageBox.Show ("Diff too big", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return string.Empty;
			}

			using (StringWriter writer = new StringWriter (sb)) {
				using (StringReader reader = new StringReader (diff)) {
					writer.WriteLine ("<!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Strict//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd'>");
					writer.WriteLine ("<html xmlns='http://www.w3.org/1999/xhtml'>");
					writer.WriteLine ("<head>");
					writer.WriteLine ("<title>Diff</title>");
					writer.WriteLine ("</head>");
					writer.WriteLine ("<body style=\"font-family: 'Courier New'; font-size: 8pt;\">");
					writer.WriteLine ("<div>");
					while (null != (line = reader.ReadLine ())) {
						string start = null;
						if (line.StartsWith ("+++") || line.StartsWith ("---") || line.StartsWith ("diff") || line.StartsWith ("index")) {
							continue; // ignore
						} else if (line.StartsWith ("@@")) {
							start = "<span style='color: blue; font-weight: bold;'>";
						} else if (line.StartsWith ("-")) {
							start = "<span style='color: red;'>";
						} else if (line.StartsWith ("+")) {
							start = "<span style='color: green;'>";
						}
						if (start != null)
							writer.Write (start);
						writer.Write (System.Web.HttpUtility.HtmlEncode (line).Replace (" ", "&nbsp;").Replace ("\t", "&nbsp;&nbsp;&nbsp;&nbsp;"));
						if (start != null)
							writer.Write ("</span>");
						writer.WriteLine ("<br/>");
					}
					writer.WriteLine ("</div>");
					writer.WriteLine ("</body>");
					writer.WriteLine ("</html>");
				}
			}
			return sb.ToString ();
		}

		private void lstFiles_SelectedIndexChanged (object sender, EventArgs e)
		{
			try {
				if (lstFiles.SelectedItems.Count == 0)
					return;
				diff.selected = (Entry) lstFiles.SelectedItems [0].Tag;
				ShowDiff (diff.selected);
			} catch (Exception ex) {
				MessageBox.Show (ex.Message);
			}
		}

		private void lstFiles_KeyPress (object sender, KeyPressEventArgs e)
		{
			try {
				if (ActiveControl == cmbRepository)
					return;

				if (e.KeyChar == '\n' || e.KeyChar == '\r') {
				} else if (e.KeyChar == 8 || e.KeyChar == 27) {
					// skip
				} else {
					if (lblCommand.Visible == false)
						lblCommand.Text = string.Empty;
					lblCommand.Text += e.KeyChar;
					lblCommand.Visible = true;
					lblCommand.Location = lstFiles.SelectedItems [0].Bounds.Location;
					e.Handled = true;
				}
				e.Handled = true;
			} catch (Exception ex) {
				MessageBox.Show (ex.Message);
			}
		}

		private void lstFiles_KeyDown (object sender, KeyEventArgs e)
		{
			try {
				switch (e.KeyCode) {
				case Keys.Return:
					diff.cmds.Execute (lblCommand.Text);
					lblCommand.Visible = false;
					PrintList ();
					e.Handled = true;
					break;
				case Keys.Escape:
					lblCommand.Visible = false;
					e.Handled = true;
					break;
				case Keys.Back:
					if (lblCommand.Text.Length > 0)
						lblCommand.Text = lblCommand.Text.Substring (0, lblCommand.Text.Length - 1);
					e.Handled = true;
					break;
				}
			} catch (Exception ex) {
				MessageBox.Show (ex.Message);
			}
		}

		private static string GeneratePatch (ListView listHunks, bool unstage)
		{
			string tmpfile = null;
			List<string> header;
			List<string> hunk;
			List<string> diff = new List<string> ();
			ListViewGroup group;

			try {
				tmpfile = @"C:\Users\Rolf\AppData\Local\Temp\tmp2D8A.tmp";//Path.GetTempFileName ();
				header = (List<string>) listHunks.Groups [0].Tag;
				group = listHunks.SelectedItems [0].Group;
				hunk = (List<string>) group.Tag;
				using (StreamWriter w = new StreamWriter (tmpfile, false)) {
					w.NewLine = "\n";
					for (int i = 0; i < header.Count; i++)
						w.WriteLine (header [i]);

					int context_lines = 0;
					int lc_diff = 0;
					for (int i = 0; i < group.Items.Count; i++) {
						ListViewItem item = group.Items [i];
						if (!listHunks.SelectedItems.Contains (item)) {
							string line = (string) item.Tag;
							switch (line [0]) {
							case '+': // added lines -> remove from patch
								if (unstage) {
									context_lines++;
									diff.Add (" " + line.Substring (1));
								}
								continue;
							case '-': // removed lines -> context lines in patch
								if (!unstage) {
									context_lines++;
									diff.Add (" " + line.Substring (1));
								}
								continue;
							default: // context lines -> copy to patch
								context_lines++;
								diff.Add (line);
								break;
							}
						} else {
							string line = (string) item.Tag;
							switch (line [0]) {
							case '+':
								if (unstage) {
									diff.Add ("-" + line.Substring (1));
									lc_diff--;
									context_lines++;
								} else {
									diff.Add (line);
									lc_diff++;
								}
								break;
							case '-':
								if (unstage) {
									diff.Add ("+" + line.Substring (1));
									lc_diff++;
								} else {
									diff.Add (line);
									lc_diff--;
									context_lines++;
								}
								break;
							default:
								context_lines++;
								diff.Add (line);
								break;
							}
						}
					}

					string [] ranges = hunk [0].Split (' ', ',');

					string hunk_range = string.Format ("@@ {0},{1} {2},{3} @@", ranges [1], context_lines, ranges [3], context_lines + lc_diff);
					w.WriteLine (hunk_range);

					for (int i = 0; i < diff.Count; i++)
						w.WriteLine (diff [i]);
				}
				//MessageBox.Show (File.ReadAllText (tmpfile));
				//System.Diagnostics.Debug.WriteLine (tmpfile);
				//System.Diagnostics.Debug.WriteLine ("");
				//System.Diagnostics.Debug.Write (File.ReadAllText (tmpfile));
				return tmpfile;
			} catch (Exception ex) {
				MessageBox.Show (ex.Message);
				return null;
			}
		}

		private void stageLinesToolStripMenuItem_Click (object sender, EventArgs e)
		{
			string tmpfile = null;

			try {
				tmpfile = GeneratePatch (lstUnstagedHunks, false);
				this.diff.Stage (tmpfile);
				index_processing = true;
				PrintList ();
			} catch (Exception ex) {
				MessageBox.Show (ex.Message);
			} finally {
				index_processing = false;
				if (tmpfile != null) {
					try {
						//File.Delete (tmpfile);
					} catch {
					}
				}
			}
		}

		private void unstageLinesToolStripMenuItem1_Click (object sender, EventArgs e)
		{
			string tmpfile = null;

			try {
				tmpfile = GeneratePatch (lstStagedHunks, true);
				this.diff.Stage (tmpfile);
				index_processing = true;
				PrintList ();
			} catch (Exception ex) {
				MessageBox.Show (ex.Message);
			} finally {
				index_processing = false;
				if (tmpfile != null) {
					try {
						//File.Delete (tmpfile);
					} catch {
					}
				}
			}
		}

		private void stageHunkToolStripMenuItem_Click (object sender, EventArgs e)
		{
			string tmpfile = null;

			try {
				foreach (ListViewItem item in lstUnstagedHunks.SelectedItems [0].Group.Items)
					item.Selected = true;

				tmpfile = GeneratePatch (lstUnstagedHunks, false);
				this.diff.Stage (tmpfile);
				index_processing = true;
				PrintList ();
			} catch (Exception ex) {
				MessageBox.Show (ex.Message);
			} finally {
				index_processing = false;
				if (tmpfile != null) {
					try {
						//File.Delete (tmpfile);
					} catch {
					}
				}
			}
		}

		private void unstageHunkToolStripMenuItem1_Click (object sender, EventArgs e)
		{
			string tmpfile = null;

			try {
				foreach (ListViewItem item in lstStagedHunks.SelectedItems [0].Group.Items)
					item.Selected = true;

				tmpfile = GeneratePatch (lstStagedHunks, true);
				this.diff.Stage (tmpfile);
				index_processing = true;
				PrintList ();
			} catch (Exception ex) {
				MessageBox.Show (ex.Message);
			} finally {
				index_processing = false;
				if (tmpfile != null) {
					try {
						//File.Delete (tmpfile);
					} catch {
					}
				}
			}
		}

		private void cmbRepository_SelectedIndexChanged (object sender, EventArgs e)
		{
			try {
				if (!Directory.Exists (cmbRepository.Text)) {
					cmbRepository.BackColor = System.Drawing.Color.LightGray;
					return;
				} else {
					cmbRepository.BackColor = Color.White;
				}
				Environment.CurrentDirectory = cmbRepository.Text;
				diff.cmds.Execute ("r");
				lblCommand.Visible = false;
				PrintList ();
			} catch (Exception ex) {
				MessageBox.Show (ex.Message);
			}
		}
	}

	public class WinFormsDiff : Diff {
		public Commands cmds;
		public WinFormsGui gui;

		public WinFormsDiff ()
		{
			cmds = new Commands ()
		{
			{ "h|help|?", "Show this help message", delegate (string v)
				{
					cmds.Help ();
				}
			},
			{ "e|edit|kate", "Open file in editor (kate)", delegate (string v)
				{
					if (selected == null)
						throw new DiffException ("You need to select a file first.");
					Execute ("kate", selected.filename);
				}
			},
			{ "nano", "Open file in nano", delegate (string v)
				{
					if (selected == null)
						throw new DiffException ("You need to select a file first.");
					Execute ("nano", "-c " + selected.filename, false);
				}
			},
			{ "a|add", "Add file to index", delegate (string v)
				{
					if (selected == null)
						throw new DiffException ("You need to select a file first.");
					list_dirty = true;
					Execute ("git", (selected.deleted ? "rm -- " : "add ") + selected.QuotedFileName);
					gui.PrintList ();
				}
			},
			{ "add+next|an", "Add file to index and go to next file", delegate (string v)
				{
					if (selected == null)
						throw new DiffException ("You need to select a file first.");
					list_dirty = true;
					Execute ("git", (selected.deleted ? "rm -- " : "add ") + selected.QuotedFileName);
					if (selected == null)
						throw new DiffException ("You need to select a file first.");
					int idx = entries.IndexOf (selected);
					if (idx + 1 == entries.Count) {
						idx = 0;
					} else {
						idx++;
					}
					selected = entries [idx];
					gui.Advance ();
				}
			},
			{ "addall|add -u", "Add all changed files to the index", delegate (string v)
				{
					list_dirty = true;
					foreach (var entry in entries) {
						if (!entry.untracked) {
							Execute ("git", "add " + entry.filename);
							Console.WriteLine ("Added " + entry.filename);
						}
					}
					gui.PrintList ();
				}
			},
			{ "addalluntracked", "Add all untracked files to the index", delegate (string v)
				{
					list_dirty = true;
					foreach (var entry in entries) {
						if (entry.untracked) {
							Execute ("git", "add " + entry.QuotedFileName);
							Console.WriteLine ("Added " + entry.filename);
						}
					}
					gui.PrintList ();
				}
			},
			{ "p|add -p", "Add file to index in interactive mode", delegate (string v)
				{
					if (selected == null)
						throw new DiffException ("You need to select a file first.");
					list_dirty = true;
					Execute ("git", "add -p " + selected.QuotedFileName, false);
				}
			},
			{ "ci|gitci|git ci", "Commit using gitci", delegate (string v)
				{
					Execute ("gitci", "", false);
					list_dirty = true;
					gui.PrintList ();
				}
			},
			{ "commit", "Commit using git (git commit)", delegate (string v)
				{
					list_dirty = true;
					Execute ("git", "commit", false);
					gui.PrintList ();
				}
			},
			{ "dt|dm|difftool|diffmerge", "Show the diff using diffmerge", delegate (string v)
				{
					list_dirty = true;
					Execute ("git", "difftool --no-prompt --extcmd=/c/Users/Rolf/bin/diffmerge-diff2.sh -- " + selected.QuotedFileName, false);
					gui.PrintList ();
				}
			},
			//{ "d|diff", "Show the diff for the selected file", delegate (string v)
			//    {
			//        gui.ShowDiff (false);
			//    }
			//},
			{ "vs", "Open in VS", delegate (string v)
				{
					Execute (Path.Combine (Environment.CurrentDirectory, selected.filename), string.Empty, false, false, true);
				}
			},
			//{ "sd|diff --staged|stageddiff|staged diff", "Show the staged diff", delegate (string v)
			//    {
			//        gui.ShowDiff (true);
			//    }
			//},
			{ "rm|delete", "Delete the selected files", delegate (string v)
				{
					if (selected == null)
						throw new DiffException ("You need to select a file first.");
					list_dirty = true;
					if (selected.untracked) {
						Execute ("rm", selected.QuotedFileName);
					} else {
						Execute ("git", "rm -f " + selected.QuotedFileName);
					}
					gui.PrintList ();
				}
			},
			{ "amend", "Executes git commit --amend", delegate (string v)
				{
					list_dirty = true;
					Execute ("git", "commit --amend", false);
					gui.PrintList ();
				}
			},
			{ "reset", "Executes git reset on the selected file", delegate (string v)
				{
					if (selected == null)
						throw new DiffException ("You need to select a file first.");
					list_dirty = true;
					Execute ("git", "reset -- " + selected.QuotedFileName);
				}
			},
			{ "checkout", "Checks out the selected file (equivalent to svn revert)", delegate (string v)
				{
					if (selected == null)
						throw new DiffException ("You need to select a file first.");
					list_dirty = true;
					Execute ("git", "checkout " + selected.QuotedFileName);
					gui.PrintList ();
				}
			},
			{ "meld", "View the selected file in meld", delegate (string v)
				{
					if (selected == null)
						throw new DiffException ("You need to select a file first.");
					list_dirty = true;
					Execute ("muld", Path.Combine (Environment.CurrentDirectory, selected.filename), false);
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
					gui.PrintList ();
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
					gui.PrintList ();
				}
			},
			{ "dos2unix|unix2dos|dosunix|unixdos", "Change the eol-style to dos/unix for the selected file", delegate (string v)
				{
					if (selected == null)
						throw new DiffException ("You need to select a file first.");
					switch (v) {
					case "dos2unix":
					case "dosunix":
						Dos2Unix (selected.filename);
						break;
					case "unix2dos":
					case "unixdos":
						Unix2Dos (selected.filename);
						break;
					}
					list_dirty = true;
					gui.PrintList ();
				}
			},
			{ "i|ignore", "Add file to .gitignore in current directory", delegate (string v)
				{
					if (selected == null)
						throw new DiffException ("You need to select a file first.");
					File.AppendAllText (Path.Combine (Path.GetDirectoryName (selected.filename), ".gitignore"), Path.GetFileName (selected.filename) + '\n');
					list_dirty = true;
				}
			},
			{ "ignore-extension", "Add extension of file to .gitignore in current directory", delegate (string v)
				{
					if (selected == null)
						throw new DiffException ("You need to select a file first.");
					File.AppendAllText (Path.Combine (Path.GetDirectoryName (selected.filename), ".gitignore"), "*" + Path.GetExtension (selected.filename) + '\n');
					list_dirty = true;
				}
			},
		};
		}

		protected override void Run ()
		{
			Application.EnableVisualStyles ();
			Application.Run (new WinFormsGui (this));
		}

		public void UpdateList ()
		{
			if (list_dirty)
				RefreshList ();
		}
	}
}

