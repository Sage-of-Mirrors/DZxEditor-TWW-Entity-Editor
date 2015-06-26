using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameFormatReader.Common;
using GameFormatReader.GCWii.Binaries.GC;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OpenTK;
using System.Xml;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

namespace DZxEditor
{
    public enum FieldType
    {
        Byte,
        Short,
        Integer,
        Float,
        String,
        ListByte,
        Vector2,
        Vector3
    }

    public enum GeometryType
    {
        None,
        Cube,
        Sphere,
        Pyramid,
        Door
    }

    public enum ShaderAttributeIds
    {
        Position, Color,
        TexCoord, Normal
    }

    /// <summary>
    /// Main class of the program. Mediates between data structure and UI.
    /// </summary>
    class Worker
    {
        #region Variables

        List<Chunk> Chunks;

        List<EntityTemplate> ChunkTemplates;

        Chunk SelectedChunk;

        Stopwatch Stop;

        Timer time;

        Form1 MainForm;

        Camera Cam;

        Matrix4 ViewMatrix;

        Matrix4 ProjMatrix;

        TreeNode CurSelectedNode;

        public bool IsListLoaded = false;

        int _programID;

        int _uniformMVP;

        int _uniformColor;

        #endregion

        #region Init

        public Worker(Form1 mainForm)
        {
            Chunks = new List<Chunk>();

            MainForm = mainForm;
        }

        private List<EntityTemplate> LoadTemplates()
        {
            string folderPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Templates/";

            DirectoryInfo dI = new DirectoryInfo(folderPath);

            List<EntityTemplate> itemTemplates = new List<EntityTemplate>();

            foreach (var file in dI.GetFiles())
            {
                var template = JsonConvert.DeserializeObject<EntityTemplate>(File.ReadAllText(file.FullName));
                itemTemplates.Add(template);
            }

            return itemTemplates;
        }

        protected void LoadShader(string fileName, ShaderType type, int program, out int address)
        {
            //Gets an id from OpenGL
            address = GL.CreateShader(type);
            using (var streamReader = new StreamReader(fileName))
            {
                GL.ShaderSource(address, streamReader.ReadToEnd());
            }
            //Compiles the shader code
            GL.CompileShader(address);
            //Tells OpenGL that this shader (be it vertex of fragment) belongs to the specified program
            GL.AttachShader(program, address);

            //Error checking.
            int compileSuccess;
            GL.GetShader(address, ShaderParameter.CompileStatus, out compileSuccess);

            if (compileSuccess == 0)
                Console.WriteLine(GL.GetShaderInfoLog(address));
        }

        public void SetUpViewport()
        {
            //_programID = GL.CreateProgram();

            ////Create the Vertex and Fragment shader from file using our helper function
            //int vertShaderId, fragShaderId;
            //LoadShader("vs.glsl", ShaderType.VertexShader, _programID, out vertShaderId);
            //LoadShader("fs.glsl", ShaderType.FragmentShader, _programID, out fragShaderId);

            ////Deincriment the reference count on the shaders so that they don't exist until the context is destroyed.
            ////(Housekeeping really)
            //GL.DeleteShader(vertShaderId);
            //GL.DeleteShader(fragShaderId);

            ////This specifically tells OpenGL that we want to be able to refer to the "vertexPos" variable inside of the vs.glsl 
            ////This allows us to later refer to it by specicic number.
            //GL.BindAttribLocation(_programID, (int)ShaderAttributeIds.Position, "vertexPos");

            ////Linking the shader tells OpenGL to finish compiling it or something. It's required. :P
            //GL.LinkProgram(_programID);

            ////Now that the program is linked we can get the identifier/location of the uniforms (by id) within the shader.
            //_uniformMVP = GL.GetUniformLocation(_programID, "modelview");
            //_uniformColor = GL.GetUniformLocation(_programID, "outputColor");

            ////More error checking
            //if (GL.GetError() != ErrorCode.NoError)
            //    Console.WriteLine(GL.GetProgramInfoLog(_programID));

            //CreateTimer();

            /* This stuff is done *once* per program load */

            //Generate a Program ID
            _programID = GL.CreateProgram();

            Cam = new Camera();

            //Create the Vertex and Fragment shader from file using our helper function
            int vertShaderId, fragShaderId;
            LoadShader("vs.glsl", ShaderType.VertexShader, _programID, out vertShaderId);
            LoadShader("fs.glsl", ShaderType.FragmentShader, _programID, out fragShaderId);

            //Deincriment the reference count on the shaders so that they don't exist until the context is destroyed.
            //(Housekeeping really)
            GL.DeleteShader(vertShaderId);
            GL.DeleteShader(fragShaderId);

            //This specifically tells OpenGL that we want to be able to refer to the "vertexPos" variable inside of the vs.glsl 
            //This allows us to later refer to it by specicic number.
            GL.BindAttribLocation(_programID, (int)ShaderAttributeIds.Position, "vertexPos");

            //Linking the shader tells OpenGL to finish compiling it or something. It's required. :P
            GL.LinkProgram(_programID);

            //Now that the program is linked we can get the identifier/location of the uniforms (by id) within the shader.
            _uniformMVP = GL.GetUniformLocation(_programID, "modelview");
            _uniformColor = GL.GetUniformLocation(_programID, "col");

            //More error checking
            if (GL.GetError() != ErrorCode.NoError)
                Console.WriteLine(GL.GetProgramInfoLog(_programID));

            //This just hooks winforms to draw our control.
            CreateTimer();
        }

