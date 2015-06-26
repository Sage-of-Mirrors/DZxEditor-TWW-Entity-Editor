namespace DZxEditor
{
    partial class AddChunkForm
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
            this.chunkSelectorBox = new System.Windows.Forms.ComboBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // chunkSelectorBox
            // 
            this.chunkSelectorBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.chunkSelectorBox.FormattingEnabled = true;
            this.chunkSelectorBox.Items.AddRange(new object[] {
            "2DMA",
            "ACTR",
            "AROB",
            "CAMR",
            "DMAP",
            "EVNT",
            "FILI",
            "FLOR",
            "LBNK",
            "LGHT",
            "LGTV",
            "MECO",
            "MEMA",
            "MULT",
            "PATH",
            "PLYR",
            "PPNT",
            "RARO",
            "RCAM",
            "RPAT",
            "RPPN",
            "RTBL",
            "SCLS",
            "SCOB",
            "SHIP",
            "SOND",
            "STAG",
            "TGDR",
            "TGOB",
            "TGSC",
            "TRES",
            "ACT1",
            "ACT2",
            "ACT3",
            "ACT4",
            "ACT5",
            "ACT6",
            "ACT7",
            "ACT8",
            "ACT9",
            "ACTa",
            "ACTb",
            "SCO0",
            "SCO1",
            "SCO2",
            "SCO3",
            "SCO4",
            "SCO5",
            "SCO6",
            "SCO7",
            "SCO8",
            "SCO9",
            "SCOa",
            "SCOb",
            "TRE0",
            "TRE1",
            "TRE2",
            "TRE3",
            "TRE4",
            "TRE5",
            "TRE6",
            "TRE7",
            "TRE8",
            "TRE9",
            "TREa",
            "TREb"});
            this.chunkSelectorBox.Location = new System.Drawing.Point(12, 12);
            this.chunkSelectorBox.Name = "chunkSelectorBox";
            this.chunkSelectorBox.Size = new System.Drawing.Size(121, 21);
            this.chunkSelectorBox.TabIndex = 0;
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Location = new System.Drawing.Point(170, 10);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(87, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button2.Location = new System.Drawing.Point(275, 10);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(87, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // AddChunkForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(374, 41);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.chunkSelectorBox);
            this.Name = "AddChunkForm";
            this.Text = "Select a Chunk Type";
            this.Shown += new System.EventHandler(this.AddChunkForm_Shown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox chunkSelectorBox;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}