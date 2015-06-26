using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DZxEditor
{
    public partial class AddChunkForm : Form
    {
        public string ChunkType;

        public AddChunkForm()
        {
            InitializeComponent();
        }

        private void AddChunkForm_Shown(object sender, EventArgs e)
        {
            chunkSelectorBox.SelectedIndex = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ChunkType = (string)chunkSelectorBox.SelectedItem;
        }
    }
}