        private void CreateTimer()
        {
            Stop = new Stopwatch();

            time = new Timer();

            time.Interval = 16;

            time.Tick += (o, args) =>
            {
                Stop.Start();

                Input.Internal_SetMousePos(new Vector2(Cursor.Position.X, Cursor.Position.Y));

                Input.Internal_UpdateInputState();

                Cam.Update();

                Draw();

                Stop.Stop();

                //deltaTime = (int)Stop.ElapsedMilliseconds;

            };

            time.Enabled = true;
        }

        #endregion

        #region Input/Output

        public void LoadFromArc(string fileName)
        {
            RARC arc = new RARC(fileName);

            for (int i = 0; i < arc.Nodes.Count(); i++)
            {
                for (int j = 0; j < arc.Nodes[i].Entries.Count(); j++)
                {
                    if (arc.Nodes[i].Entries[j].Name.Contains(".dzr"))
                    {
                        EndianBinaryReader reader = new EndianBinaryReader(arc.Nodes[i].Entries[j].Data, Endian.Big);

                        Read(reader);

                        IsListLoaded = true;
                    }
                }
            }
        }

        public void Read(EndianBinaryReader reader)
        {
            Cam = new Camera();

            ChunkTemplates = LoadTemplates();

            Chunks.Clear();

            int numChunkHeaders = reader.ReadInt32();

            int readerOffsetStorage = 4;

            for (int i = 0; i < numChunkHeaders; i++)
            {
                string chunkName = reader.ReadStringUntil('\0');

                reader.BaseStream.Position -= 1;

                int numChunks = reader.ReadInt32();

                int chunksOffset = reader.ReadInt32();

                readerOffsetStorage = (int)reader.BaseStream.Position;

                reader.BaseStream.Position = chunksOffset;

                for (int j = 0; j < numChunks; j++)
                {
                    Chunk newChunk = new Chunk();

                    if (chunkName == "RTBL")
                    {
                        EntityTemplate template = ChunkTemplates.Find(x => x.ChunkID == "RTBL");

                        newChunk = template.ProcessRTBL(reader, numChunks);

                        Chunks.Add(newChunk);

                        continue;
                    }

                    newChunk.Read(reader, chunkName, ChunkTemplates);

                    Chunks.Add(newChunk);
                }

                reader.BaseStream.Position = readerOffsetStorage;
            }

            UpdateTreeView();

            MainForm.ElementView.SelectedNode = MainForm.ElementView.Nodes[0];

            SelectedChunk = Chunks[0];
        }

        void Write(EndianBinaryWriter writer)
        {
            IEnumerable<IGrouping<string, Chunk>> query = Chunks.GroupBy(x => x.ChunkType, x => x);

            int numUniqueChunks = (int)query.Count<IGrouping<string, Chunk>>();

            writer.Write(numUniqueChunks);

            foreach (IGrouping<string, Chunk> chunk in query)
            {
                writer.WriteFixedString(chunk.Key, 4);

                writer.Write((int)chunk.Count());

                writer.Write((int)0);
            }

            for (int i = 0; i < numUniqueChunks; i++)
            {
                int offsetFieldOffset = (4 * (1 + i)) + (8 * (1 + i));

                int currentWriterOffset = (int)writer.BaseStream.Position;

                writer.BaseStream.Position = offsetFieldOffset;

                writer.Write(currentWriterOffset);

                writer.BaseStream.Position = currentWriterOffset;

                foreach (Chunk chun in query.ElementAt(i))
                {
                    string chunkName = chun.ChunkType.Remove(chun.ChunkType.Length - 1);
                    chun.Write(writer, ChunkTemplates.Find(x => x.ChunkID.Contains(chunkName)));
                }
            }
        }

        #endregion

        #region Treeview Manipulation

        void UpdateTreeView()
        {
            MainForm.ElementView.SuspendLayout();

            MainForm.ElementView.Nodes.Clear();

            IEnumerable<IGrouping<string, Chunk>> query = GroupChunksByFourCc();

            foreach (IGrouping<string, Chunk> chunk in query)
            {
                TreeNode chunkNode = new TreeNode(chunk.Key);

                chunkNode.Tag = chunk.Key;

                foreach (Chunk chun in chunk)
                {
                    TreeNode elemNode = new TreeNode();

                    elemNode.Text = chun.DisplayName;

                    elemNode.Tag = chunk.Key;

                    chunkNode.Nodes.Add(elemNode);
                }

                MainForm.ElementView.Nodes.Add(chunkNode);

                MainForm.ElementView.ResumeLayout();
            }
        }

        void AddToChunkNode(TreeNode parent, Chunk chunk)
        {
            TreeNode node = new TreeNode(chunk.DisplayName);

            node.Tag = chunk.ChunkType;

            parent.Nodes.Add(node);
        }

        void DeleteFromChunkNode(TreeNode parent, int index)
        {
            parent.Nodes.RemoveAt(index);

            if (parent.Nodes.Count == 0)
                MainForm.ElementView.Nodes.Remove(parent);
        }

