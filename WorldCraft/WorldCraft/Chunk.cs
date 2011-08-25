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

        private BasicEffect _effect;

        private VertexBuffer _solidVertexBuffer;
        private IndexBuffer _solidIndexBuffer;

        private List<int> _solidBlocksOffsetsList;
        private List<int> _solidIndexList;
        private List<VertexPositionNormalTexture> _solidVertexList;
        private Dictionary<int, List<int>> _solidIndicesDict;

        private VertexBuffer _liquidVertexBuffer;
        private IndexBuffer _liquidIndexBuffer;

        private List<int> _liquidBlocksOffsetsList;
        private List<int> _liquidIndexList;
        private List<VertexPositionNormalTexture> _liquidVertexList;
        private Dictionary<int, List<int>> _liquidIndicesDict;

        public const short WIDTH = 32;
        public const short DEPTH = 32;
        public const short HEIGHT = 32;

        public int NumVertices { get; protected set; }

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
            _solidIndicesDict = new Dictionary<int,List<int>>();
            _solidBlocksOffsetsList = new List<int>();

            _liquidVertexList = new List<VertexPositionNormalTexture>();
            _liquidIndexList = new List<int>();
            _liquidIndicesDict = new Dictionary<int, List<int>>();
            _liquidBlocksOffsetsList = new List<int>();

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
            if (_solidVertexList.Count == 0 && _liquidVertexList.Count == 0)
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

                    if (_solidVertexList.Count > 0 )
                    {
                        _game.GraphicsDevice.SetVertexBuffer(_solidVertexBuffer);
                        _game.GraphicsDevice.Indices = _solidIndexBuffer;
                        _game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _solidVertexList.Count, 0, _solidIndexList.Count / 3);
                    }

                    if (_liquidVertexList.Count > 0)
                    {
                        _game.GraphicsDevice.SetVertexBuffer(_liquidVertexBuffer);
                        _game.GraphicsDevice.Indices = _liquidIndexBuffer;
                        _game.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _liquidVertexList.Count, 0, _liquidIndexList.Count / 3);
                    }
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

            if (BlockHelper.IsNone(oldBlock) && !BlockHelper.IsNone(block))
            {
                // Replace none block with plain block => We must create new block vertices and destroy neighbours ones
            }
            else if (!BlockHelper.IsNone(oldBlock) && BlockHelper.IsNone(block))
            {
                // Replace plain block with none block => We must destroy vertices and build neighbours ones

                NumVertices -= DestroyBlockVertices(mapX, mapY, mapZ);
                var directions = _blockAccessor.AllDirections;

                foreach (var dir in directions)
                {
                    if (!_blockAccessor.MoveTo(mapX, mapY, mapZ).MoveTo(dir).IsNone)
                        NumVertices += RebuildBlockVertices(_blockAccessor.X, _blockAccessor.Y, _blockAccessor.Z);
                }
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

            _liquidIndexList.Clear();
            _liquidVertexList.Clear();
            _liquidIndicesDict.Clear();

            NumVertices = 0;
        }

        private void UpdateBuffers()
        {
            if (_solidVertexList.Count > 0)
            {
                _solidVertexBuffer = new VertexBuffer(_game.GraphicsDevice, typeof(VertexPositionNormalTexture), _solidVertexList.Count, BufferUsage.WriteOnly);
                _solidVertexBuffer.SetData(_solidVertexList.ToArray());

                _solidIndexBuffer = new IndexBuffer(_game.GraphicsDevice, IndexElementSize.ThirtyTwoBits, _solidIndexList.Count, BufferUsage.WriteOnly);
                _solidIndexBuffer.SetData(_solidIndexList.ToArray());
            }

            if (_liquidVertexList.Count > 0)
            {
                _liquidVertexBuffer = new VertexBuffer(_game.GraphicsDevice, typeof(VertexPositionNormalTexture), _liquidVertexList.Count, BufferUsage.WriteOnly);
                _liquidVertexBuffer.SetData(_liquidVertexList.ToArray());

                _liquidIndexBuffer = new IndexBuffer(_game.GraphicsDevice, IndexElementSize.ThirtyTwoBits, _liquidIndexList.Count, BufferUsage.WriteOnly);
                _liquidIndexBuffer.SetData(_liquidIndexList.ToArray());
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
                        NumVertices += BuildVertices(
                            _blocks[offset + y],
                            x + _offset.X * WIDTH,
                            y + _offset.Y * HEIGHT,
                            z + _offset.Z * DEPTH);
                    }
                }


            UpdateBuffers();
        }

        private bool DrawFace(Block block1, Block block2)
        {
            if (BlockHelper.IsTransparent(block1) && BlockHelper.IsSolid(block2))
                return true;
            if (BlockHelper.IsSolid(block1) && BlockHelper.IsTransparent(block2))
                return true;
            else if (BlockHelper.IsSolid(block1) && BlockHelper.IsSolid(block2))
                return false;
            else if (BlockHelper.IsTransparent(block1) && BlockHelper.IsTransparent(block2) && !BlockHelper.IsNone(block1) && !BlockHelper.IsNone(block2))
                return false;
            else return true;
        }

        private int BuildVertices(Block block, float x, float y, float z)
        {
            if (block.Type == BlockType.None)
                return 0;

            x += 0.5f;
            y += 0.5f;
            z += 0.5f;

            bool isLiquid = BlockHelper.IsLiquid(block);

            float scaleX = 0.5f, scaleY = 0.5f, scaleZ = 0.5f;

            var faces = new List<VertexPositionNormalTexture[]>();

            // Front
            if (DrawFace(block, _blockAccessor.MoveTo((int)x, (int)y, (int)z).Backward.Block))
            {
                var face = new VertexPositionNormalTexture[4];
                face[0].Position = new Vector3(1 * scaleX + x, 1 * scaleY + y, -1 * scaleZ + z);
                face[1].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);
                face[2].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);
                face[3].Position = new Vector3(-1 * scaleX + x, 1 * scaleY + y, -1 * scaleZ + z);
                faces.Add(face);
            }

            //Back
            if (DrawFace(block, _blockAccessor.MoveTo((int)x, (int)y, (int)z).Forward.Block))
            {
                var face = new VertexPositionNormalTexture[4];
                face[0].Position = new Vector3(1 * scaleX + x, 1 * scaleY + y, 1 * scaleZ + z);
                face[1].Position = new Vector3(-1 * scaleX + x, 1 * scaleY + y, 1 * scaleZ + z);
                face[2].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                face[3].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                faces.Add(face);
            }

            //Right
            if (DrawFace(block, _blockAccessor.MoveTo((int)x, (int)y, (int)z).Right.Block))
            {
                var face = new VertexPositionNormalTexture[4];
                face[0].Position = new Vector3(1 * scaleX + x, 1 * scaleY + y, -1 * scaleZ + z);
                face[1].Position = new Vector3(1 * scaleX + x, 1 * scaleY + y, 1 * scaleZ + z);
                face[2].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                face[3].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);

                faces.Add(face);
            }


            //Bottom
            if (DrawFace(block, _blockAccessor.MoveTo((int)x, (int)y, (int)z).Down.Block))
            {
                var face = new VertexPositionNormalTexture[4];
                face[0].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);
                face[1].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                face[2].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                face[3].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);

                faces.Add(face);
            }

            //Left
            if (DrawFace(block, _blockAccessor.MoveTo((int)x, (int)y, (int)z).Left.Block))
            {
                var face = new VertexPositionNormalTexture[4];
                face[0].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);
                face[1].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                face[2].Position = new Vector3(-1 * scaleX + x, 1 * scaleY + y, 1 * scaleZ + z);
                face[3].Position = new Vector3(-1 * scaleX + x, 1 * scaleY + y, -1 * scaleZ + z);

                faces.Add(face);
            }

            //Top
            if (DrawFace(block, _blockAccessor.MoveTo((int)x, (int)y, (int)z).Up.Block))
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

                var currentVertexOffset = isLiquid ? _liquidVertexList.Count : _solidVertexList.Count;

                var indicesDict = isLiquid ? _liquidIndicesDict : _solidIndicesDict;
                var vertexList = isLiquid ? _liquidVertexList : _solidVertexList;
                var indexList = isLiquid ? _liquidIndexList : _solidIndexList;
                var blocksOffsetsList = isLiquid ? _liquidBlocksOffsetsList : _solidBlocksOffsetsList;

                foreach (var face in faces)
                    foreach (var vertex in face)
                        vertexList.Add(vertex);

                var blockOffset = GetBlockOffset((int)x, (int)y, (int)z);
                indicesDict.Add(blockOffset, new List<int>());
                blocksOffsetsList.Add(blockOffset);


                foreach (var index in indices)
                {
                    indexList.Add(index + currentVertexOffset);
                    indicesDict[blockOffset].Add(index + currentVertexOffset);
                }
            }

            return faces.Count * 4;
        }

        private int DestroyBlockVertices(float x, float y, float z)
        {
            _blockAccessor.MoveTo(x, y, z);

            var indices = _solidIndexList.ToArray();
            
            var blockOffset = GetBlockOffset((int)x, (int)y, (int)z);

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

            var offsetsListEnum = _solidBlocksOffsetsList.GetEnumerator();
            offsetsListEnum.MoveNext();

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
                    offsetsListEnum.MoveNext();

                    continue;
                }

                var list = _solidIndicesDict[offsetsListEnum.Current];
               
                for(var j = 0; j < list.Count; j++)
                {
                    list[j] += verticesOffset;
                    _solidIndexList.Add(list[j]);
                }

                i += list.Count;

                offsetsListEnum.MoveNext();
            }

            _solidIndicesDict.Remove(blockOffset);
            _solidBlocksOffsetsList.Remove(blockOffset);

            // [TODO] Must rebuild neighbours vertices

            return uniqIndices.Count;
        }

        private int RebuildBlockVertices(float x, float y, float z)
        {
            var blockOffset = GetBlockOffset((int)x, (int)y, (int)z);

            var verticesCount = 0;

            if (_solidIndicesDict.ContainsKey(blockOffset))
                verticesCount -= DestroyBlockVertices(x, y, z);

            verticesCount += BuildVertices(_blocks[blockOffset], _blockAccessor.X, _blockAccessor.Y, _blockAccessor.Z);

            return verticesCount;
        }
        #endregion

    }
}
