#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using Microsoft.Xna.Framework;

#endregion

namespace WorldCraft
{
    #region BlockType

    public enum BlockType : byte
    {
        None,
        Rock,
        Grass,
        Dirt,
        Water
    }

    #endregion

    #region Block

    public struct Block
    {
        public BlockType Type;

        public Block(BlockType blockType)
        {
            Type = blockType;
        }
    }

    #endregion

    #region BlockHelper

    public static class BlockHelper
    {
        public static bool IsNone(Block block)
        {
            return block.Type == BlockType.None;
        }

        public static bool IsSolid(Block block)
        {
            return !IsNone(block) && !IsLiquid(block);
        }

        public static bool IsLiquid(Block block)
        {
            return block.Type == BlockType.Water;
        }

        public static bool IsTransparent(Block block)
        {
            return IsNone(block) || IsLiquid(block);
        }

        public static bool IsSelectable(Block block)
        {
            return block.Type != BlockType.None;
        }

        public static float[] GetUVMapping(Block block)
        {
            int numTexturesPerRow = 2;
            int numTexturesPerCol = 2;

            float width = 1.0f / numTexturesPerRow;
            float height = 1.0f / numTexturesPerCol;

            float x = ((float)(((int)block.Type - 1) % numTexturesPerRow)) / numTexturesPerRow;
            float y = ((float)(int)(((int)block.Type - 1) / numTexturesPerRow)) / numTexturesPerCol;

            return new float[] { x, y, width, height };
        }
    }

    #endregion

    #region BlocAccessor

    public class BlockAccessor
    {
        private Block _outOfMapBlock;
        private Map _map;

        public static short[] LEFT = new short[] { -1, 0, 0 };
        public static short[] RIGHT = new short[] { 1, 0, 0 };
        public static short[] UP = new short[] { 0, 1, 0 };
        public static short[] DOWN = new short[] { 0, -1, 0 };
        public static short[] FORWARD = new short[] { 0, 0, 1 };
        public static short[] BACKWARD = new short[] { 0, 0, -1 };

        public Chunk Chunk { get; protected set; }
        public Block Block { get; protected set; }

        public int X { get; protected set; }
        public int Y { get; protected set; }
        public int Z { get; protected set; }

        public List<short[]> AllDirections
        {
            get
            {
                var list = new List<short[]>(6);
                list.Add(LEFT);
                list.Add(RIGHT);
                list.Add(UP);
                list.Add(DOWN);
                list.Add(FORWARD);
                list.Add(BACKWARD);
                return list;
            }
        }

        public BlockAccessor(Map map)
        {
            _map = map;
            _outOfMapBlock = new Block(BlockType.None);
        }

        public BlockAccessor MoveTo(int x, int y, int z)
        {
            Chunk = _map.GetChunkAt(x, y, z);

            if (Chunk != null)
                Block = Chunk.GetBlockAt(x, y, z);
            else
                Block = _outOfMapBlock;

            X = x;
            Y = y;
            Z = z;

            return this;
        }

        public BlockAccessor MoveTo(float x, float y, float z)
        {
            return MoveTo((int)x, (int)y, (int)z);
        }

        public BlockAccessor MoveTo(short[] direction)
        {
            return MoveTo(X + direction[0], Y + direction[1], Z + direction[2]);
        }

        public BlockAccessor Up
        {
            get
            {
                MoveTo(X, Y + 1, Z);
                return this;
            }
        }

        public BlockAccessor Down
        {
            get
            {
                MoveTo(X, Y - 1, Z);
                return this;
            }
        }

        public BlockAccessor Left
        {
            get
            {
                MoveTo(X - 1, Y, Z);
                return this;
            }
        }

        public BlockAccessor Right
        {
            get
            {
                MoveTo(X + 1, Y, Z);

                return this;
            }
        }

        public BlockAccessor Backward
        {
            get
            {
                MoveTo(X, Y, Z - 1);
                return this;
            }
        }

        public BlockAccessor Forward
        {
            get
            {
                MoveTo(X, Y, Z + 1);
                return this;
            }
        }

        public BoundingBox BoundingBox
        {
            get
            {
                return new BoundingBox(new Vector3(X, Y, Z), new Vector3(X + 1, Y + 1, Z + 1));
            }
        }

        public bool IsNone
        {
            get
            {
                return BlockHelper.IsNone(Block);
            }
        }

        public bool IsSolid
        {
            get
            {
                return BlockHelper.IsSolid(Block);
            }
        }

        public bool IsLiquid
        {
            get
            {
                return BlockHelper.IsLiquid(Block);
            }
        }

        public bool IsTransparent
        {
            get
            {
                return BlockHelper.IsTransparent(Block);
            }
        }

        public bool IsSelectable
        {
            get
            {
                return BlockHelper.IsSelectable(Block);
            }
        }

        public void ReplaceWithBlock(Block block)
        {
            var chunk = _map.GetChunkAt(X, Y, Z);
            chunk.SetBlockAt(X, Y, Z, block);
        }

    }

    #endregion
}