        #endregion

        private void Draw()
        {
            //This is called *every* frame. Every time we want to draw, we do the following.
            GL.ClearColor(new Color4(.36f, .25f, .94f, 1f));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            //Tell OpenGL which program to use (our Vert Shader (VS) and Frag Shader (FS))
            GL.UseProgram(_programID);

            //Enable depth-testing which keeps models from rendering inside out.
            GL.Enable(EnableCap.DepthTest);

            //Clear any previously bound buffer so we have no leftover data or anything.
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

            /* Anything below this point would technically be done per object you draw */

            int width, height;

            if (MainForm.Viewport.Width == 0)
                width = 1;

            else
                width = MainForm.Viewport.Width;

            if (MainForm.Viewport.Height == 0)
                height = 1;

            else
                height = MainForm.Viewport.Height;

            ViewMatrix = Cam.ViewMatrix;
            ProjMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(65), width / height, 100, 500000);

            foreach (Chunk chun in Chunks)
            {
                if (chun.Geometry != GeometryType.None)
                {
                    GL.Enable(EnableCap.PolygonOffsetFill);
                    GL.PolygonOffset(1.0f, 1.0f);
                    chun.Render(MainForm.Viewport, _uniformMVP, _uniformColor, ViewMatrix, ProjMatrix);
                    GL.Disable(EnableCap.PolygonOffsetFill);

                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                    chun.DisplayColor = Color.Black;
                    chun.Render(MainForm.Viewport, _uniformMVP, _uniformColor, ViewMatrix, ProjMatrix);

                    if (chun == SelectedChunk)
                        chun.DisplayColor = Color.Red;

                    else
                        chun.ResetDisplayColor();
                    GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                    //chun.Render(MainForm.Viewport, _uniformMVP, _uniformColor, ViewMatrix, ProjMatrix);
                }
            }

            ////Build a Model View Projection Matrix. This is where you would add camera movement (modifiying the View matrix), Perspective rendering (perspective matrix) and model position/scale/rotation (Model)
            //Matrix4 viewMatrix = Matrix4.LookAt(new Vector3(25, 15, 25), Vector3.Zero, Vector3.UnitY);
            //Matrix4 projMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(65), Viewport.Width / (float)Viewport.Height, 10, 1000);
            //Matrix4 modelMatrix = Matrix4.Identity; //Identity = doesn't change anything when multiplied.

            ////Bind the buffers that have the data you want to use
            //GL.BindBuffer(BufferTarget.ArrayBuffer, _glVbo);
            //GL.BindBuffer(BufferTarget.ElementArrayBuffer, _glEbo);

            ////Then, you have to tell the GPU what the contents of the Array buffer look like. Ie: Is each entry just a position, or does it have a position, color, normal, etc.
            //GL.EnableVertexAttribArray((int)ShaderAttributeIds.Position);
            //GL.VertexAttribPointer((int)ShaderAttributeIds.Position, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);

            ////Upload the WVP to the GPU
            //Matrix4 finalMatrix = modelMatrix * viewMatrix * projMatrix;
            //GL.UniformMatrix4(_uniformMVP, false, ref finalMatrix);

            ////Now we tell the GPU to actually draw the data we have
            //GL.DrawElements(BeginMode.Triangles, 36, DrawElementsType.UnsignedInt, 0);

            ////This is cleanup to undo the changes to the OpenGL state we did to draw this model.
            //GL.DisableVertexAttribArray((int)ShaderAttributeIds.Position);

            /* This anything below is done at the end of the frame, only once */
            MainForm.Viewport.SwapBuffers();
        }

        #region Chunk List Manipulation

        public void AddChunkElement()
        {
            string chunkType = (string)CurSelectedNode.Tag;

            string templateSearchString = chunkType.Remove(chunkType.Length - 1);

            EntityTemplate template = ChunkTemplates.Find(x => x.ChunkID.Contains(templateSearchString));

            Chunk newChunk = new Chunk();

            newChunk.MakeEmptyChunkFromTemplate(template);

            Chunks.Add(newChunk);

            TreeNode parentNode;

            if (CurSelectedNode.Parent == null)
                parentNode = CurSelectedNode;

            else
                parentNode = CurSelectedNode.Parent;

            AddToChunkNode(parentNode, newChunk);
        }

        public void DeleteChunkElement()
        {
            IEnumerable<IGrouping<string, Chunk>> query = Chunks.GroupBy(x => x.ChunkType, x => x);

            IGrouping<string, Chunk> selectedChunk = query.First(x => x.Key == (string)CurSelectedNode.Tag);

            List<Chunk> tempList = selectedChunk.ToList();

            int nodeIndex = CurSelectedNode.Index;

            Chunks.Remove(tempList[nodeIndex]);

            TreeNode parentNode;

            if (CurSelectedNode.Parent == null)
                parentNode = CurSelectedNode;

            else
                parentNode = CurSelectedNode.Parent;

            DeleteFromChunkNode(parentNode, nodeIndex);
        }

