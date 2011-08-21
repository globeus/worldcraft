using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace WorldCraft
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class Chunk
    {
        #region Properties

        private Game1 _game;
        private Vector3 _offset;
        private BoundingBox _boundingBox;
        private Block[] _blocks;
        private BlockAccessor _blockAccessor;

        public const short WIDTH = 64;
        public const short DEPTH = 64;
        public const short HEIGHT = 128;

        private BasicEffect _effect;

        public GraphicsDevice GraphicsDevice { get { return _game.GraphicsDevice; } }
        public VertexBuffer SolidVertexBuffer { get; protected set; }
        public IndexBuffer SolidIndexBuffer { get; protected set; }
        public List<int> SolidIndexList { get; protected set; }
        public List<VertexPositionNormalTexture> SolidVertexList { get; protected set; }

        public bool InViewFrustrum { get; protected set; }

        #endregion

        #region GameComponent

        public Chunk(Game1 game, Vector3 offset, Block[] blocks)
        {
            _game = game;
            _offset = offset;

            _boundingBox = new BoundingBox(
                new Vector3(_offset.X * WIDTH, _offset.Y * HEIGHT, _offset.Z * DEPTH),
                new Vector3((_offset.X + 1) * WIDTH, (_offset.Y + 1) * HEIGHT, (_offset.Z + 1) * DEPTH));

            SolidVertexList = new List<VertexPositionNormalTexture>();
            SolidIndexList = new List<int>();

            _blocks = blocks;
            _blockAccessor = new BlockAccessor(_game.Map);

            _effect = new BasicEffect(GraphicsDevice);
        }

        public void Initialize()
        {
            BuildVertices();
            //OctreeBuildVertices(0, 0, 0, WIDTH, HEIGHT, DEPTH);            
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void Update(GameTime gameTime)
        {
            if (SolidVertexList.Count == 0)
                return;

            InViewFrustrum = _game.Camera.InViewFrustrum(_boundingBox);
        }

        /// <summary>
        /// Called when the DrawableGameComponent needs to be drawn. Override this method
        //  with component-specific drawing code.
        /// </summary>
        /// <param name="gameTime">Time passed since the last call to Draw.</param>
        public void Draw(GameTime gameTime)
        {
            if (SolidVertexList.Count == 0)
                return;

            if (InViewFrustrum)
            {
                // Initialize vertex buffer if it was not done before
                if (SolidVertexBuffer == null)
                {
                    SolidVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionNormalTexture), SolidVertexList.Count, BufferUsage.WriteOnly);
                    SolidVertexBuffer.SetData(SolidVertexList.ToArray());
                }

                // Initialize index buffer if it was not done before
                if (SolidIndexBuffer == null)
                {
                    SolidIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, SolidIndexList.Count, BufferUsage.WriteOnly);
                    SolidIndexBuffer.SetData(SolidIndexList.ToArray());
                }

                _effect.World = Matrix.Identity;
                _effect.View = _game.Camera.View;
                _effect.Projection = _game.Camera.Projection;

                _effect.VertexColorEnabled = false;
                _effect.TextureEnabled = true;
                _effect.Texture = _game.Map.Texture;
                _effect.LightingEnabled = true;
                _effect.EnableDefaultLighting();

                foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.SetVertexBuffer(SolidVertexBuffer);
                    GraphicsDevice.Indices = SolidIndexBuffer;
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, SolidVertexList.Count, 0, SolidIndexList.Count / 3);
                }
            }
        }

        #endregion

        #region Block accessor

        public Block GetBlockAt(int mapX, int mapY, int mapZ)
        {
            return _blocks[(mapX - (int)_offset.X * WIDTH) * DEPTH * HEIGHT
                + (mapZ - (int)_offset.Z * DEPTH) * HEIGHT
                + (mapY - (int)_offset.Y * HEIGHT)]; 
        }

        #endregion

        #region Vertices generation

        private void OctreeBuildVertices(int x, int y, int z, int width, int height, int depth)
        {
            if (width == 1 && height == 1 && depth == 1)
            {
                int offset = x * DEPTH * HEIGHT + z * HEIGHT + y;
                Block block = _blocks[offset];

                BuildVertices(
                    block,
                    x + 0.5f + _offset.X * WIDTH,
                    y + 0.5f + _offset.Y * HEIGHT,
                    z + 0.5f + _offset.Z * DEPTH,
                    0.5f, 0.5f, 0.5f);
            }
            else
            {
                bool hasTransparentBlock = false;
                int offset;
                Block block;

                float subdiv = 2.0f;

                int nwidth, nheight, ndepth, cwidth, cheight, cdepth, fwidth, fheight, fdepth;

                // Face 1,2

                for (int nz = 0; nz < depth; nz += depth - 1)
                    for (int nx = 0; nx < width; nx++)
                        for (int ny = 0; ny < height; ny++)
                        {
                            offset = (x + nx) * DEPTH * HEIGHT + (z + nz) * HEIGHT + (y + ny);
                            block = _blocks[offset];

                            int nv = BuildVertices(
                                block,
                                x + nx + 0.5f + _offset.X * WIDTH,
                                y + ny + 0.5f + _offset.Y * HEIGHT,
                                z + nz + 0.5f + _offset.Z * DEPTH,
                                0.5f, 0.5f, 0.5f);

                            if (nv == 0 || BlockHelper.isBlockTransparent(block))
                                hasTransparentBlock = true;
                        }

                // Face 3,4

                for (int ny = 0; ny < height; ny += height - 1)
                    for (int nx = 0; nx < width; nx++)
                        for (int nz = 1; nz < depth - 1; nz++)
                        {
                            offset = (x + nx) * DEPTH * HEIGHT + (z + nz) * HEIGHT + (y + ny);
                            block = _blocks[offset];

                            int nv = BuildVertices(
                                block,
                                x + nx + 0.5f + _offset.X * WIDTH,
                                y + ny + 0.5f + _offset.Y * HEIGHT,
                                z + nz + 0.5f + _offset.Z * DEPTH,
                                0.5f, 0.5f, 0.5f);

                            if (nv == 0 || BlockHelper.isBlockTransparent(block))
                                hasTransparentBlock = true;
                        }

                // Face 5,6

                for (int nx = 0; nx < width; nx += width - 1)
                    for (int ny = 1; ny < height - 1; ny++)
                        for (int nz = 1; nz < depth - 1; nz++)
                        {
                            offset = (x + nx) * DEPTH * HEIGHT + (z + nz) * HEIGHT + (y + ny);
                            block = _blocks[offset];

                            int nv = BuildVertices(
                                block,
                                x + nx + 0.5f + _offset.X * WIDTH,
                                y + ny + 0.5f + _offset.Y * HEIGHT,
                                z + nz + 0.5f + _offset.Z * DEPTH,
                                0.5f, 0.5f, 0.5f);

                            if (nv == 0 || BlockHelper.isBlockTransparent(block))
                                hasTransparentBlock = true;
                        }

                // Begin recursion if there is transparent block

                if (hasTransparentBlock)
                {
                    nwidth = (int)Math.Floor((width - 2) / subdiv);
                    nheight = (int)Math.Floor((height - 2) / subdiv);
                    ndepth = (int)Math.Floor((depth - 2) / subdiv);

                    cwidth = 0;

                    for (short i = 0; i < subdiv; i++)
                    {
                        cheight = 0;
                        fwidth = (i == (int)subdiv - 1) ? width - 2 - cwidth : nwidth;

                        for (short j = 0; j < subdiv; j++)
                        {
                            cdepth = 0;
                            fheight = (j == (int)subdiv - 1) ? height - 2 - cheight : nheight;

                            for (short k = 0; k < subdiv; k++)
                            {
                                fdepth = (k == (int)subdiv - 1) ? depth - 2 - cdepth : ndepth;

                                if (fwidth > 0 && fheight > 0 && fdepth > 0)
                                    OctreeBuildVertices(
                                        1 + x + cwidth,
                                        1 + y + cheight,
                                        1 + z + cdepth,
                                        fwidth, fheight, fdepth);

                                cdepth += fdepth;
                            }

                            cheight += fheight;
                        }

                        cwidth += fwidth;
                    }
                }
            }
        }

        private void BuildVertices()
        {
            for (var x = 0; x < WIDTH; x++)
                for (var z = 0; z < DEPTH; z++)
                {
                    var offset = x * DEPTH * HEIGHT + z * HEIGHT;

                    for (var y = 0; y < HEIGHT; y++)
                    {
                        BuildVertices(
                            _blocks[offset + y],
                            x + 0.5f + _offset.X * WIDTH,
                            y + 0.5f + _offset.Y * HEIGHT,
                            z + 0.5f + _offset.Z * DEPTH,
                            0.5f, 0.5f, 0.5f);
                    }
                }
        }

        private int BuildVertices(Block block, float x, float y, float z, float scaleX, float scaleY, float scaleZ)
        {
            if (block.Type == BlockType.None)
                return 0;

            var faces = new List<VertexPositionNormalTexture[]>();

            Block neighbourBlock;

            // Front
            _blockAccessor.MoveTo((int)x, (int)y, (int)z);
            neighbourBlock = _blockAccessor.Backward.Block;

            if (BlockHelper.isBlockTransparent(neighbourBlock))
            {
                var face = new VertexPositionNormalTexture[4];
                face[0].Position = new Vector3(1 * scaleX + x, 1 * scaleY + y, -1 * scaleZ + z);
                face[1].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);
                face[2].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);
                face[3].Position = new Vector3(-1 * scaleX + x, 1 * scaleY + y, -1 * scaleZ + z);
                faces.Add(face);
            }

            //Back
            _blockAccessor.MoveTo((int)x, (int)y, (int)z);
            neighbourBlock = _blockAccessor.Forward.Block;

            if (BlockHelper.isBlockTransparent(neighbourBlock))
            {
                var face = new VertexPositionNormalTexture[4];
                face[0].Position = new Vector3(1 * scaleX + x, 1 * scaleY + y, 1 * scaleZ + z);
                face[1].Position = new Vector3(-1 * scaleX + x, 1 * scaleY + y, 1 * scaleZ + z);
                face[2].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                face[3].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                faces.Add(face);
            }

            //Right
            _blockAccessor.MoveTo((int)x, (int)y, (int)z);
            neighbourBlock = _blockAccessor.Right.Block;

            if (BlockHelper.isBlockTransparent(neighbourBlock))
            {
                var face = new VertexPositionNormalTexture[4];
                face[0].Position = new Vector3(1 * scaleX + x, 1 * scaleY + y, -1 * scaleZ + z);
                face[1].Position = new Vector3(1 * scaleX + x, 1 * scaleY + y, 1 * scaleZ + z);
                face[2].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                face[3].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);

                faces.Add(face);
            }


            //Bottom
            _blockAccessor.MoveTo((int)x, (int)y, (int)z);
            neighbourBlock = _blockAccessor.Down.Block;

            if (BlockHelper.isBlockTransparent(neighbourBlock))
            {
                var face = new VertexPositionNormalTexture[4];
                face[0].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);
                face[1].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                face[2].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                face[3].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);

                faces.Add(face);
            }

            //Left
            _blockAccessor.MoveTo((int)x, (int)y, (int)z);
            neighbourBlock = _blockAccessor.Left.Block;

            if (BlockHelper.isBlockTransparent(neighbourBlock))
            {
                var face = new VertexPositionNormalTexture[4];
                face[0].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);
                face[1].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                face[2].Position = new Vector3(-1 * scaleX + x, 1 * scaleY + y, 1 * scaleZ + z);
                face[3].Position = new Vector3(-1 * scaleX + x, 1 * scaleY + y, -1 * scaleZ + z);

                faces.Add(face);
            }

            //Top
            _blockAccessor.MoveTo((int)x, (int)y, (int)z);
            neighbourBlock = _blockAccessor.Up.Block;

            if (BlockHelper.isBlockTransparent(neighbourBlock))
            {
                var face = new VertexPositionNormalTexture[4];
                face[0].Position = new Vector3(1 * scaleX + x, 1 * scaleY + y, 1 * scaleZ + z);
                face[1].Position = new Vector3(1 * scaleX + x, 1 * scaleY + y, -1 * scaleZ + z);
                face[2].Position = new Vector3(-1 * scaleX + x, 1 * scaleY + y, -1 * scaleZ + z);
                face[3].Position = new Vector3(-1 * scaleX + x, 1 * scaleY + y, 1 * scaleZ + z);

                faces.Add(face);
            }

            for (var i = 0; i < faces.Count; i++)
            {
                var face = faces.ElementAt(i);

                float[] uv = BlockHelper.getUVMapping(block);

                float left = uv[0] + 0.1f;
                float right = uv[0] + uv[2] * 0.90f;
                float top = uv[1] + 0.1f;
                float bottom = uv[1] + uv[3] * 0.90f;

                face[0].TextureCoordinate = new Vector2(right, top);
                face[1].TextureCoordinate = new Vector2(right, bottom);
                face[2].TextureCoordinate = new Vector2(left, bottom);
                face[3].TextureCoordinate = new Vector2(left, top);
            }

            var indices = new int[faces.Count * 6]; 

            int j = 0;
            for(int i = 0; i < faces.Count; i++)
            {
                var offset = i * 4;
                indices[j++] = 0 + offset;
                indices[j++] = 3 + offset;
                indices[j++] = 2 + offset;

                indices[j++] = 0 + offset;
                indices[j++] = 2 + offset;
                indices[j++] = 1 + offset;
            }


            for (int i = 0; i < indices.Length / 3; i++)
            {
                var face = faces.ElementAt((int)(float)i / 2);
                
                // i = 0 => face[3] - face[0], face[0] - face[2]
                // i = 1 => face[2] - face[0], face[0] - face[1]
                Vector3 firstvec = face[indices[i * 3 + 1] % 4].Position - face[indices[i * 3] % 4].Position;
                Vector3 secondvec = face[indices[i * 3] % 4].Position - face[indices[i * 3 + 2] % 4].Position;
                Vector3 normal = Vector3.Cross(firstvec, secondvec);
                normal.Normalize();
                face[indices[i * 3] % 4].Normal += normal;
                face[indices[i * 3 + 1] % 4].Normal += normal;
                face[indices[i * 3 + 2] % 4].Normal += normal;
            }

            var currentVertexOffset = SolidVertexList.Count;

            foreach (var face in faces)
                foreach(var vertex in face)
                {
                    SolidVertexList.Add(vertex);
                }

            foreach (var index in indices)
                SolidIndexList.Add(index + currentVertexOffset);

            return SolidVertexList.Count;
        }

        #endregion
    }
}
