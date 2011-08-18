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
    public class Map : Microsoft.Xna.Framework.DrawableGameComponent
    {
        #region Properties

        private Game _game;
        private Camera _camera;  
        private BasicEffect _effect;
        private Block[] _blocks;

        private Texture2D[] _textures;

        private const short MAP_WIDTH = 20;
        private const short MAP_DEPTH = 20;
        private const short MAP_HEIGHT = 40;
        private const short MAP_WATER_LEVEL = 20;
        
        public VertexBuffer SolidVertexBuffer { get; protected set; }
        public IndexBuffer SolidIndexBuffer { get; protected set; }
        public List<int> SolidIndexList { get; protected set; }
        public List<VertexPositionTexture> SolidVertexList { get; protected set; }

        #endregion

        #region GameComponent

        public Map(Game game, Camera camera)
            : base(game)
        {
            _game = game;
            _camera = camera;

            SolidVertexList = new List<VertexPositionTexture>();
            SolidIndexList = new List<int>();

            _blocks = new Block[MAP_WIDTH * MAP_DEPTH * MAP_HEIGHT];
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here

            base.Initialize();                
        }

        protected override void LoadContent()
        {
            _effect = new BasicEffect(GraphicsDevice);
            
            _textures = new Texture2D[] { 
                null,
                _game.Content.Load<Texture2D>("block_Rock_128")
            };

            generateBlocks();
            buildVertices();

            base.LoadContent();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // TODO: Add your update code here

            base.Update(gameTime);
        }

        /// <summary>
        /// Called when the DrawableGameComponent needs to be drawn. Override this method
        //  with component-specific drawing code.
        /// </summary>
        /// <param name="gameTime">Time passed since the last call to Draw.</param>
        public override void Draw(GameTime gameTime)
        {
            // Initialize vertex buffer if it was not done before
            if (SolidVertexBuffer == null)
            {
                SolidVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionTexture), SolidVertexList.Count, BufferUsage.WriteOnly);
                SolidVertexBuffer.SetData(SolidVertexList.ToArray());
            }

            // Initialize index buffer if it was not done before
            if (SolidIndexBuffer == null)
            {
                SolidIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, SolidIndexList.Count, BufferUsage.WriteOnly);
                SolidIndexBuffer.SetData(SolidIndexList.ToArray());
            }

            _effect.World = Matrix.Identity;
            _effect.View = _camera.View;
            _effect.Projection = _camera.Projection;

            _effect.VertexColorEnabled = false;
            _effect.TextureEnabled = true;
            _effect.Texture = _textures[1];

            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.SetVertexBuffer(SolidVertexBuffer);
                GraphicsDevice.Indices = SolidIndexBuffer;
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, SolidVertexList.Count, 0, SolidIndexList.Count / 3);
            }

            base.Draw(gameTime);
        }

        #endregion

        #region Blocks generation

        private void generateBlocks()
        {
            var rnd = new Random();
            for(var x = 0; x < MAP_WIDTH; x++)
                for(var z = 0; z < MAP_DEPTH; z++)
                {
                    var offset = x*MAP_WIDTH*MAP_DEPTH + z * MAP_HEIGHT;

                    var groundHeight = MAP_WATER_LEVEL + rnd.Next(0, 10)-5;

                    for (var y = 0; y < groundHeight; y++)
                    {
                        _blocks[offset + y] = new Block(BlockType.Rock);
                    }

                    for (var y = groundHeight; y < MAP_HEIGHT; y++)
                    {
                        _blocks[offset + y] = new Block(BlockType.None);
                    }
                }
        }

        #endregion

        private void buildVertices()
        {
            for(var x = 0; x < MAP_WIDTH; x++)
                for (var z = 0; z < MAP_DEPTH; z++)
                {
                    var offset = x * MAP_WIDTH * MAP_DEPTH + z * MAP_HEIGHT;

                    for (var y = 0; y < MAP_HEIGHT; y++)
                    {
                        buildVertices(_blocks[offset+y], x, y, z, 0.5f, 0.5f, 0.5f);
                    }
                }
        }

        private void buildVertices(Block block, float x, float y, float z, float scaleX, float scaleY, float scaleZ)
        {
            if (block.Type == BlockType.None)
                return;

            var vertexList = new VertexPositionTexture[] {
                new VertexPositionTexture( new Vector3(1*scaleX+x , 1*scaleY+y , -1*scaleZ+z)    , new Vector2(1, 0)),
                new VertexPositionTexture( new Vector3(1*scaleX+x , -1*scaleY+y , -1*scaleZ+z)   , new Vector2(1, 1)),
                new VertexPositionTexture( new Vector3(-1*scaleX+x , -1*scaleY+y , -1*scaleZ+z)  , new Vector2(0, 1)),
                new VertexPositionTexture( new Vector3(-1*scaleX+x , 1*scaleY+y , -1*scaleZ+z)   , new Vector2(0, 0)),

                new VertexPositionTexture( new Vector3(1*scaleX+x , 1*scaleY+y , 1*scaleZ+z)     , new Vector2(1, 0)),
                new VertexPositionTexture( new Vector3(-1*scaleX+x , 1*scaleY+y , 1*scaleZ+z)    , new Vector2(1, 1)),
                new VertexPositionTexture( new Vector3(-1*scaleX+x , -1*scaleY+y , 1*scaleZ+z)   , new Vector2(0, 1)),
                new VertexPositionTexture( new Vector3(1*scaleX+x , -1*scaleY+y , 1*scaleZ+z)    , new Vector2(0, 0)),

                new VertexPositionTexture( new Vector3(1*scaleX+x , 1*scaleY+y , -1*scaleZ+z)    , new Vector2(1, 0)),
                new VertexPositionTexture( new Vector3(1*scaleX+x , 1*scaleY+y , 1*scaleZ+z)     , new Vector2(1, 1)),
                new VertexPositionTexture( new Vector3(1*scaleX+x , -1*scaleY+y , 1*scaleZ+z)    , new Vector2(0, 1)),
                new VertexPositionTexture( new Vector3(1*scaleX+x , -1*scaleY+y , -1*scaleZ+z)   , new Vector2(0, 0)),

                new VertexPositionTexture( new Vector3(1*scaleX+x , -1*scaleY+y , -1*scaleZ+z)   , new Vector2(1, 0)),
                new VertexPositionTexture( new Vector3(1*scaleX+x , -1*scaleY+y , 1*scaleZ+z)    , new Vector2(1, 1)),
                new VertexPositionTexture( new Vector3(-1*scaleX+x , -1*scaleY+y , 1*scaleZ+z)   , new Vector2(0, 1)),
                new VertexPositionTexture( new Vector3(-1*scaleX+x , -1*scaleY+y , -1*scaleZ+z)  , new Vector2(0, 0)),

                new VertexPositionTexture( new Vector3(-1*scaleX+x , -1*scaleY+y , -1*scaleZ+z)  , new Vector2(1, 0)),
                new VertexPositionTexture( new Vector3(-1*scaleX+x , -1*scaleY+y , 1*scaleZ+z)   , new Vector2(1, 1)),
                new VertexPositionTexture( new Vector3(-1*scaleX+x , 1*scaleY+y , 1*scaleZ+z)    , new Vector2(0, 1)),
                new VertexPositionTexture( new Vector3(-1*scaleX+x , 1*scaleY+y , -1*scaleZ+z)   , new Vector2(0, 0)),

                new VertexPositionTexture( new Vector3(1*scaleX+x , 1*scaleY+y , 1*scaleZ+z)     , new Vector2(1, 0)),
                new VertexPositionTexture( new Vector3(1*scaleX+x , 1*scaleY+y , -1*scaleZ+z)    , new Vector2(1, 1)),
                new VertexPositionTexture( new Vector3(-1*scaleX+x , 1*scaleY+y , -1*scaleZ+z)   , new Vector2(0, 1)),
                new VertexPositionTexture( new Vector3(-1*scaleX+x , 1*scaleY+y , 1*scaleZ+z)    , new Vector2(0, 0))
            };

            var indexList = new int[]
            {
                0, 3, 2, 0, 2, 1,
                4, 7, 6, 4,6, 5,
                8, 11, 10,8,10, 9,
                12, 15, 14, 12, 14, 13,
                16, 19, 18,16,18, 17,
                20, 23, 22,20,22, 21
            };

            var currentVertexOffset = SolidVertexList.Count;

            foreach(var vertex in vertexList)
                SolidVertexList.Add(vertex);

            foreach(var index in indexList)
                SolidIndexList.Add(index + currentVertexOffset);
        }
    }
}
