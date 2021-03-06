﻿using System;
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
        ListShort,
        ListRPPN,
        Color,
        ColorAlpha,
        Vector2,
        Vector3
    }

    public enum GeometryType
    {
        None,
        Cube,
        Sphere,
        Pyramid,
        LineStrip,
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

        List<ControlObject> Controls;

        Chunk SelectedChunk;

        ControlObject CurrentControl;

        Timer time;

        MainUI MainForm;

        Camera Cam;

        Matrix4 ViewMatrix;

        Matrix4 ProjMatrix;

        TreeNode CurSelectedNode;

        CollisionMesh Collision;

        List<FileData> FilesFromArc;

        public bool IsListLoaded = false;

        int _programID;

        int _uniformMVP;

        int _uniformColor;

        #endregion

        #region Init

        public Worker(MainUI mainForm)
        {
            Chunks = new List<Chunk>();

            Controls = new List<ControlObject>();

            MainForm = mainForm;
        }

        private List<EntityTemplate> LoadTemplates()
        {
            string folderPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "/Templates/";

            DirectoryInfo dI = new DirectoryInfo(folderPath);

            List<EntityTemplate> itemTemplates = new List<EntityTemplate>();

            foreach (var file in dI.GetFiles())
            {
                if (file.Name.Contains(".json"))
                {
                    var template = JsonConvert.DeserializeObject<EntityTemplate>(File.ReadAllText(file.FullName));
                    itemTemplates.Add(template);
                }
            }

            return itemTemplates;
        }

        private List<ControlObject> LoadControls()
        {
            List<ControlObject> controlList = new List<ControlObject>();

            string lastChunkType = "NULL";

            foreach (Chunk chunk in Chunks)
            {
                if (chunk.ChunkType != lastChunkType)
                {
                    ControlObject obj = new ControlObject();

                    obj.Load(chunk);

                    controlList.Add(obj);

                    lastChunkType = chunk.ChunkType;
                }
            }

            return controlList;
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

            Console.WriteLine(_programID + Environment.NewLine);

            Cam = new Camera();

            //Create the Vertex and Fragment shader from file using our helper function
            int vertShaderId, fragShaderId;
            LoadShader("vs.glsl", ShaderType.VertexShader, _programID, out vertShaderId);
            LoadShader("fs.glsl", ShaderType.FragmentShader, _programID, out fragShaderId);

            Console.WriteLine(vertShaderId + Environment.NewLine);

            Console.WriteLine(fragShaderId + Environment.NewLine);

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

            Console.WriteLine(_uniformMVP + Environment.NewLine);

            Console.WriteLine(_uniformColor + Environment.NewLine);

            //More error checking
            if (GL.GetError() != ErrorCode.NoError)
                Console.WriteLine(GL.GetProgramInfoLog(_programID));

            //This just hooks winforms to draw our control.
            CreateTimer();

            Console.WriteLine("Timer Created" + Environment.NewLine);
        }

        private void CreateTimer()
        {
            time = new Timer();

            time.Interval = 16;

            time.Tick += (o, args) =>
            {
                Input.Internal_SetMousePos(new Vector2(Cursor.Position.X, Cursor.Position.Y));

                Input.Internal_UpdateInputState();

                Cam.Update();

                Draw();

                if ((CurrentControl != null) && (SelectedChunk != null))
                {
                    CurrentControl.SaveFields(SelectedChunk);
                }

            };

            time.Enabled = true;
        }

        #endregion

        #region Input/Output

        public void LoadFromArc(string fileName)
        {
            string tempFileName = "";

            using (FileStream yaz0TestStream = new FileStream(fileName, FileMode.Open))
            {

                EndianBinaryReader yaz0TestReader = new EndianBinaryReader(yaz0TestStream, Endian.Big);

                string yaz0Test = yaz0TestReader.ReadString(4);

                if (yaz0Test == "Yaz0")
                {
                    byte[] uncompressedArc = DecodeYaz0(yaz0TestReader);

                    yaz0TestReader.Close();

                    fileName = Path.GetTempFileName();

                    tempFileName = fileName;

                    FileInfo info = new FileInfo(fileName);

                    info.Attributes = FileAttributes.Temporary;

                    using (FileStream tempStream = new FileStream(fileName, FileMode.Open))
                    {
                        EndianBinaryWriter tempWriter = new EndianBinaryWriter(tempStream, Endian.Big);

                        tempWriter.Write(uncompressedArc);

                        tempWriter.Flush();

                        tempWriter.Close();
                    }
                }
            }

            RARC loadedArc = new RARC(fileName);

            if (File.Exists(tempFileName))
            {
                File.Delete(tempFileName);
            }

            FilesFromArc = new List<FileData>();

            for (int i = 0; i < loadedArc.Nodes.Count(); i++)
            {
                for (int j = 0; j < loadedArc.Nodes[i].Entries.Count(); j++)
                {
                    if (loadedArc.Nodes[i].Entries[j].Data != null)
                    {
                        FileData file = new FileData();

                        file.Name = loadedArc.Nodes[i].Entries[j].Name;

                        file.Data = loadedArc.Nodes[i].Entries[j].Data;

                        FilesFromArc.Add(file);
                    }
                }
            }

            foreach (FileData file in FilesFromArc)
            {
                if (file.Name.Contains(".dzb"))
                {
                    using (EndianBinaryReader reader = new EndianBinaryReader(file.Data, Endian.Big))
                    {

                        Collision = new CollisionMesh();

                        Collision.Load(reader);
                    }
                }

                if (file.Name.Contains(".dzs") || file.Name.Contains(".dzr"))
                {
                    using (EndianBinaryReader reader = new EndianBinaryReader(file.Data, Endian.Big))
                    {
                        Read(reader);
                    }
                }

                if (file.Name.Contains(".bdl") || file.Name.Contains(".bmd"))
                {
                    //Not implemented
                }
            }
        }

        public void LoadFromDzx(string fileName)
        {
            if ((fileName.Contains(".dzr")) || (fileName.Contains(".dzs")))
            {
                FileStream stream = new FileStream(fileName, FileMode.Open);

                EndianBinaryReader reader = new EndianBinaryReader(stream, Endian.Big);

                Read(reader);
            }
        }

        public void SaveToArc(string fileName)
        {
            FileStream stream = new FileStream(fileName, FileMode.Create);
            EndianBinaryWriter writer = new EndianBinaryWriter(stream, Endian.Big);

            RARCPacker packer = new RARCPacker();

            string[] deconstructedFileName = fileName.Split('\\');

            string[] nameFromExtension = deconstructedFileName[deconstructedFileName.Length - 1].Split('.');

            string actualName = nameFromExtension[0];

            MemoryStream memStream = new MemoryStream();

            EndianBinaryWriter arrayWriter = new EndianBinaryWriter(memStream, Endian.Big);

            Write(arrayWriter);

            byte[] fileData = memStream.ToArray();

            FileData file = FilesFromArc.Find(x => x.Name.Contains(".dzr"));

            if (file == null)
            {
                file = FilesFromArc.Find(x => x.Name.Contains(".dzs"));
            }

            file.Data = fileData;

            VirtualFolder root = MakeVirtualDir(actualName);

            packer.Pack(root, writer);
        }

        void ChunkListToByteArray(EndianBinaryWriter writer)
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

        public void SaveToDzx(string fileName)
        {
            FileStream stream = new FileStream(fileName, FileMode.Create);

            EndianBinaryWriter writer = new EndianBinaryWriter(stream, Endian.Big);

            Write(writer);
        }

        void Read(EndianBinaryReader reader)
        {
            if (CurrentControl != null)
            {
                CurrentControl.Hide();

                CurrentControl.RemoveFromMainForm(MainForm);

                CurrentControl = null;
            }

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

                if (chunkName == "RTBL")
                {
                    //Skip offset bank, which is a table of integers
                    reader.BaseStream.Position += 4 * numChunks;
                }

                for (int j = 0; j < numChunks; j++)
                {
                    Chunk newChunk = new Chunk();

                    newChunk.Read(reader, chunkName, ChunkTemplates);

                    Chunks.Add(newChunk);
                }

                reader.BaseStream.Position = readerOffsetStorage;
            }

            foreach (Chunk chunk in Chunks)
            {
                string chunkSearchString = chunk.ChunkType.Remove(chunk.ChunkType.Length - 1);

                EntityTemplate template = ChunkTemplates.Find(x => x.ChunkID.Contains(chunkSearchString));

                template.ReadSpecialProcess(chunk.Fields, Chunks);
            }

            UpdateTreeView();

            MainForm.ElementView.SelectedNode = MainForm.ElementView.Nodes[0];

            SelectedChunk = Chunks[0];

            Controls = LoadControls();

            IsListLoaded = true;
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

        VirtualFolder MakeVirtualDir(string fileName)
        {
            VirtualFolder root = new VirtualFolder();

            root.Name = fileName;

            root.NodeName = "ROOT";

            root.Subdirs = new List<VirtualFolder>();

            root.Files = new List<FileData>();

            string lastExtension = "";

            VirtualFolder lastFolder = new VirtualFolder();

            foreach (FileData data in FilesFromArc)
            {
                string[] fileNameSplit = data.Name.Split('.');

                string fileExtension = "";

                if (fileNameSplit.Length == 2)
                {
                    fileExtension = fileNameSplit[1];
                }

                if (fileExtension == "bti")
                {
                    fileExtension = "tex";
                }

                if (fileExtension == lastExtension)
                {
                    lastFolder.Files.Add(data);

                    continue;
                }

                VirtualFolder folder = new VirtualFolder();

                folder.Name = fileExtension;

                folder.NodeName = fileExtension.ToUpper() + " ";

                folder.Subdirs = new List<VirtualFolder>();

                folder.Files = new List<FileData>();

                folder.Files.Add(data);

                root.Subdirs.Add(folder);

                lastExtension = fileExtension;

                lastFolder = folder;
            }

            return root;
        }

        public byte[] DecodeYaz0(EndianBinaryReader reader)
        {
            int uncompressedSize = reader.ReadInt32();

            byte[] dest = new byte[uncompressedSize];

            int srcPlace = 0x10, dstPlace = 0; //current read/write positions

            int validBitCount = 0; //number of valid bits left in "code" byte

            byte currCodeByte = 0;

            while (dstPlace < uncompressedSize)
            {
                //read new "code" byte if the current one is used up
                if (validBitCount == 0)
                {
                    currCodeByte = reader.ReadByteAt(srcPlace);

                    ++srcPlace;

                    validBitCount = 8;
                }

                if ((currCodeByte & 0x80) != 0)
                {
                    //straight copy
                    dest[dstPlace] = reader.ReadByteAt(srcPlace);

                    dstPlace++;

                    srcPlace++;
                }

                else
                {
                    //RLE part
                    byte byte1 = reader.ReadByteAt(srcPlace);

                    byte byte2 = reader.ReadByteAt(srcPlace + 1);

                    srcPlace += 2;

                    int dist = ((byte1 & 0xF) << 8) | byte2;

                    int copySource = dstPlace - (dist + 1);

                    int numBytes = byte1 >> 4;

                    if (numBytes == 0)
                    {
                        numBytes = reader.ReadByteAt(srcPlace) + 0x12;
                        srcPlace++;
                    }

                    else
                        numBytes += 2;

                    //copy run
                    for (int i = 0; i < numBytes; ++i)
                    {
                        dest[dstPlace] = dest[copySource];

                        copySource++;

                        dstPlace++;
                    }
                }

                //use next bit from "code" byte
                currCodeByte <<= 1;

                validBitCount -= 1;
            }

            return dest;
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
            ProjMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(65), (float)((float)width / (float)height), 100, 500000);

            foreach (Chunk chun in Chunks)
            {
                switch (chun.Geometry)
                {
                    case GeometryType.Sphere:
                    case GeometryType.Cube:
                        GL.Enable(EnableCap.PolygonOffsetFill);

                        GL.PolygonOffset(1.0f, 1.0f);

                        chun.Render(_uniformMVP, _uniformColor, ViewMatrix, ProjMatrix);

                        GL.Disable(EnableCap.PolygonOffsetFill);

                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);

                        chun.DisplayColor = Color.Black;

                        chun.Render(_uniformMVP, _uniformColor, ViewMatrix, ProjMatrix);

                        if (chun == SelectedChunk)
                            chun.DisplayColor = Color.Red;

                        else
                            chun.ResetDisplayColor();

                        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

                        break;
                    case GeometryType.LineStrip:
                        chun.UpdateLineStrip();

                        GL.LineWidth(5.0f);

                        chun.Render(_uniformMVP, _uniformColor, ViewMatrix, ProjMatrix);

                        GL.LineWidth(1.0f);

                        break;
                    case GeometryType.None:
                        //Do nothing
                        break;
                }
            }

            if (Collision != null)
            {
                GL.Disable(EnableCap.Texture2D);

                GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
                GL.Enable(EnableCap.Blend);
                GL.Enable(EnableCap.CullFace);
                GL.CullFace(CullFaceMode.Back);
                GL.FrontFace(FrontFaceDirection.Ccw);

                GL.Enable(EnableCap.PolygonOffsetFill);
                GL.PolygonOffset(1.0f, 1.0f);

                Color4 colColor = new Color4(Color4.LightYellow.R, Color4.LightYellow.G, Color4.LightYellow.B, 0.45f);

                GL.Uniform4(_uniformColor, colColor);

                Collision.Render(MainForm.Viewport, _uniformMVP, ViewMatrix, ProjMatrix);

                GL.Disable(EnableCap.PolygonOffsetFill);

                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
                GL.Uniform4(_uniformColor, Color4.Black);
                Collision.Render(MainForm.Viewport, _uniformMVP, ViewMatrix, ProjMatrix);
                GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
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

            string searchString = chunkType.Remove(chunkType.Length - 1);

            newChunk.MakeEmptyChunkFromTemplate(ChunkTemplates.Find(x => x.ChunkID.Contains(searchString)));

            newChunk.ChunkType = chunkType;

            Chunks.Add(newChunk);

            ControlObject newControl = new ControlObject();

            newControl.Load(newChunk);

            Controls.Add(newControl);

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

            UpdateControls();
        }

        public void ChangeSelectionFromTreeNode(TreeNode node)
        {
            CurSelectedNode = node;

            if (CurrentControl != null)
            {
                //CurrentControl.Hide();

                //CurrentControl.RemoveFromMainForm(MainForm);
            }

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

                        UpdateControls();
                    }
                }
            }
        }

        private void UpdateControls()
        {
            if (CurrentControl != null)
            {
                CurrentControl.Hide();

                CurrentControl.RemoveFromMainForm(MainForm);
            }

            string truncatedType = SelectedChunk.ChunkType.Remove(SelectedChunk.ChunkType.Length - 1);

            CurrentControl = Controls.Find(x => x.ChunkID.Contains(truncatedType));

            CurrentControl.FillGeneral(SelectedChunk);

            CurrentControl.AddToMainForm(MainForm);

            CurrentControl.Show();
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
        #region Variables

        public string ChunkType;

        public string DisplayName;

        public GeometryType Geometry;

        public Vector3 Position;

        Color4 ActualColor;

        public Color4 DisplayColor;

        public List<ElementProperty> Fields;

        Vector3[] Vertexes;

        int[] Meshes;

        int _glVbo;

        int _glEbo;

        #endregion

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

            if (ChunkType == "RTBL")
            {
            }

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

                        if (prop.Name == "Position")
                        {
                            Position = (Vector3)prop.Data;
                        }
                        break;
                    case FieldType.String:
                        string test = reader.ReadString((uint)prop.Length).Trim('\0');
                        prop.Data = test;
                        DisplayName = (string)prop.Data;
                        break;
                    case FieldType.ListByte:
                        List<byte> byteList = new List<byte>();

                        int readerOffsetStorage = (int)reader.BaseStream.Position;

                        if (ChunkType == "RTBL")
                        {
                            int roomListOffset = (int)template.Properties.Find(x => x.Name == "Room List Offset").Data;

                            readerOffsetStorage = (int)reader.BaseStream.Position;

                            reader.BaseStream.Position = roomListOffset;

                            prop.Length = Convert.ToInt32(template.Properties.Find(x => x.Name == "Room Count").Data);
                        }

                        for (int i = 0; i < prop.Length; i++)
                        {
                            byteList.Add(reader.ReadByte());
                        }

                        prop.Data = byteList;

                        if (ChunkType == "RTBL")
                        {
                            reader.BaseStream.Position = readerOffsetStorage;
                        }
                        break;
                    case FieldType.ListShort:
                        List<short> shortList = new List<short>();

                        for (int i = 0; i < prop.Length; i++)
                        {
                            shortList.Add(reader.ReadInt16());
                        }

                        prop.Data = shortList;
                        break;
                    case FieldType.ListRPPN:
                        List<Chunk> rppnList = new List<Chunk>();

                        prop.Data = rppnList;
                        break;
                    case FieldType.Color:
                        Color color = Color.FromArgb((int)reader.ReadByte(), (int)reader.ReadByte(), (int)reader.ReadByte());

                        prop.Data = color;
                        break;
                    case FieldType.ColorAlpha:

                        int r = (int)reader.ReadByte();
                        int g = (int)reader.ReadByte();
                        int b = (int)reader.ReadByte();
                        int a = (int)reader.ReadByte();

                        Color colorAlpha = Color.FromArgb(a, r, g, b);

                        prop.Data = colorAlpha;
                        break;
                }
            }

            foreach (ElementProperty prop in template.Properties)
            {
                ElementProperty actualProp = prop.Copy();

                Fields.Add(actualProp);
            }

            //template.ReadSpecialProcess(Fields);

            switch (Geometry)
            {
                case GeometryType.Cube:
                    GenerateCube();
                    break;
                case GeometryType.Sphere:
                    GenerateSphere(250);
                    break;
                case GeometryType.LineStrip:
                    GenerateLineStrip();
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
                        prop.Data = (byte)0;
                        break;
                    case FieldType.Short:
                        prop.Data = (short)0;
                        break;
                    case FieldType.Integer:
                        prop.Data = (int)0;
                        break;
                    case FieldType.Float:
                        prop.Data = 0.0f;
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

        public void Render(int _uniformMVP, int _uniformColor, Matrix4 viewMatrix, Matrix4 projMatrix)
        {
            Matrix4 modelMatrix;

            Matrix4 finalMatrix;

            switch (Geometry)
            {
                case GeometryType.Cube:
                case GeometryType.Sphere:
                    //Build a Model View Projection Matrix. This is where you would add camera movement (modifiying the View matrix), Perspective rendering (perspective matrix) and model position/scale/rotation (Model)
                    modelMatrix = Matrix4.CreateTranslation(Position) * Matrix4.Rotate(Quaternion.Identity) * Matrix4.Scale(1);

                    //Bind the buffers that have the data you want to use
                    GL.BindBuffer(BufferTarget.ArrayBuffer, _glVbo);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, _glEbo);

                    //Then, you have to tell the GPU what the contents of the Array buffer look like. Ie: Is each entry just a position, or does it have a position, color, normal, etc.
                    GL.EnableVertexAttribArray((int)ShaderAttributeIds.Position);
                    GL.VertexAttribPointer((int)ShaderAttributeIds.Position, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);

                    //Upload the WVP to the GPU
                    finalMatrix = modelMatrix * viewMatrix * projMatrix;
                    GL.Uniform4(_uniformColor, DisplayColor);
                    GL.UniformMatrix4(_uniformMVP, false, ref finalMatrix);

                    //Now we tell the GPU to actually draw the data we have
                    GL.DrawElements(BeginMode.Triangles, Meshes.Count(), DrawElementsType.UnsignedInt, 0);

                    //This is cleanup to undo the changes to the OpenGL state we did to draw this model.
                    GL.DisableVertexAttribArray((int)ShaderAttributeIds.Position);
                    break;
                case GeometryType.LineStrip:
                    //Build a Model View Projection Matrix. This is where you would add camera movement (modifiying the View matrix), Perspective rendering (perspective matrix) and model position/scale/rotation (Model)
                    modelMatrix = Matrix4.Identity;

                    //Bind the buffers that have the data you want to use
                    GL.BindBuffer(BufferTarget.ArrayBuffer, _glVbo);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, _glEbo);

                    //Then, you have to tell the GPU what the contents of the Array buffer look like. Ie: Is each entry just a position, or does it have a position, color, normal, etc.
                    GL.EnableVertexAttribArray((int)ShaderAttributeIds.Position);
                    GL.VertexAttribPointer((int)ShaderAttributeIds.Position, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);

                    //Upload the WVP to the GPU
                    finalMatrix = modelMatrix * viewMatrix * projMatrix;
                    GL.Uniform4(_uniformColor, DisplayColor);
                    GL.UniformMatrix4(_uniformMVP, false, ref finalMatrix);

                    //Now we tell the GPU to actually draw the data we have
                    GL.DrawElements(BeginMode.LineStrip, Meshes.Count(), DrawElementsType.UnsignedInt, 0);

                    //This is cleanup to undo the changes to the OpenGL state we did to draw this model.
                    GL.DisableVertexAttribArray((int)ShaderAttributeIds.Position);
                    break;
                case GeometryType.None:
                    break;
            }
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

        void GenerateLineStrip()
        {
            GL.GenBuffers(1, out _glVbo);

            GL.GenBuffers(1, out _glEbo);
        }

        public void UpdateLineStrip()
        {
            List<Chunk> rppnList = (List<Chunk>)Fields.Find(x => x.Name == "Waypoint List").Data;

            Vertexes = new Vector3[rppnList.Count];

            Meshes = new int[rppnList.Count];

            for (int i = 0; i < rppnList.Count; i++)
            {
                Vertexes[i] = rppnList[i].Position;

                Meshes[i] = i;
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, _glVbo);

            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Vertexes.Length * Vector3.SizeInBytes), Vertexes,
                BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _glEbo);

            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(Meshes.Length * 4), Meshes,
                BufferUsageHint.StaticDraw);
        }

        public void ResetDisplayColor()
        {
            DisplayColor = ActualColor;
        }

        public bool CheckRay(Vector3 eye, Vector3 ray)
        {
            bool isSelected = false;

            switch (Geometry)
            {
                case GeometryType.Sphere:
                case GeometryType.Cube:
                    Vector3 position = (Vector3)Fields.Find(x => x.Name == "Position").Data;

                    Position = position;

                    float b = Vector3.Dot(ray, (eye - position));

                    float c = Vector3.Dot((eye - position), (eye - position));

                    c = c - 2000;

                    float a = (b * b) - c;

                    if (a >= 0)
                        isSelected = true;

                    break;
                case GeometryType.LineStrip:
                case GeometryType.None:
                    break;
            }

            return isSelected;
        }
    }

    public class EntityTemplate
    {
        public string ChunkID;

        public GeometryType Geometry;

        public System.Drawing.Color Color;

        public List<ElementProperty> Properties = new List<ElementProperty>();

        public List<BitFieldObject> BitField = new List<BitFieldObject>();

        public List<Chunk> ChunkListReference;

        public void ReadSpecialProcess(List<ElementProperty> fields, List<Chunk> mainListReference)
        {
            ChunkListReference = mainListReference;

            List<ElementProperty> tempFieldStorage = new List<ElementProperty>();

            foreach (ElementProperty field in fields)
            {
                switch (field.Name)
                {
                    case "Bit Field":
                        if (BitField.Count > 0)
                        {
                            int fieldValue = (int)field.Data;

                            for (int i = 0; i < BitField.Count; i++)
                            {
                                ElementProperty prop = new ElementProperty();

                                int propValue = (fieldValue & BitField[i].Mask) >> BitField[i].BitShift;

                                prop.MakeProperty(BitField[i].Name, FieldType.Integer, (int)propValue);

                                tempFieldStorage.Add(prop);
                            }
                        }
                        break;
                    case "Waypoint List":
                        int firstPointOffset = Convert.ToInt32(fields.Find(x => x.Name == "First Waypoint Offset").Data);

                        int firstWaypointIndex = firstPointOffset / 0x10;

                        int numPoints = Convert.ToInt32(fields.Find(x => x.Name == "Number of Waypoints").Data);

                        List<Chunk> chunkRPPNList = (List<Chunk>)field.Data;

                        List<IGrouping<string, Chunk>> query = ChunkListReference.GroupBy(x => x.ChunkType, x => x).ToList();

                        List<Chunk> rPPN = query.Find(x => x.Key == "RPPN").ToList();

                        for (int i = 0; i < numPoints; i++)
                        {
                            chunkRPPNList.Add(rPPN[firstWaypointIndex + i]);
                        }

                        break;
                }
            }

            if (tempFieldStorage.Count > 0)
            {
                fields.InsertRange(fields.IndexOf(fields.Find(x => x.Name == "Bit Field")), tempFieldStorage);

                fields.RemoveAt(fields.IndexOf(fields.Find(x => x.Name == "Bit Field")));
            }

            if (ChunkID == "RPAT" || ChunkID == "PATH")
            {
                int numberPointsIndex = fields.IndexOf(fields.Find(x => x.Name == "Number of Waypoints"));


                using (FileStream stream = new FileStream("C:\\consoleoutput.txt", FileMode.Create))
                {
                    EndianBinaryWriter writer = new EndianBinaryWriter(stream, Endian.Big);

                    writer.Write("Number of Points Index: " + numberPointsIndex + Environment.NewLine);
                    writer.Write("Collection count: " + fields.Count + Environment.NewLine);
                }

                if ((numberPointsIndex >= 0) && (numberPointsIndex <= fields.Count - 1))
                    fields.RemoveAt(numberPointsIndex);

                int firstPointOffsetIndex = fields.IndexOf(fields.Find(x => x.Name == "First Waypoint Offset"));

                if ((firstPointOffsetIndex >= 0) && (firstPointOffsetIndex <= fields.Count - 1))
                    fields.RemoveAt(firstPointOffsetIndex);
            }
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

    public class ControlObject
    {
        public string ChunkID;

        Panel ControlBase;

        public void Load(Chunk chunk)
        {
            ChunkID = chunk.ChunkType;

            ControlBase = new Panel();

            ControlBase.Dock = DockStyle.Right;

            ControlBase.Width = 225;

            int controlYPos = 13;

            foreach (ElementProperty prop in chunk.Fields)
            {
                switch (prop.Type)
                {
                    case FieldType.Byte:
                        NumericUpDown ByteNumeric = new NumericUpDown();
                        ByteNumeric.Name = prop.Name;
                        ByteNumeric.Minimum = 0;
                        ByteNumeric.Maximum = 255;
                        ByteNumeric.Location = new Point(ControlBase.Width - ByteNumeric.Width, controlYPos);
                        ControlBase.Controls.Add(ByteNumeric);
                        break;
                    case FieldType.Short:
                        NumericUpDown ShortNumeric = new NumericUpDown();
                        ShortNumeric.Name = prop.Name;
                        ShortNumeric.Minimum = short.MinValue;
                        ShortNumeric.Maximum = short.MaxValue;
                        ShortNumeric.Location = new Point(ControlBase.Width - ShortNumeric.Width, controlYPos);
                        ControlBase.Controls.Add(ShortNumeric);
                        break;
                    case FieldType.Integer:
                        NumericUpDown IntNumeric = new NumericUpDown();
                        IntNumeric.Name = prop.Name;
                        IntNumeric.Minimum = int.MinValue;
                        IntNumeric.Maximum = int.MaxValue;
                        IntNumeric.Location = new Point(ControlBase.Width - IntNumeric.Width, controlYPos);
                        ControlBase.Controls.Add(IntNumeric);
                        break;
                    case FieldType.Float:
                        NumericUpDown FloatNumeric = new NumericUpDown();
                        FloatNumeric.Name = prop.Name;
                        FloatNumeric.Minimum = decimal.MinValue;
                        FloatNumeric.Maximum = decimal.MaxValue;
                        FloatNumeric.DecimalPlaces = 3;
                        FloatNumeric.Location = new Point(ControlBase.Width - FloatNumeric.Width, controlYPos);
                        ControlBase.Controls.Add(FloatNumeric);
                        break;
                    case FieldType.Vector2:
                        controlYPos = LoadVec2Controls(controlYPos, prop);
                        continue;
                    case FieldType.Vector3:
                        controlYPos = LoadVec3Controls(controlYPos, prop);
                        continue;
                    case FieldType.String:
                        TextBox StringBox = new TextBox();
                        StringBox.Name = prop.Name;
                        StringBox.Width = 120;
                        StringBox.Location = new Point(ControlBase.Width - StringBox.Width, controlYPos);
                        ControlBase.Controls.Add(StringBox);
                        break;
                    case FieldType.ListByte:
                        break;
                }

                Label propLable = new Label();

                propLable.Name = prop.Name + "Label";
                propLable.Text = prop.Name + ": ";

                propLable.Location = new Point(7, controlYPos);

                ControlBase.Controls.Add(propLable);

                controlYPos += 26;
            }
        }

        private int LoadVec2Controls(int yPos, ElementProperty prop)
        {
            NumericUpDown Vec2NumericX = new NumericUpDown();
            Vec2NumericX.Name = prop.Name + ":X";
            Vec2NumericX.Minimum = decimal.MinValue;
            Vec2NumericX.Maximum = decimal.MaxValue;
            Vec2NumericX.DecimalPlaces = 3;
            Vec2NumericX.Location = new Point(ControlBase.Width - Vec2NumericX.Width, yPos);
            ControlBase.Controls.Add(Vec2NumericX);

            Label propLabelX = new Label();

            propLabelX.Name = prop.Name + "Label";
            propLabelX.Text = "X " + prop.Name + ": ";

            propLabelX.Location = new Point(7, yPos);

            ControlBase.Controls.Add(propLabelX);

            yPos += 26;

            NumericUpDown Vec2NumericY = new NumericUpDown();
            Vec2NumericY.Name = prop.Name + ":Y";
            Vec2NumericY.Minimum = decimal.MinValue;
            Vec2NumericY.Maximum = decimal.MaxValue;
            Vec2NumericY.DecimalPlaces = 3;
            Vec2NumericY.Location = new Point(ControlBase.Width - Vec2NumericY.Width, yPos);
            ControlBase.Controls.Add(Vec2NumericY);

            Label propLabelY = new Label();

            propLabelY.Name = prop.Name + "Label";
            propLabelY.Text = "Y " + prop.Name + ": ";

            propLabelY.Location = new Point(7, yPos);

            ControlBase.Controls.Add(propLabelY);

            yPos += 26;

            return yPos;
        }

        private int LoadVec3Controls(int yPos, ElementProperty prop)
        {
            NumericUpDown Vec3NumericX = new NumericUpDown();
            Vec3NumericX.Name = prop.Name + ":X";
            Vec3NumericX.Minimum = decimal.MinValue;
            Vec3NumericX.Maximum = decimal.MaxValue;
            Vec3NumericX.DecimalPlaces = 3;
            Vec3NumericX.Location = new Point(ControlBase.Width - Vec3NumericX.Width, yPos);
            ControlBase.Controls.Add(Vec3NumericX);

            Label propLabelX = new Label();

            propLabelX.Name = prop.Name + "Label";
            propLabelX.Text = "X " + prop.Name + ": ";

            propLabelX.Location = new Point(7, yPos);

            ControlBase.Controls.Add(propLabelX);

            yPos += 26;

            NumericUpDown Vec3NumericY = new NumericUpDown();
            Vec3NumericY.Name = prop.Name + ":Y";
            Vec3NumericY.Minimum = decimal.MinValue;
            Vec3NumericY.Maximum = decimal.MaxValue;
            Vec3NumericY.DecimalPlaces = 3;
            Vec3NumericY.Location = new Point(ControlBase.Width - Vec3NumericY.Width, yPos);
            ControlBase.Controls.Add(Vec3NumericY);

            Label propLabelY = new Label();

            propLabelY.Name = prop.Name + "Label";
            propLabelY.Text = "Y " + prop.Name + ": ";

            propLabelY.Location = new Point(7, yPos);

            ControlBase.Controls.Add(propLabelY);

            yPos += 26;

            NumericUpDown Vec3NumericZ = new NumericUpDown();
            Vec3NumericZ.Name = prop.Name + ":Z";
            Vec3NumericZ.Minimum = decimal.MinValue;
            Vec3NumericZ.Maximum = decimal.MaxValue;
            Vec3NumericZ.DecimalPlaces = 3;
            Vec3NumericZ.Location = new Point(ControlBase.Width - Vec3NumericZ.Width, yPos);
            ControlBase.Controls.Add(Vec3NumericZ);

            Label propLabelZ = new Label();

            propLabelZ.Name = prop.Name + "Label";
            propLabelZ.Text = "Z " + prop.Name + ": ";

            propLabelZ.Location = new Point(7, yPos);

            ControlBase.Controls.Add(propLabelZ);

            yPos += 26;

            return yPos;
        }

        public void FillGeneral(Chunk chunk)
        {
            foreach (ElementProperty prop in chunk.Fields)
            {
                if (prop.Name == "Position")
                {
                    FillVec3(prop);
                }

                var control = ControlBase.Controls.Find(prop.Name, false);

                switch (prop.Type)
                {
                    case FieldType.Byte:
                        NumericUpDown byteControl = (NumericUpDown)control[0];
                        byteControl.Value = (byte)prop.Data;
                        break;
                    case FieldType.Short:
                        NumericUpDown shortControl = (NumericUpDown)control[0];
                        shortControl.Value = (short)prop.Data;
                        break;
                    case FieldType.Integer:
                        NumericUpDown intControl = (NumericUpDown)control[0];
                        intControl.Value = (int)prop.Data;
                        break;
                    case FieldType.Float:
                        NumericUpDown floatControl = (NumericUpDown)control[0];
                        floatControl.Value = Convert.ToDecimal(prop.Data);
                        break;
                    case FieldType.Vector2:
                        //prop.Data = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                        break;
                    case FieldType.Vector3:
                        //prop.Data = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                        break;
                    case FieldType.String:
                        TextBox box = (TextBox)control[0];
                        box.Text = (string)prop.Data;
                        break;
                    case FieldType.ListByte:
                        break;
                }
            }
        }

        private void FillVec3(ElementProperty prop)
        {
            Vector3 pos = (Vector3)prop.Data;

            List<NumericUpDown> posControls = new List<NumericUpDown>();

            List<NumericUpDown> vecControls = new List<NumericUpDown>();

            Control[] controlX = ControlBase.Controls.Find("Position:X", false);

            Control[] controlY = ControlBase.Controls.Find("Position:Y", false);

            Control[] controlZ = ControlBase.Controls.Find("Position:Z", false);

            NumericUpDown numControlX = (NumericUpDown)controlX[0];

            NumericUpDown numControlY = (NumericUpDown)controlY[0];

            NumericUpDown numControlZ = (NumericUpDown)controlZ[0];

            numControlX.Value = Convert.ToDecimal(pos.X);

            numControlY.Value = Convert.ToDecimal(pos.Y);

            numControlZ.Value = Convert.ToDecimal(pos.Z);
        }

        public void Hide()
        {
            ControlBase.Hide();
        }

        public void Show()
        {
            ControlBase.Show();
        }

        public void AddToMainForm(MainUI main)
        {
            main.Controls.Add(ControlBase);
        }

        public void RemoveFromMainForm(MainUI main)
        {
            main.Controls.Remove(ControlBase);
        }

        public void SaveFields(Chunk chunk)
        {
            foreach (ElementProperty prop in chunk.Fields)
            {
                var control = ControlBase.Controls.Find(prop.Name, false);

                switch (prop.Type)
                {
                    case FieldType.Byte:
                        NumericUpDown byteControl = (NumericUpDown)control[0];
                        prop.Data = (byte)byteControl.Value;
                        break;
                    case FieldType.Short:
                        NumericUpDown shortControl = (NumericUpDown)control[0];
                        prop.Data = (short)shortControl.Value;
                        break;
                    case FieldType.Integer:
                        NumericUpDown intControl = (NumericUpDown)control[0];
                        prop.Data = (int)intControl.Value;
                        break;
                    case FieldType.Float:
                        NumericUpDown floatControl = (NumericUpDown)control[0];
                        prop.Data = Convert.ToSingle(floatControl.Value);
                        break;
                    case FieldType.Vector2:
                        //prop.Data = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                        break;
                    case FieldType.Vector3:
                        SavePosition(prop);
                        chunk.Position = (Vector3)prop.Data;
                        break;
                    case FieldType.String:
                        TextBox box = (TextBox)control[0];
                        prop.Data = (string)box.Text;
                        break;
                    case FieldType.ListByte:
                        break;
                }
            }
        }

        void SavePosition(ElementProperty prop)
        {
            Vector3 pos = (Vector3)prop.Data;

            List<NumericUpDown> posControls = new List<NumericUpDown>();

            List<NumericUpDown> vecControls = new List<NumericUpDown>();

            Control[] controlX = ControlBase.Controls.Find("Position:X", false);

            Control[] controlY = ControlBase.Controls.Find("Position:Y", false);

            Control[] controlZ = ControlBase.Controls.Find("Position:Z", false);

            NumericUpDown numControlX = (NumericUpDown)controlX[0];

            NumericUpDown numControlY = (NumericUpDown)controlY[0];

            NumericUpDown numControlZ = (NumericUpDown)controlZ[0];

            pos.X = Convert.ToSingle(numControlX.Value);

            pos.Y = Convert.ToSingle(numControlY.Value);

            pos.Z = Convert.ToSingle(numControlZ.Value);

            prop.Data = pos;
        }
    }

    class CollisionMesh
    {
        Vector3[] Vertexes;

        int[] Meshes;

        int _glVbo;

        int _glEbo;

        public void Load(EndianBinaryReader reader)
        {
            int numVerts = reader.ReadInt32();

            int vertsOffset = reader.ReadInt32();

            int numFaces = reader.ReadInt32();

            int facesOffset = reader.ReadInt32();

            reader.BaseStream.Position = vertsOffset;

            List<Vector3> vertsList = new List<Vector3>();

            for (int i = 0; i < numVerts; i++)
            {
                vertsList.Add(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
            }

            Vertexes = vertsList.ToArray();

            reader.BaseStream.Position = facesOffset;

            List<int> facesList = new List<int>();

            for (int i = 0; i < numFaces; i++)
            {
                facesList.Add(reader.ReadInt16());

                facesList.Add(reader.ReadInt16());

                facesList.Add(reader.ReadInt16());

                reader.Skip(4);
            }

            Meshes = facesList.ToArray();

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

        public void Render(GLControl Viewport, int _uniformMVP, Matrix4 viewMatrix, Matrix4 projMatrix)
        {
            //Build a Model View Projection Matrix. This is where you would add camera movement (modifiying the View matrix), Perspective rendering (perspective matrix) and model position/scale/rotation (Model)
            Matrix4 modelMatrix = Matrix4.Identity;

            //Bind the buffers that have the data you want to use
            GL.BindBuffer(BufferTarget.ArrayBuffer, _glVbo);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _glEbo);

            //Then, you have to tell the GPU what the contents of the Array buffer look like. Ie: Is each entry just a position, or does it have a position, color, normal, etc.
            GL.EnableVertexAttribArray((int)ShaderAttributeIds.Position);
            GL.VertexAttribPointer((int)ShaderAttributeIds.Position, 3, VertexAttribPointerType.Float, false, Vector3.SizeInBytes, 0);

            //Upload the WVP to the GPU
            Matrix4 finalMatrix = modelMatrix * viewMatrix * projMatrix;
            GL.UniformMatrix4(_uniformMVP, false, ref finalMatrix);

            //Now we tell the GPU to actually draw the data we have
            GL.DrawElements(BeginMode.Triangles, Meshes.Count(), DrawElementsType.UnsignedInt, 0);

            //This is cleanup to undo the changes to the OpenGL state we did to draw this model.
            GL.DisableVertexAttribArray((int)ShaderAttributeIds.Position);
        }

    }
}