        public void CopyChunkElement()
        {
            IEnumerable<IGrouping<string, Chunk>> query = Chunks.GroupBy(x => x.ChunkType, x => x);

            IGrouping<string, Chunk> selectedChunk = query.First(x => x.Key == (string)CurSelectedNode.Tag);

            List<Chunk> tempList = selectedChunk.ToList();

            int nodeIndex = CurSelectedNode.Index;

            Chunk copiedChunk = tempList[nodeIndex];

            Clipboard.SetData("DZRS", copiedChunk);
        }

        public void PasteChunkElement()
        {
            if (Clipboard.ContainsData("DZRS"))
            {
                Chunks.Add((Chunk)Clipboard.GetData("DZRS"));

                TreeNode parentNode;

                if (CurSelectedNode.Parent == null)
                    parentNode = CurSelectedNode;

                else
                    parentNode = CurSelectedNode.Parent;

                AddToChunkNode(parentNode, (Chunk)Clipboard.GetData("DZRS"));
            }

            else
            {
                MessageBox.Show("There is nothing to paste.");
            }
        }

        public void AddChunk(string chunkType)
        {
            var query = GroupChunksByFourCc();

            foreach (var group in query)
            {
                if (group.Key == chunkType)
                {
                    MessageBox.Show("The chunk type you selected already exists.", "Chunk Type Already Exists");

                    return;
                }
            }

            Chunk newChunk = new Chunk();

            newChunk.MakeEmptyChunkFromTemplate(ChunkTemplates.Find(x => x.ChunkID == chunkType));

            Chunks.Add(newChunk);

            UpdateTreeView();
        }

        public void DeleteChunk()
        {
            IEnumerable<IGrouping<string, Chunk>> query = Chunks.GroupBy(x => x.ChunkType, x => x);

            IGrouping<string, Chunk> selectedChunk = query.First(x => x.Key == (string)CurSelectedNode.Tag);

            foreach (Chunk chunk in selectedChunk)
            {
                Chunks.Remove(chunk);
            }

            UpdateTreeView();
        }

        #endregion

        #region Variable Manipulation

        public void ChangeSelectionFromChunkElement(Chunk chun)
        {
            ResetChunkColor();

            SelectedChunk = chun;

            SelectedChunk.DisplayColor = Color.Red;

            IEnumerable<IGrouping<string, Chunk>> query = GroupChunksByFourCc();

            int parentNodeIndex = 0;

            List<Chunk> chunkList = new List<Chunk>();

            foreach (IGrouping<string, Chunk> group in query)
            {
                if (group.Key == SelectedChunk.ChunkType)
                {
                    chunkList = group.ToList();
                    break;
                }

                else
                    parentNodeIndex += 1;
            }

            int elementNodeIndex = chunkList.IndexOf(chun);

            TreeNode elementNode = MainForm.ElementView.Nodes[parentNodeIndex].Nodes[elementNodeIndex];

            MainForm.ElementView.SelectedNode = elementNode;
        }

        public void ChangeSelectionFromTreeNode(TreeNode node)
        {
            CurSelectedNode = node;

            if (node.Parent != null)
            {
                IEnumerable<IGrouping<string, Chunk>> query = GroupChunksByFourCc();

                foreach (IGrouping<string, Chunk> group in query)
                {
                    if (group.Key == (string)node.Tag)
                    {
                        List<Chunk> chunkList = group.ToList();

                        ResetChunkColor();

                        SelectedChunk = chunkList[node.Index];

                        SelectedChunk.DisplayColor = Color.Red;
                    }
                }
            }
        }

        #endregion

        public void ResetChunkColor()
        {
            if (SelectedChunk != null)
                SelectedChunk.ResetDisplayColor();
        }

        public void SetChunkColor(Color4 color)
        {
            if (CurSelectedNode.Parent != null)
                SelectedChunk.DisplayColor = color;
        }

        public void CalculateRay(Point mouseLoc)
        {
            float x = (2.0f * mouseLoc.X) / MainForm.Viewport.ClientRectangle.Width - 1.0f;
            float y = 1.0f - (2.0f * mouseLoc.Y) / MainForm.Viewport.ClientRectangle.Height;
            float z = 1.0f;

            Vector3 ndsRay = new Vector3(x, y, z);

            Vector4 clipRay = new Vector4(ndsRay.X, ndsRay.Y, -1.0f, 1.0f);

            Vector4 eyeRay = Vector4.Transform(clipRay, Matrix4.Invert(ProjMatrix));

            eyeRay = new Vector4(eyeRay.X, eyeRay.Y, -1.0f, 0.0f);

            Vector3 unNormalizedRay = new Vector3(Vector4.Transform(eyeRay, Matrix4.Invert(ViewMatrix)).Xyz);

            Vector3 normalizedRay = Vector3.Normalize(unNormalizedRay);;

            bool isSelected = false;

            List<Chunk> matchList = new List<Chunk>();

            foreach (Chunk chun in Chunks)
            {
                isSelected = chun.CheckRay(Cam.EyePos, normalizedRay);

                if (isSelected)
                    matchList.Add(chun);
            }

            float closest = float.MaxValue;

            Chunk winningChunk = null;

            foreach (Chunk chun in matchList)
            {
                float dist = (chun.Position - Cam.EyePos).Length;

                if (dist < closest)
                {
                    winningChunk = chun;

                    closest = dist;
                }
            }

            if (winningChunk != null)
                ChangeSelectionFromChunkElement(winningChunk);
        }

