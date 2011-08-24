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

        public const short WIDTH = 16;
        public const short DEPTH = 16;
        public const short HEIGHT = 16;

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

                NumVertices -= DestroyBlockVertices(mapX, mapY, mapZ);
                var directions = _blockAccessor.AllDirections;

                foreach (var dir in directions)
                {
                    if (_blockAccessor.MoveTo(mapX, mapY, mapZ).MoveTo(dir).IsPlain)
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

            NumVertices = 0;
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

        private int BuildVertices(Block block, float x, float y, float z)
        {
            if (block.Type == BlockType.None)
                return 0;

            x += 0.5f;
            y += 0.5f;
            z += 0.5f;

            float scaleX = 0.5f, scaleY = 0.5f, scaleZ = 0.5f;

            var faces = new List<VertexPositionNormalTexture[]>();

            // Front
            if (_blockAccessor.MoveTo((int)x, (int)y, (int)z).Backward.IsTransparent)
            {
                var face = new VertexPositionNormalTexture[4];
                face[0].Position = new Vector3(1 * scaleX + x, 1 * scaleY + y, -1 * scaleZ + z);
                face[1].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);
                face[2].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);
                face[3].Position = new Vector3(-1 * scaleX + x, 1 * scaleY + y, -1 * scaleZ + z);
                faces.Add(face);
            }

            //Back
            if (_blockAccessor.MoveTo((int)x, (int)y, (int)z).Forward.IsTransparent)
            {
                var face = new VertexPositionNormalTexture[4];
                face[0].Position = new Vector3(1 * scaleX + x, 1 * scaleY + y, 1 * scaleZ + z);
                face[1].Position = new Vector3(-1 * scaleX + x, 1 * scaleY + y, 1 * scaleZ + z);
                face[2].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                face[3].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                faces.Add(face);
            }

            //Right
            if (_blockAccessor.MoveTo((int)x, (int)y, (int)z).Right.IsTransparent)
            {
                var face = new VertexPositionNormalTexture[4];
                face[0].Position = new Vector3(1 * scaleX + x, 1 * scaleY + y, -1 * scaleZ + z);
                face[1].Position = new Vector3(1 * scaleX + x, 1 * scaleY + y, 1 * scaleZ + z);
                face[2].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                face[3].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);

                faces.Add(face);
            }


            //Bottom
            if (_blockAccessor.MoveTo((int)x, (int)y, (int)z).Down.IsTransparent)
            {
                var face = new VertexPositionNormalTexture[4];
                face[0].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);
                face[1].Position = new Vector3(1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                face[2].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                face[3].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);

                faces.Add(face);
            }

            //Left
            if (_blockAccessor.MoveTo((int)x, (int)y, (int)z).Left.IsTransparent)
            {
                var face = new VertexPositionNormalTexture[4];
                face[0].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, -1 * scaleZ + z);
                face[1].Position = new Vector3(-1 * scaleX + x, -1 * scaleY + y, 1 * scaleZ + z);
                face[2].Position = new Vector3(-1 * scaleX + x, 1 * scaleY + y, 1 * scaleZ + z);
                face[3].Position = new Vector3(-1 * scaleX + x, 1 * scaleY + y, -1 * scaleZ + z);

                faces.Add(face);
            }

            //Top
            if (_blockAccessor.MoveTo((int)x, (int)y, (int)z).Up.IsTransparent)
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
                        _solidVertexList.Add(vertex);

                var blockOffset = GetBlockOffset((int)x, (int)y, (int)z);
                _solidIndicesDict.Add(blockOffset, new List<int>());
                _solidBlocksOffsetsList.Add(blockOffset);


                foreach (var index in indices)
                {
                    _solidIndexList.Add(index + currentVertexOffset);
                    _solidIndicesDict[blockOffset].Add(index + currentVertexOffset);
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
