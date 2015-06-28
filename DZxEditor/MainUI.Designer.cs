namespace DZxEditor
{
    partial class MainUI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.Viewport = new OpenTK.GLControl();
            this.ElementView = new System.Windows.Forms.TreeView();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addChunkToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openarcToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.opendzrdzsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportToArchiveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addChunkToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.panel1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // Viewport
            // 
            this.Viewport.BackColor = System.Drawing.Color.Black;
            this.Viewport.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.Viewport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Viewport.Location = new System.Drawing.Point(166, 0);
            this.Viewport.Name = "Viewport";
            this.Viewport.Size = new System.Drawing.Size(398, 391);
            this.Viewport.TabIndex = 0;
            this.Viewport.VSync = false;
            this.Viewport.Load += new System.EventHandler(this.Viewport_Load);
            this.Viewport.Click += new System.EventHandler(this.Viewport_Click);
            this.Viewport.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Viewport_KeyDown);
            this.Viewport.KeyUp += new System.Windows.Forms.KeyEventHandler(this.Viewport_KeyUp);
            this.Viewport.Leave += new System.EventHandler(this.Viewport_Leave);
            this.Viewport.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Viewport_MouseDown);
            this.Viewport.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Viewport_MouseUp);
            this.Viewport.Resize += new System.EventHandler(this.Viewport_Resize);
            // 
            // ElementView
            // 
            this.ElementView.Dock = System.Windows.Forms.DockStyle.Left;
            this.ElementView.HideSelection = false;
            this.ElementView.Location = new System.Drawing.Point(0, 0);
            this.ElementView.Name = "ElementView";
            this.ElementView.Size = new System.Drawing.Size(166, 391);
            this.ElementView.TabIndex = 1;
            this.ElementView.BeforeSelect += new System.Windows.Forms.TreeViewCancelEventHandler(this.ElementView_BeforeSelect);
            this.ElementView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.ElementView_AfterSelect);
            this.ElementView.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.ElementView_NodeMouseClick);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel1.Location = new System.Drawing.Point(564, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(129, 391);
            this.panel1.TabIndex = 2;
            this.panel1.Visible = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "label1";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addToolStripMenuItem,
            this.addChunkToolStripMenuItem1,
            this.deleteToolStripMenuItem,
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(143, 114);
            // 
            // addToolStripMenuItem
            // 
            this.addToolStripMenuItem.Name = "addToolStripMenuItem";
            this.addToolStripMenuItem.Size = new System.Drawing.Size(142, 22);
            this.addToolStripMenuItem.Text = "Add Element";
            this.addToolStripMenuItem.Click += new System.EventHandler(this.addToolStripMenuItem_Click);
            // 
            // addChunkToolStripMenuItem1
            // 
            this.addChunkToolStripMenuItem1.Name = "addChunkToolStripMenuItem1";
            this.addChunkToolStripMenuItem1.Size = new System.Drawing.Size(142, 22);
            this.addChunkToolStripMenuItem1.Text = "Add Chunk";
            this.addChunkToolStripMenuItem1.Click += new System.EventHandler(this.addChunkToolStripMenuItem1_Click);
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(142, 22);
            this.deleteToolStripMenuItem.Text = "Delete";
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(142, 22);
            this.copyToolStripMenuItem.Text = "Copy";
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
            // 
            // pasteToolStripMenuItem
            // 
            this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            this.pasteToolStripMenuItem.Size = new System.Drawing.Size(142, 22);
            this.pasteToolStripMenuItem.Text = "Paste";
            this.pasteToolStripMenuItem.Click += new System.EventHandler(this.pasteToolStripMenuItem_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.addChunkToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(166, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(398, 24);
            this.menuStrip1.TabIndex = 3;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openarcToolStripMenuItem,
            this.opendzrdzsToolStripMenuItem,
            this.exportToArchiveToolStripMenuItem,
            this.exportToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openarcToolStripMenuItem
            // 
            this.openarcToolStripMenuItem.Name = "openarcToolStripMenuItem";
            this.openarcToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            this.openarcToolStripMenuItem.Text = "Open *.arc";
            this.openarcToolStripMenuItem.Click += new System.EventHandler(this.openarcToolStripMenuItem_Click);
            // 
            // opendzrdzsToolStripMenuItem
            // 
            this.opendzrdzsToolStripMenuItem.Name = "opendzrdzsToolStripMenuItem";
            this.opendzrdzsToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            this.opendzrdzsToolStripMenuItem.Text = "Open *.dzr/*.dzs";
            this.opendzrdzsToolStripMenuItem.Click += new System.EventHandler(this.opendzrdzsToolStripMenuItem_Click);
            // 
            // exportToArchiveToolStripMenuItem
            // 
            this.exportToArchiveToolStripMenuItem.Name = "exportToArchiveToolStripMenuItem";
            this.exportToArchiveToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            this.exportToArchiveToolStripMenuItem.Text = "Export to Archive";
            this.exportToArchiveToolStripMenuItem.Click += new System.EventHandler(this.exportToArchiveToolStripMenuItem_Click);
            // 
            // exportToolStripMenuItem
            // 
            this.exportToolStripMenuItem.Name = "exportToolStripMenuItem";
            this.exportToolStripMenuItem.Size = new System.Drawing.Size(165, 22);
            this.exportToolStripMenuItem.Text = "Export to DZx File";
            this.exportToolStripMenuItem.Click += new System.EventHandler(this.exportToolStripMenuItem_Click);
            // 
            // addChunkToolStripMenuItem
            // 
            this.addChunkToolStripMenuItem.Name = "addChunkToolStripMenuItem";
            this.addChunkToolStripMenuItem.Size = new System.Drawing.Size(79, 20);
            this.addChunkToolStripMenuItem.Text = "Add Chunk";
            this.addChunkToolStripMenuItem.Click += new System.EventHandler(this.addChunkToolStripMenuItem_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // MainUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(693, 391);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.Viewport);
            this.Controls.Add(this.ElementView);
            this.Controls.Add(this.panel1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainUI";
            this.Text = "Form1";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public OpenTK.GLControl Viewport;
        public System.Windows.Forms.TreeView ElementView;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem addToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addChunkToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openarcToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.ToolStripMenuItem addChunkToolStripMenuItem1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.ToolStripMenuItem opendzrdzsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportToArchiveToolStripMenuItem;
    }
}

