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
        Dirt
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
        public static bool isBlockTransparent(Block block)
        {
            return block.Type == BlockType.None;
        }

        public static float[] getUVMapping(Block block)
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
        public static short[] LEFT =        new short[] {-1, 0, 0};
        public static short[] RIGHT =       new short[] {1, 0, 0};
        public static short[] UP =          new short[] {0, 1, 0};
        public static short[] DOWN =        new short[] {0, -1, 0};
        public static short[] FORWARD =     new short[] {0, 0, 1};
        public static short[] BACKWARD =    new short[] {0, 0, -1};

        public Chunk Chunk { get; protected set; }
        public Block Block { get; protected set; }

        private Block _outOfMapBlock;
        private Map _map;
        private int _x, _y, _z;

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

            _x = x;
            _y = y;
            _z = z;

            return this;
        }

        public BlockAccessor MoveTo(short[] direction)
        {
            return MoveTo(_x + direction[0], _y + direction[1], _z + direction[2]);
        }

        public BlockAccessor Up
        {
            get
            {
                MoveTo(_x, _y + 1, _z);
                return this;
            }
        }

        public BlockAccessor Down
        {
            get
            {
                MoveTo(_x, _y - 1, _z);
                return this;
            }
        }

        public BlockAccessor Left
        {
            get
            {
                MoveTo(_x - 1, _y, _z);
                return this;
            }
        }

        public BlockAccessor Right
        {
            get
            {
                MoveTo(_x + 1, _y, _z);

                return this;
            }
        }

        public BlockAccessor Backward
        {
            get
            {
                MoveTo(_x, _y, _z - 1);
                return this;
            }
        }

        public BlockAccessor Forward
        {
            get
            {
                MoveTo(_x, _y, _z + 1);
                return this;
            }
        }

    }

    #endregion
}
