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

        public const short WIDTH = 4;
        public const short DEPTH = 4;
        public const short HEIGHT = 4;

        private BasicEffect _effect;

        private VertexBuffer _solidVertexBuffer;
        private IndexBuffer _solidIndexBuffer;

        private List<int> _solidIndexList;
        private List<VertexPositionNormalTexture> _solidVertexList;
        private SortedDictionary<int, List<int>> _solidIndicesDict;

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

            _solidVertexList = new List<VertexPositionNormalTexture>();
            _solidIndexList = new List<int>();
            _solidIndicesDict = new SortedDictionary<int,List<int>>();

            _blocks = blocks;
            _blockAccessor = new BlockAccessor(_game.Map);

            _effect = new BasicEffect(_game.GraphicsDevice);
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
            if (_solidVertexList.Count == 0)
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
            if (_solidVertexList.Count == 0)
                return;

            if (InViewFrustrum)
            {
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
                    _game.GraphicsDevice.SetVertexBuffer(_solidVertexBuffer);
                    _game.GraphicsDevice.Indices = _solidIndexBuffer;
                    _game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _solidVertexList.Count, 0, _solidIndexList.Count / 3);
                }
            }
        }

        #endregion

        #region Block accessor

        private int GetBlockOffset(int mapX, int mapY, int mapZ)
        {
            return (mapX - (int)_offset.X * WIDTH) * DEPTH * HEIGHT
                + (mapZ - (int)_offset.Z * DEPTH) * HEIGHT
                + (mapY - (int)_offset.Y * HEIGHT);
        }

        public Block GetBlockAt(int mapX, int mapY, int mapZ)
        {
            return _blocks[GetBlockOffset(mapX, mapY, mapZ)];
        }

        public void SetBlockAt(int mapX, int mapY, int mapZ, Block block)
        {
            var offset = GetBlockOffset(mapX, mapY, mapZ);

            var oldBlock = _blocks[offset];
            _blocks[offset] = block;

            if (BlockHelper.IsNone(oldBlock) && BlockHelper.IsPlain(block))
            {
                // Replace none block with plain block => We must create new block vertices and destroy neighbours ones
            }
            else if (BlockHelper.IsPlain(oldBlock) && BlockHelper.IsNone(block))
            {
                // Replace plain block with none block => We must destroy vertices and build neighbours ones

                destroyBlockVertices(mapX, mapY, mapZ);
            }
            else if(block.Type != oldBlock.Type)
            {
                // Block type changed, need redraw
            }

            UpdateBuffers();
        }

        #endregion

        #region Vertices processing

        private void ClearVertices()
        {
            _solidIndexList.Clear();
            _solidVertexList.Clear();
            _solidIndicesDict.Clear();
        }

        private void UpdateBuffers()
        {
            if (_solidVertexList.Count == 0)
                return;

            _solidVertexBuffer = new VertexBuffer(_game.GraphicsDevice, typeof(VertexPositionNormalTexture), _solidVertexList.Count, BufferUsage.WriteOnly);
            _solidVertexBuffer.SetData(_solidVertexList.ToArray());

            _solidIndexBuffer = new IndexBuffer(_game.GraphicsDevice, IndexElementSize.ThirtyTwoBits, _solidIndexList.Count, BufferUsage.WriteOnly);
            _solidIndexBuffer.SetData(_solidIndexList.ToArray());
        }

        private void OctreeBuildVertices(int x, int y, int z, int width, int height, int depth)
        {
            if (width == 1 && height == 1 && depth == 1)
            {
                int offset = x * DEPTH * HEIGHT + z * HEIGHT + y;
                Block block = _blocks[offset];

                BuildVertices(
                    block,
                    x + _offset.X * WIDTH,
                    y + _offset.Y * HEIGHT,
                    z + _offset.Z * DEPTH);
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
                                x + nx + _offset.X * WIDTH,
                                y + ny + _offset.Y * HEIGHT,
                                z + nz + _offset.Z * DEPTH);

                            if (nv == 0 || BlockHelper.IsTransparent(block))
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
                                x + nx + _offset.X * WIDTH,
                                y + ny + _offset.Y * HEIGHT,
                                z + nz + _offset.Z * DEPTH);

                            if (nv == 0 || BlockHelper.IsTransparent(block))
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
                                x + nx + _offset.X * WIDTH,
                                y + ny + _offset.Y * HEIGHT,
                                z + nz + _offset.Z * DEPTH);

                            if (nv == 0 || BlockHelper.IsTransparent(block))
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
            ClearVertices();

            for (int x = 0; x < WIDTH; x++)
                for (int z = 0; z < DEPTH; z++)
                {
                    int offset = x * DEPTH * HEIGHT + z * HEIGHT;

                    for (int y = 0; y < HEIGHT; y++)
                    {
                        BuildVertices(
                            _blocks[offset + y],
                            x + _offset.X * WIDTH,
                            y + _offset.Y * HEIGHT,
                            z + _offset.Z * DEPTH);
                    }
                }


            UpdateBuffers();
        }

        private int BuildVertices(Block block, float x, float y, float z)
        {
            if (block.Type == BlockType.None)
                return 0;

            x += 0.5f;
            y += 0.5f;
            z += 0.5f;

            float scaleX = 0.5f, scaleY = 0.5f, scaleZ = 0.5f;

            var faces = new List<VertexPositionNormalTexture[]>();

            Block neighbourBlock;

            // Front
            _blockAccessor.MoveTo((int)x, (int)y, (int)z);
            neighbourBlock = _blockAccessor.Backward.Block;

            if (BlockHelper.IsTransparent(neighbourBlock))
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

            if (BlockHelper.IsTransparent(neighbourBlock))
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

            if (BlockHelper.IsTransparent(neighbourBlock))
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

            if (BlockHelper.IsTransparent(neighbourBlock))
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

            if (BlockHelper.IsTransparent(neighbourBlock))
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

            if (BlockHelper.IsTransparent(neighbourBlock))
            {
                var face = new VertexPositionNormalTexture[4];
                face[0].Position = new Vector3(1 * scaleX + x, 1 * scaleY + y, 1 * scaleZ + z);
                face[1].Position = new Vector3(1 * scaleX + x, 1 * scaleY + y, -1 * scaleZ + z);
                face[2].Position = new Vector3(-1 * scaleX + x, 1 * scaleY + y, -1 * scaleZ + z);
                face[3].Position = new Vector3(-1 * scaleX + x, 1 * scaleY + y, 1 * scaleZ + z);

                faces.Add(face);
            }

            if (faces.Count > 0)
            {
                for (var i = 0; i < faces.Count; i++)
                {
                    var face = faces.ElementAt(i);

                    float[] uv = BlockHelper.GetUVMapping(block);

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
                for (int i = 0; i < faces.Count; i++)
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

                    face[indices[i * 3] % 4].Normal.Normalize();
                    face[indices[i * 3 + 1] % 4].Normal.Normalize();
                    face[indices[i * 3 + 2] % 4].Normal.Normalize();
                }

                var currentVertexOffset = _solidVertexList.Count;

                var verticesList = new List<VertexPositionNormalTexture>();

                foreach (var face in faces)
                    foreach (var vertex in face)
                    {
                        _solidVertexList.Add(vertex);
                        verticesList.Add(vertex);
                    }

                var blockOffset = GetBlockOffset((int)x, (int)y, (int)z);
                _solidIndicesDict[blockOffset] = new List<int>();

                foreach (var index in indices)
                {
                    _solidIndexList.Add(index + currentVertexOffset);
                    _solidIndicesDict[blockOffset].Add(index + currentVertexOffset);
                }
            }

            return _solidVertexList.Count;
        }

        private int destroyBlockVertices(float blockX, float blockY, float blockZ)
        {
            _blockAccessor.MoveTo(blockX, blockY, blockZ);

            var indices = _solidIndexList.ToArray();
            
            var blockOffset = GetBlockOffset((int)blockX, (int)blockY, (int)blockZ);

            if (!_solidIndicesDict.ContainsKey(blockOffset))
                return 0;

            var blockIndices = _solidIndicesDict[blockOffset];
            blockIndices.Sort();
            blockIndices.Reverse();
            
            var uniqIndices = blockIndices.GroupBy(id => id).Select(id => id.Key).ToList<int>();
            uniqIndices.Sort();
            uniqIndices.Reverse();

            var minIndex = uniqIndices.Min<int>();
            var maxIndex = uniqIndices.Max<int>();
            var numVertices = uniqIndices.Count;
            var numIndices = blockIndices.Count;

            var dictEnum = _solidIndicesDict.GetEnumerator();
            dictEnum.MoveNext();

            _solidIndexList.Clear();

            foreach (var index in uniqIndices)
                _solidVertexList.RemoveAt(index);

            var verticesOffset = 0;

            var i = 0;

            while (i < indices.Length)
            {
                var index = indices[i];

                if(index == minIndex)
                {
                    verticesOffset -= numVertices;
                    i += numIndices;
                    dictEnum.MoveNext();

                    continue;
                }

                var list = dictEnum.Current.Value;
               
                for(var j = 0; j < list.Count; j++)
                {
                    list[j] += verticesOffset;
                    _solidIndexList.Add(list[j]);
                }

                i += list.Count;

                dictEnum.MoveNext();
            }

            _solidIndicesDict.Remove(blockOffset);

            // [TODO] Must rebuild neighbours vertices

            return uniqIndices.Count;
        }

        #endregion

        #region Chunk update

        public void NotifyUpdate()
        {
        }

        #endregion

    }
}