        public void BeforeSelectChunkElement()
        {
            ResetChunkColor();
        }

        public IEnumerable<IGrouping<string, Chunk>> GroupChunksByFourCc()
        {
            IEnumerable<IGrouping<string, Chunk>> query = Chunks.GroupBy(x => x.ChunkType, x => x);

            return query;
        }
    }

    [Serializable]
    public class Chunk
    {
        public string ChunkType;

        public string DisplayName;

        public GeometryType Geometry;

        Color4 ActualColor;

        public Color4 DisplayColor;

        public List<ElementProperty> Fields;

        public Vector3 Position;

        Vector3[] Vertexes;

        int[] Meshes;

        int _glVbo;

        int _glEbo;

        int _glNbo;

        public float RayResult;

        public Chunk()
        {
            Fields = new List<ElementProperty>();

            ChunkType = "NULL";

            DisplayName = "default";

            Geometry = GeometryType.Cube;

            DisplayColor = Color4.Black;
        }

        public void Read(EndianBinaryReader reader, string type, List<EntityTemplate> templates)
        {
            ChunkType = type;

            DisplayName = ChunkType;

            Fields = new List<ElementProperty>();

            string searchID = ChunkType.Remove(ChunkType.Length - 1);

            EntityTemplate template = templates.Find(x => x.ChunkID.Contains(searchID));

            Geometry = template.Geometry;

            ActualColor = template.Color;

            DisplayColor = ActualColor;

            foreach (ElementProperty prop in template.Properties)
            {
                switch (prop.Type)
                {
                    case FieldType.Byte:
                        prop.Data = reader.ReadByte();
                        break;
                    case FieldType.Short:
                        prop.Data = reader.ReadInt16();
                        break;
                    case FieldType.Integer:
                        prop.Data = reader.ReadInt32();
                        break;
                    case FieldType.Float:
                        prop.Data = reader.ReadSingle();
                        break;
                    case FieldType.Vector2:
                        prop.Data = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                        break;
                    case FieldType.Vector3:
                        prop.Data = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        break;
                    case FieldType.String:
                        prop.Data = reader.ReadString((uint)prop.Length).Trim('\0');
                        DisplayName = (string)prop.Data;
                        break;
                    case FieldType.ListByte:
                        List<byte> byteList = new List<byte>();

                        for (int i = 0; i < prop.Length; i++)
                        {
                            byteList.Add(reader.ReadByte());
                        }

                        prop.Data = byteList;
                        break;
                }
            }

            template.ReadSpecialProcess();

            foreach (ElementProperty prop in template.Properties)
            {
                ElementProperty actualProp = prop.Copy();

                Fields.Add(actualProp);
            }

            switch (Geometry)
            {
                case GeometryType.Cube:
                    GenerateCube();
                    break;
                case GeometryType.Sphere:
                    GenerateSphere(250);
                    break;
                case GeometryType.None:
                    Vertexes = new Vector3[1];
                    Meshes = new int[1];
                    return;
            }
        }

        public void Write(EndianBinaryWriter writer, EntityTemplate template)
        {
            if (template.BitField.Count > 0)
            {
                ProcessBitFields(template);
            }

            foreach (ElementProperty prop in Fields)
            {
                switch (prop.Type)
                {
                    case FieldType.Byte:
                        writer.Write((byte)prop.Data);
                        break;
                    case FieldType.Short:
                        writer.Write((short)prop.Data);
                        break;
                    case FieldType.Integer:
                        writer.Write((int)prop.Data);
                        break;
                    case FieldType.Float:
                        writer.Write((float)prop.Data);
                        break;
                    case FieldType.Vector2:
                        Vector2 vec2 = (Vector2)prop.Data;
                        writer.Write((float)vec2.X);
                        writer.Write((float)vec2.Y);
                        break;
                    case FieldType.Vector3:
                        Vector3 vec3 = (Vector3)prop.Data;
                        writer.Write((float)vec3.X);
                        writer.Write((float)vec3.Y);
                        writer.Write((float)vec3.Z);
                        break;
                    case FieldType.String:
                        string name = (string)prop.Data;

                        writer.WriteFixedString(name, 8);

                        for (int i = 0; i < prop.Length - name.Length; i++)
                        {
                            writer.Write((byte)0);
                        }
                        break;
                    case FieldType.ListByte:
                        List<byte> byteList = (List<byte>)prop.Data;

                        foreach (byte by in byteList)
                        {
                            writer.Write(by);
                        }
                        break;
                }
            }
        }

        public Chunk Copy()
        {
            Chunk copiedChunk = new Chunk();

            foreach (ElementProperty prop in Fields)
            {
                ElementProperty copiedProp = prop.Copy();

                copiedChunk.Fields.Add(copiedProp);
            }

            return copiedChunk;
        }

        public void MakeEmptyChunkFromTemplate(EntityTemplate template)
        {
            ChunkType = template.ChunkID;

            DisplayName = "default";

            Geometry = template.Geometry;

            ActualColor = template.Color;

            DisplayColor = ActualColor;

            switch (Geometry)
            {
                case GeometryType.Cube:
                    GenerateCube();
                    break;
                case GeometryType.Sphere:
                    GenerateSphere(25);
                    break;
                case GeometryType.None:
                    Vertexes = new Vector3[1];
                    Meshes = new int[1];
                    break;
            }

            foreach (ElementProperty prop in template.Properties)
            {
                switch (prop.Type)
                {
                    case FieldType.Byte:
                    case FieldType.Short:
                    case FieldType.Integer:
                    case FieldType.Float:
                        prop.Data = 0;
                        break;
                    case FieldType.Vector2:
                        prop.Data = new Vector2();
                        break;
                    case FieldType.Vector3:
                        prop.Data = new Vector3();
                        break;
                    case FieldType.String:
                        prop.Data = "default";
                        break;
                    case FieldType.ListByte:
                        List<byte> byteList = new List<byte>();
                        prop.Data = byteList;
                        break;
                }

                Fields.Add(prop.Copy());
            }
        }

        public void AddField(ElementProperty prop)
        {
            Fields.Add(prop);
        }

        void ProcessBitFields(EntityTemplate template)
        {
            int bitField = 0;

            foreach (BitFieldObject bitOb in template.BitField)
            {
                ElementProperty prop = Fields.Find(x => x.Name == bitOb.Name);

                int shiftedPropVal = (int)prop.Data << bitOb.BitShift;

                bitField = shiftedPropVal | bitField;
            }

            ElementProperty bitFieldProp = new ElementProperty();

            bitFieldProp.MakeProperty("Bit Field", FieldType.Integer, bitField);

            Fields.Insert(Fields.IndexOf(Fields.Find(x => x.Name == template.BitField[0].Name)), bitFieldProp);

            foreach (BitFieldObject bitOb in template.BitField)
            {
                ElementProperty prop = Fields.Find(x => x.Name == bitOb.Name);

                Fields.Remove(prop);
            }
        }

        public void Render(GLControl Viewport, int _uniformMVP, int _uniformColor, Matrix4 viewMatrix, Matrix4 projMatrix)
        {
            if (Geometry == GeometryType.None)
                return;

            //Build a Model View Projection Matrix. This is where you would add camera movement (modifiying the View matrix), Perspective rendering (perspective matrix) and model position/scale/rotation (Model)
            Matrix4 modelMatrix = Matrix4.CreateTranslation((Vector3)Fields.Find(x => x.Name == "Position").Data) * Matrix4.Rotate(Quaternion.Identity)* Matrix4.Scale(1);

            //Bind the buffers that have the data you want to use
            GL.BindBuffer(BufferTarget.ArrayBuffer, _glVbo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _glEbo);

            //Then, you have to tell the GPU what the contents of the Array buffer look like. Ie: Is each entry just a position, or does it have a position, color, normal, etc.
            GL.EnableVertexAttribArray((int)ShaderAttributeIds.Position);
            GL.VertexAttribPointer((int)ShaderAttributeIds.Position, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);

            //Upload the WVP to the GPU
            Matrix4 finalMatrix = modelMatrix * viewMatrix * projMatrix;
            GL.Uniform4(_uniformColor, DisplayColor);
            GL.UniformMatrix4(_uniformMVP, false, ref finalMatrix);

            //Now we tell the GPU to actually draw the data we have
            GL.DrawElements(BeginMode.Triangles, Meshes.Count(), DrawElementsType.UnsignedInt, 0);

            //This is cleanup to undo the changes to the OpenGL state we did to draw this model.
            GL.DisableVertexAttribArray((int)ShaderAttributeIds.Position);
        }

        void GenerateCube()
        {
            //This is our vertex data, just positions
            Vertexes = new Vector3[]
            { 
                new Vector3(-25f, -25f,  -25f),
                new Vector3(25f, -25f,  -25f),
                new Vector3(25f, 25f,  -25f),
                new Vector3(-25f, 25f,  -25f),
                new Vector3(-25f, -25f,  25f),
                new Vector3(25f, -25f,  25f),
                new Vector3(25f, 25f,  25f),
                new Vector3(-25f, 25f,  25f),
            };

            //These are indexes (like the collision mesh uses)
            Meshes = new int[]
            {
                //front
                0, 7, 3,
                0, 4, 7,
                //back
                1, 2, 6,
                6, 5, 1,
                //left
                0, 2, 1,
                0, 3, 2,
                //right
                4, 5, 6,
                6, 7, 4,
                //top
                2, 3, 6,
                6, 3, 7,
                //bottom
                0, 1, 5,
                0, 5, 4
            };

            //Generate a buffer on the GPU and get the ID to it
            GL.GenBuffers(1, out _glVbo);

            //This "binds" the buffer. Once a buffer is bound, all actions are relative to it until another buffer is bound.
            GL.BindBuffer(BufferTarget.ArrayBuffer, _glVbo);

            //This uploads data to the currently bound buffer from the CPU -> GPU. This only needs to be done with the data changes (ie: you edited a vertexes position on the cpu side)
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vertexes.Length * Vector3.SizeInBytes), Vertexes,
                BufferUsageHint.StaticDraw);

            //Now we're going to repeat the same process for the Element buffer, which is what OpenGL calls indicies. (Notice how it's basically identical?)
            GL.GenBuffers(1, out _glEbo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _glEbo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(Meshes.Length * 4), Meshes,
                BufferUsageHint.StaticDraw);
        }

        void GenerateSphere(int size)
        {
            float t = (float)((1.0f + Math.Sqrt(5.0)) / 2.0);

            Vertexes = new Vector3[]
            {
                new Vector3(-1.0f, t, 0.0f), new Vector3(1.0f, t, 0.0f), new Vector3(-1.0f, -t, 0.0f), new Vector3(1.0f, -t, 0.0f),
                new Vector3(0.0f, -1, t), new Vector3(0.0f, 1, t), new Vector3(0.0f, -1, -t), new Vector3(0.0f, 1, -t),
                new Vector3(t, 0.0f, -1.0f), new Vector3(t, 0.0f, 1.0f), new Vector3(-t, 0.0f, -1.0f), new Vector3(-t, 0.0f, 1.0f)
            };

            
            for (int i = 0; i < Vertexes.Count(); i++)
            {
                Vector3 vec = Vertexes[i];

                float length = (float)Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);

                Vertexes[i] = Vector3.Normalize(vec) * 50f;
            }
            

            Meshes = new int[]
            {
                0, 11, 5,
                0, 5, 1,
                0, 1, 7,
                0, 7, 10,
                0, 10, 11,

                4, 9, 5,
                2, 4, 11,
                6, 2, 10,
                8, 6, 7,
                9, 8, 1,

                3, 8, 9,
                3, 9, 4,
                3, 4, 2,
                3, 2, 6,
                3, 6, 8,

                1, 5, 9,
                5, 11, 4,
                11, 10, 2,
                10, 7, 6,
                7, 1, 8
                
            };

            GL.GenBuffers(1, out _glVbo);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _glVbo);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vertexes.Length * Vector3.SizeInBytes), Vertexes, BufferUsageHint.StaticDraw);

            GL.GenBuffers(1, out _glEbo);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _glEbo);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Meshes.Length * 4), Meshes, BufferUsageHint.StaticDraw);
        }

        public void ResetDisplayColor()
        {
            DisplayColor = ActualColor;
        }

        public bool CheckRay(Vector3 eye, Vector3 ray)
        {
            bool isSelected = false;

            if (Geometry != GeometryType.None)
            {
                Vector3 position = (Vector3)Fields.Find(x => x.Name == "Position").Data;

                Position = position;

                float b = Vector3.Dot(ray, (eye - position));

                float c = Vector3.Dot((eye - position), (eye - position));

                c = c - 2000;

                float a = (b * b) - c;

                if (a >= 0)
                    isSelected = true;
            }

            return isSelected;
        }

        public void MakeDebugChunk(Vector3 ray)
        {
            GenerateCube();

            DisplayColor = Color.Bisque;

            ElementProperty prop = new ElementProperty();

            prop.MakeProperty("Position", FieldType.Vector3, ray);

            Fields.Add(prop);
        }
    }

    public class EntityTemplate
    {
        public string ChunkID;

        public GeometryType Geometry;

        public System.Drawing.Color Color;

        public List<ElementProperty> Properties = new List<ElementProperty>();

        public List<BitFieldObject> BitField = new List<BitFieldObject>();

        public void ReadSpecialProcess()
        {
            if (BitField.Count > 0)
            {
                int fieldValue = (int)Properties.Find(x => x.Name == "Bit Field").Data;

                for (int i = 0; i < BitField.Count; i++)
                {
                    ElementProperty prop = new ElementProperty();

                    int propValue = (fieldValue & BitField[i].Mask) >> BitField[i].BitShift;

                    prop.MakeProperty(BitField[i].Name, FieldType.Integer, propValue);

                    Properties.Insert(Properties.IndexOf(Properties.Find(x => x.Name == "Bit Field")), prop);
                }

                Properties.Remove(Properties.Find(y => y.Name == "Bit Field"));
            }
        }

        public Chunk ProcessRTBL(EndianBinaryReader reader, int numChunks)
        {
            Chunk rtblChunk = new Chunk();

            int elementOffset = reader.ReadInt32();

            int offsetStorage = (int)reader.BaseStream.Position;

            reader.BaseStream.Position = elementOffset;

            ElementProperty roomNumProp = new ElementProperty();

            byte roomNum = reader.ReadByte();

            roomNumProp.MakeProperty("Number of Rooms", FieldType.Byte, roomNum);

            rtblChunk.AddField(roomNumProp);

            ElementProperty isTimePassProp = new ElementProperty();

            short isTimePass = reader.ReadInt16();

            isTimePassProp.MakeProperty("Does time pass?", FieldType.Short, isTimePass);

            rtblChunk.AddField(isTimePassProp);

            ElementProperty unknownProp = new ElementProperty();

            unknownProp.MakeProperty("Unknown1", FieldType.Byte, reader.ReadByte());

            rtblChunk.AddField(unknownProp);

            int roomTableOffset = reader.ReadInt32();

            reader.BaseStream.Position = roomTableOffset;

            List<byte> roomList = new List<byte>();

            for (int i = 0; i < roomNum; i++)
            {
                roomList.Add(reader.ReadByte());
            }

            ElementProperty roomListProp = new ElementProperty();

            roomListProp.MakeProperty("Room List", FieldType.ListByte, roomList);

            rtblChunk.AddField(roomListProp);

            reader.BaseStream.Position = offsetStorage;

            return rtblChunk;
        }
    }

    [Serializable]
    public class ElementProperty
    {
        public string Name;

        public int Length;

        public FieldType Type;

        public object Data;

        public void MakeProperty(string name, FieldType type, object data)
        {
            Name = name;

            Type = type;

            Data = data;
        }

        public ElementProperty Copy()
        {
            ElementProperty copiedProp = new ElementProperty();

            copiedProp.Name = Name;

            copiedProp.Length = Length;

            copiedProp.Type = Type;

            copiedProp.Data = Data;

            return copiedProp;
        }
    }

    public class BitFieldObject
    {
        public string Name;

        public int Mask;

        public int BitShift;
    }

    public static class Vec3
    {
        #region Project
        /// <summary>
        /// Projects a vector from object space into screen space.
        /// </summary>
        /// <param name="vector">The vector to project.</param>
        /// <param name="x">The X coordinate of the viewport.</param>
        /// <param name="y">The Y coordinate of the viewport.</param>
        /// <param name="width">The width of the viewport.</param>
        /// <param name="height">The height of the viewport.</param>
        /// <param name="minZ">The minimum depth of the viewport.</param>
        /// <param name="maxZ">The maximum depth of the viewport.</param>
        /// <param name="worldViewProjection">The world-view-projection matrix.</param>
        /// <returns>The vector in screen space.</returns>
        /// <remarks>
        /// To project to normalized device coordinates (NDC) use the following parameters:
        /// Project(vector, -1, -1, 2, 2, -1, 1, worldViewProjection).
        /// </remarks>
        public static Vector3 Project(Vector3 vector, float x, float y, float width, float height, float minZ, float maxZ, Matrix4 worldViewProjection)
        {
            Vector4 result;

            result.X = 
                vector.X * worldViewProjection.M11 + 
                vector.Y * worldViewProjection.M21 + 
                vector.Z * worldViewProjection.M31 + 
                worldViewProjection.M41;

            result.Y =
                vector.X * worldViewProjection.M12 +
                vector.Y * worldViewProjection.M22 +
                vector.Z * worldViewProjection.M32 +
                worldViewProjection.M42;

            result.Z =
                vector.X * worldViewProjection.M13 +
                vector.Y * worldViewProjection.M23 +
                vector.Z * worldViewProjection.M33 +
                worldViewProjection.M43;

            result.W =
                vector.X * worldViewProjection.M14 +
                vector.Y * worldViewProjection.M24 +
                vector.Z * worldViewProjection.M34 +
                worldViewProjection.M44;

            result /= result.W;

            result.X = x + (width * ((result.X + 1.0f) / 2.0f));
            result.Y = y + (height * ((result.Y + 1.0f) / 2.0f));
            result.Z = minZ + ((maxZ - minZ) * ((result.Z + 1.0f) / 2.0f));

            return new Vector3(result.X, result.Y, result.Z);
        }

        #endregion

        #region Unproject

        /// <summary>
        /// Projects a vector from screen space into object space.
        /// </summary>
        /// <param name="vector">The vector to project.</param>
        /// <param name="x">The X coordinate of the viewport.</param>
        /// <param name="y">The Y coordinate of the viewport.</param>
        /// <param name="width">The width of the viewport.</param>
        /// <param name="height">The height of the viewport.</param>
        /// <param name="minZ">The minimum depth of the viewport.</param>
        /// <param name="maxZ">The maximum depth of the viewport.</param>
        /// <param name="worldViewProjection">The inverse of the world-view-projection matrix.</param>
        /// <returns>The vector in object space.</returns>
        /// <remarks>
        /// To project from normalized device coordinates (NDC) use the following parameters:
        /// Project(vector, -1, -1, 2, 2, -1, 1, inverseWorldViewProjection).
        /// </remarks>
        public static Vector3 Unproject(Vector3 vector, float x, float y, float width, float height, float minZ, float maxZ, Matrix4 inverseWorldViewProjection)
        {
            Vector4 result;

            result.X = ((((vector.X - x) / width) * 2.0f) - 1.0f);
            result.Y = ((((vector.Y - y) / height) * 2.0f) - 1.0f);
            result.Z = (((vector.Z / (maxZ - minZ)) * 2.0f) - 1.0f);

            result.X =
                result.X * inverseWorldViewProjection.M11 +
                result.Y * inverseWorldViewProjection.M21 +
                result.Z * inverseWorldViewProjection.M31 +
                inverseWorldViewProjection.M41;

            result.Y =
                result.X * inverseWorldViewProjection.M12 +
                result.Y * inverseWorldViewProjection.M22 +
                result.Z * inverseWorldViewProjection.M32 +
                inverseWorldViewProjection.M42;

            result.Z =
                result.X * inverseWorldViewProjection.M13 +
                result.Y * inverseWorldViewProjection.M23 +
                result.Z * inverseWorldViewProjection.M33 +
                inverseWorldViewProjection.M43;

            result.W =
                result.X * inverseWorldViewProjection.M14 +
                result.Y * inverseWorldViewProjection.M24 +
                result.Z * inverseWorldViewProjection.M34 +
                inverseWorldViewProjection.M44;

            result /= result.W;

            return new Vector3(result.X, result.Y, result.Z);
        }

        #endregion
    }
}
