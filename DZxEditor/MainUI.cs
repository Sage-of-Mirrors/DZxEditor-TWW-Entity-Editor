using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using GameFormatReader.Common;
using OpenTK.Graphics.OpenGL;

namespace DZxEditor
{
    public partial class MainUI : Form
    {
        Worker Work;

        bool IsTreeNodeClicked = false;

        public MainUI()
        {
            InitializeComponent();

            Work = new Worker(this);

            FileStream stream = new FileStream("C:\\Program Files (x86)\\SZS Tools\\Root Pure\\res\\Stage\\A_mori\\Stage.arc", FileMode.Open);

            EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Big);

            reader.BaseStream.Position += 4;

            byte[] test = Work.DecodeYaz0(reader);

            FileStream outSTream = new FileStream("C:\\Program Files (x86)\\SZS Tools\\amoristagetest.arc", FileMode.Create);

            EndianBinaryWriter writer = new EndianBinaryWriter(outSTream, Endian.Big);

            writer.Write(test);
        }

        private void Viewport_Load(object sender, EventArgs e)
        {
            if (Viewport.IsHandleCreated)
                Work.SetUpViewport();
            /*
            FileStream stream = new FileStream("C:\\Program Files (x86)\\SZS Tools\\De-Arc-ed Stage\\M_NewD2.wrkDir\\Room2\\dzr\\room.dzr", FileMode.Open);

            EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Big);

            Work.Read(reader, this);
             */
        }

        private void Viewport_KeyDown(object sender, KeyEventArgs e)
        {
            Input.Internal_SetKeyState(e.KeyCode, true);
        }

        private void Viewport_KeyUp(object sender, KeyEventArgs e)
        {
            Input.Internal_SetKeyState(e.KeyCode, false);
        }

        private void Viewport_MouseDown(object sender, MouseEventArgs e)
        {
            Input.Internal_SetMouseBtnState(e.Button, true);
        }

        private void Viewport_MouseUp(object sender, MouseEventArgs e)
        {
            Input.Internal_SetMouseBtnState(e.Button, false);
        }

        private void Viewport_Resize(object sender, EventArgs e)
        {
            GL.Viewport(0, 0, Viewport.Width, Viewport.Height);
        }

        private void ElementView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if ((e.Button == System.Windows.Forms.MouseButtons.Right) || (e.Button == System.Windows.Forms.MouseButtons.Left))
            {
                IsTreeNodeClicked = true;
                ElementView.SelectedNode = ElementView.GetNodeAt(e.X, e.Y);

                Work.ChangeSelectionFromTreeNode(ElementView.GetNodeAt(e.X, e.Y));

                if (e.Button == System.Windows.Forms.MouseButtons.Right)
                    contextMenuStrip1.Show(Cursor.Position);
            }

            IsTreeNodeClicked = false;
        }

        private void addToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Work.AddChunkElement();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ElementView.SelectedNode.Parent != null)
                Work.DeleteChunkElement();

            else
                Work.DeleteChunk();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ElementView.SelectedNode.Parent != null)
                Work.CopyChunkElement();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Work.PasteChunkElement();
        }

        private void ElementView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
        }

        private void ElementView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            ElementView.Focus();
        }

        private void Viewport_Click(object sender, EventArgs e)
        {
            MouseEventArgs mouse = (MouseEventArgs)e;

            Point mouseLocation = mouse.Location;

            Work.CalculateRay(mouseLocation);
        }

        private void openarcToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Work.LoadFromArc(openFileDialog1.FileName);
            }
        }

        private void addChunkToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ProcAddChunk();
        }

        private void ProcAddChunk()
        {
            if (Work.IsListLoaded)
            {
                AddChunkForm addForm = new AddChunkForm();

                if (addForm.ShowDialog() == DialogResult.OK)
                {
                    Work.AddChunk(addForm.ChunkType);
                }
            }
        }

        private void addChunkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProcAddChunk();
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Work.IsListLoaded)
            {
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    Work.SaveToDzx(saveFileDialog1.FileName);
                }
            }
        }

        private void Viewport_Leave(object sender, EventArgs e)
        {
            //If you don't clear the _keysDown list after leaving the control, a glitch where occur where if you're holding a key when
            //the control loses focus, Input will report that it is pressed even if you aren't actually pressing it.
            Input.ClearKeys();
        }

        private void opendzrdzsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Work.LoadFromDzx(openFileDialog1.FileName);
            }
        }

        private void exportToArchiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Work.IsListLoaded)
            {
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    Work.SaveToArc(saveFileDialog1.FileName);
                }
            }
        }
    }
}
