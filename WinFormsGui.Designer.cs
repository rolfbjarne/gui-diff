namespace gui_diff
{
	partial class WinFormsGui
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose (bool disposing)
		{
			if (disposing && (components != null)) {
				components.Dispose ();
			}
			base.Dispose (disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent ()
		{
			this.components = new System.ComponentModel.Container ();
			this.lstFiles = new System.Windows.Forms.ListView ();
			this.col1 = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader ()));
			this.colTracked = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader ()));
			this.colEOL = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader ()));
			this.colStaged = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader ()));
			this.col2 = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader ()));
			this.splitContainer1 = new System.Windows.Forms.SplitContainer ();
			this.lblCommand = new System.Windows.Forms.Label ();
			this.tabDiff = new System.Windows.Forms.TabControl ();
			this.mnuStage = new System.Windows.Forms.ContextMenuStrip (this.components);
			this.stageHunkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.revertHunkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator ();
			this.stageLinesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.revertLinesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.tabStaged = new System.Windows.Forms.TabPage ();
			this.lstStagedHunks = new System.Windows.Forms.ListView ();
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader ()));
			this.mnuUnstage = new System.Windows.Forms.ContextMenuStrip (this.components);
			this.unstageHunkToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem ();
			this.unstageLinesToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem ();
			this.tabUnstaged = new System.Windows.Forms.TabPage ();
			this.lstUnstagedHunks = new System.Windows.Forms.ListView ();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader ()));
			this.cmbRepository = new System.Windows.Forms.ComboBox ();
			((System.ComponentModel.ISupportInitialize) (this.splitContainer1)).BeginInit ();
			this.splitContainer1.Panel1.SuspendLayout ();
			this.splitContainer1.Panel2.SuspendLayout ();
			this.splitContainer1.SuspendLayout ();
			this.tabDiff.SuspendLayout ();
			this.mnuStage.SuspendLayout ();
			this.tabStaged.SuspendLayout ();
			this.mnuUnstage.SuspendLayout ();
			this.tabUnstaged.SuspendLayout ();
			this.SuspendLayout ();
			// 
			// lstFiles
			// 
			this.lstFiles.Columns.AddRange (new System.Windows.Forms.ColumnHeader [] {
            this.col1,
            this.colTracked,
            this.colEOL,
            this.colStaged,
            this.col2});
			this.lstFiles.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lstFiles.FullRowSelect = true;
			this.lstFiles.HideSelection = false;
			this.lstFiles.Location = new System.Drawing.Point (0, 0);
			this.lstFiles.Name = "lstFiles";
			this.lstFiles.Size = new System.Drawing.Size (438, 752);
			this.lstFiles.TabIndex = 0;
			this.lstFiles.UseCompatibleStateImageBehavior = false;
			this.lstFiles.View = System.Windows.Forms.View.Details;
			this.lstFiles.SelectedIndexChanged += new System.EventHandler (this.lstFiles_SelectedIndexChanged);
			// 
			// col1
			// 
			this.col1.Text = "Alive";
			this.col1.Width = 45;
			// 
			// colTracked
			// 
			this.colTracked.Text = "Track";
			this.colTracked.Width = 45;
			// 
			// colEOL
			// 
			this.colEOL.Text = "EOL";
			this.colEOL.Width = 45;
			// 
			// colStaged
			// 
			this.colStaged.Text = "Stage";
			this.colStaged.Width = 45;
			// 
			// col2
			// 
			this.col2.Text = "File";
			this.col2.Width = 800;
			// 
			// splitContainer1
			// 
			this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.splitContainer1.Location = new System.Drawing.Point (0, 22);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add (this.lblCommand);
			this.splitContainer1.Panel1.Controls.Add (this.lstFiles);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add (this.tabDiff);
			this.splitContainer1.Size = new System.Drawing.Size (1114, 752);
			this.splitContainer1.SplitterDistance = 438;
			this.splitContainer1.TabIndex = 1;
			// 
			// lblCommand
			// 
			this.lblCommand.AutoSize = true;
			this.lblCommand.Location = new System.Drawing.Point (386, 121);
			this.lblCommand.Name = "lblCommand";
			this.lblCommand.Size = new System.Drawing.Size (0, 13);
			this.lblCommand.TabIndex = 2;
			this.lblCommand.Visible = false;
			// 
			// tabDiff
			// 
			this.tabDiff.ContextMenuStrip = this.mnuStage;
			this.tabDiff.Controls.Add (this.tabStaged);
			this.tabDiff.Controls.Add (this.tabUnstaged);
			this.tabDiff.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabDiff.Location = new System.Drawing.Point (0, 0);
			this.tabDiff.Name = "tabDiff";
			this.tabDiff.SelectedIndex = 0;
			this.tabDiff.Size = new System.Drawing.Size (672, 752);
			this.tabDiff.TabIndex = 2;
			// 
			// mnuStage
			// 
			this.mnuStage.Items.AddRange (new System.Windows.Forms.ToolStripItem [] {
            this.stageHunkToolStripMenuItem,
            this.revertHunkToolStripMenuItem,
            this.toolStripMenuItem1,
            this.stageLinesToolStripMenuItem,
            this.revertLinesToolStripMenuItem});
			this.mnuStage.Name = "contextMenuStrip1";
			this.mnuStage.Size = new System.Drawing.Size (143, 98);
			// 
			// stageHunkToolStripMenuItem
			// 
			this.stageHunkToolStripMenuItem.Name = "stageHunkToolStripMenuItem";
			this.stageHunkToolStripMenuItem.Size = new System.Drawing.Size (142, 22);
			this.stageHunkToolStripMenuItem.Text = "Stage hunk";
			this.stageHunkToolStripMenuItem.Click += new System.EventHandler (this.stageHunkToolStripMenuItem_Click);
			// 
			// revertHunkToolStripMenuItem
			// 
			this.revertHunkToolStripMenuItem.Name = "revertHunkToolStripMenuItem";
			this.revertHunkToolStripMenuItem.Size = new System.Drawing.Size (142, 22);
			this.revertHunkToolStripMenuItem.Text = "Revert hunk";
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size (139, 6);
			// 
			// stageLinesToolStripMenuItem
			// 
			this.stageLinesToolStripMenuItem.Name = "stageLinesToolStripMenuItem";
			this.stageLinesToolStripMenuItem.Size = new System.Drawing.Size (142, 22);
			this.stageLinesToolStripMenuItem.Text = "Stage line(s)";
			this.stageLinesToolStripMenuItem.Click += new System.EventHandler (this.stageLinesToolStripMenuItem_Click);
			// 
			// revertLinesToolStripMenuItem
			// 
			this.revertLinesToolStripMenuItem.Name = "revertLinesToolStripMenuItem";
			this.revertLinesToolStripMenuItem.Size = new System.Drawing.Size (142, 22);
			this.revertLinesToolStripMenuItem.Text = "Revert line(s)";
			// 
			// tabStaged
			// 
			this.tabStaged.Controls.Add (this.lstStagedHunks);
			this.tabStaged.Location = new System.Drawing.Point (4, 22);
			this.tabStaged.Name = "tabStaged";
			this.tabStaged.Padding = new System.Windows.Forms.Padding (3);
			this.tabStaged.Size = new System.Drawing.Size (664, 726);
			this.tabStaged.TabIndex = 2;
			this.tabStaged.Text = "Staged";
			this.tabStaged.UseVisualStyleBackColor = true;
			// 
			// lstStagedHunks
			// 
			this.lstStagedHunks.BackColor = System.Drawing.Color.FromArgb (((int) (((byte) (245)))), ((int) (((byte) (245)))), ((int) (((byte) (255)))));
			this.lstStagedHunks.Columns.AddRange (new System.Windows.Forms.ColumnHeader [] {
            this.columnHeader2});
			this.lstStagedHunks.ContextMenuStrip = this.mnuUnstage;
			this.lstStagedHunks.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lstStagedHunks.Font = new System.Drawing.Font ("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
			this.lstStagedHunks.FullRowSelect = true;
			this.lstStagedHunks.Location = new System.Drawing.Point (3, 3);
			this.lstStagedHunks.Name = "lstStagedHunks";
			this.lstStagedHunks.Size = new System.Drawing.Size (658, 720);
			this.lstStagedHunks.TabIndex = 1;
			this.lstStagedHunks.UseCompatibleStateImageBehavior = false;
			this.lstStagedHunks.View = System.Windows.Forms.View.Details;
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Diff";
			this.columnHeader2.Width = 3000;
			// 
			// mnuUnstage
			// 
			this.mnuUnstage.Items.AddRange (new System.Windows.Forms.ToolStripItem [] {
            this.unstageHunkToolStripMenuItem1,
            this.unstageLinesToolStripMenuItem1});
			this.mnuUnstage.Name = "mnuUnstage";
			this.mnuUnstage.Size = new System.Drawing.Size (153, 48);
			// 
			// unstageHunkToolStripMenuItem1
			// 
			this.unstageHunkToolStripMenuItem1.Name = "unstageHunkToolStripMenuItem1";
			this.unstageHunkToolStripMenuItem1.Size = new System.Drawing.Size (152, 22);
			this.unstageHunkToolStripMenuItem1.Text = "Unstage hunk";
			this.unstageHunkToolStripMenuItem1.Click += new System.EventHandler (this.unstageHunkToolStripMenuItem1_Click);
			// 
			// unstageLinesToolStripMenuItem1
			// 
			this.unstageLinesToolStripMenuItem1.Name = "unstageLinesToolStripMenuItem1";
			this.unstageLinesToolStripMenuItem1.Size = new System.Drawing.Size (152, 22);
			this.unstageLinesToolStripMenuItem1.Text = "Unstage line(s)";
			this.unstageLinesToolStripMenuItem1.Click += new System.EventHandler (this.unstageLinesToolStripMenuItem1_Click);
			// 
			// tabUnstaged
			// 
			this.tabUnstaged.Controls.Add (this.lstUnstagedHunks);
			this.tabUnstaged.Location = new System.Drawing.Point (4, 22);
			this.tabUnstaged.Name = "tabUnstaged";
			this.tabUnstaged.Padding = new System.Windows.Forms.Padding (3);
			this.tabUnstaged.Size = new System.Drawing.Size (664, 726);
			this.tabUnstaged.TabIndex = 1;
			this.tabUnstaged.Text = "Unstaged";
			this.tabUnstaged.UseVisualStyleBackColor = true;
			// 
			// lstUnstagedHunks
			// 
			this.lstUnstagedHunks.Columns.AddRange (new System.Windows.Forms.ColumnHeader [] {
            this.columnHeader1});
			this.lstUnstagedHunks.ContextMenuStrip = this.mnuStage;
			this.lstUnstagedHunks.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lstUnstagedHunks.Font = new System.Drawing.Font ("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
			this.lstUnstagedHunks.FullRowSelect = true;
			this.lstUnstagedHunks.Location = new System.Drawing.Point (3, 3);
			this.lstUnstagedHunks.Name = "lstUnstagedHunks";
			this.lstUnstagedHunks.Size = new System.Drawing.Size (658, 747);
			this.lstUnstagedHunks.TabIndex = 0;
			this.lstUnstagedHunks.UseCompatibleStateImageBehavior = false;
			this.lstUnstagedHunks.View = System.Windows.Forms.View.Details;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Diff";
			this.columnHeader1.Width = 3000;
			// 
			// cmbRepository
			// 
			this.cmbRepository.Dock = System.Windows.Forms.DockStyle.Top;
			this.cmbRepository.FormattingEnabled = true;
			this.cmbRepository.Location = new System.Drawing.Point (0, 0);
			this.cmbRepository.Name = "cmbRepository";
			this.cmbRepository.Size = new System.Drawing.Size (1114, 21);
			this.cmbRepository.TabIndex = 2;
			this.cmbRepository.SelectedIndexChanged += new System.EventHandler (this.cmbRepository_SelectedIndexChanged);
			this.cmbRepository.TextChanged += new System.EventHandler (this.cmbRepository_SelectedIndexChanged);
			// 
			// WinFormsGui
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF (6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size (1114, 774);
			this.Controls.Add (this.cmbRepository);
			this.Controls.Add (this.splitContainer1);
			this.Font = new System.Drawing.Font ("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte) (0)));
			this.KeyPreview = true;
			this.Name = "WinFormsGui";
			this.Text = "gui";
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			this.Load += new System.EventHandler (this.gui_Load);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler (this.lstFiles_KeyDown);
			this.KeyPress += new System.Windows.Forms.KeyPressEventHandler (this.lstFiles_KeyPress);
			this.splitContainer1.Panel1.ResumeLayout (false);
			this.splitContainer1.Panel1.PerformLayout ();
			this.splitContainer1.Panel2.ResumeLayout (false);
			((System.ComponentModel.ISupportInitialize) (this.splitContainer1)).EndInit ();
			this.splitContainer1.ResumeLayout (false);
			this.tabDiff.ResumeLayout (false);
			this.mnuStage.ResumeLayout (false);
			this.tabStaged.ResumeLayout (false);
			this.mnuUnstage.ResumeLayout (false);
			this.tabUnstaged.ResumeLayout (false);
			this.ResumeLayout (false);

		}

		#endregion

		private System.Windows.Forms.ListView lstFiles;
		private System.Windows.Forms.ColumnHeader col1;
		private System.Windows.Forms.ColumnHeader col2;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.ColumnHeader colTracked;
		private System.Windows.Forms.ColumnHeader colEOL;
		private System.Windows.Forms.ColumnHeader colStaged;
		private System.Windows.Forms.Label lblCommand;
		private System.Windows.Forms.TabControl tabDiff;
		private System.Windows.Forms.TabPage tabUnstaged;
		private System.Windows.Forms.ListView lstUnstagedHunks;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ContextMenuStrip mnuStage;
		private System.Windows.Forms.ToolStripMenuItem stageHunkToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem revertHunkToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem stageLinesToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem revertLinesToolStripMenuItem;
		private System.Windows.Forms.TabPage tabStaged;
		private System.Windows.Forms.ListView lstStagedHunks;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.ContextMenuStrip mnuUnstage;
		private System.Windows.Forms.ToolStripMenuItem unstageHunkToolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem unstageLinesToolStripMenuItem1;
		private System.Windows.Forms.ComboBox cmbRepository;
	}
}

