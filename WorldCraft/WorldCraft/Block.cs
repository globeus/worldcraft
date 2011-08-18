#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
#endregion

namespace WorldCraft
{
    #region BlockType

    public enum BlockType : byte
    {
        None,
        Rock,
        Grass
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

}
