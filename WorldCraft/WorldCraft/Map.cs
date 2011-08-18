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

        private Game1 _game;
        private Chunk[] _chunks;

        public const short NUM_CHUNKS_WIDTH = 4;
        public const short NUM_CHUNKS_DEPTH = 4;
        public const short NUM_CHUNKS_HEIGHT = 1;
        public const short MAP_WATER_HEIGHT = 25;
        
        public VertexBuffer SolidVertexBuffer { get; protected set; }
        public IndexBuffer SolidIndexBuffer { get; protected set; }
        public List<int> SolidIndexList { get; protected set; }
        public List<VertexPositionNormalTexture> SolidVertexList { get; protected set; }

        public int Seed { get; protected set; }
        public Texture2D Texture { get; set; }

        #endregion

        #region GameComponent

        public Map(Game1 game)
            : base(game)
        {
            _game = game;
            
            Seed = new Random().Next();

            SolidVertexList = new List<VertexPositionNormalTexture>();
            SolidIndexList = new List<int>();

            _chunks = new Chunk[NUM_CHUNKS_WIDTH * NUM_CHUNKS_DEPTH * NUM_CHUNKS_HEIGHT];
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
            Texture = _game.Content.Load<Texture2D>("block_Rock_128");

            generateChunks();

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
            base.Draw(gameTime);
        }

        #endregion

        #region Chunks generation

        private void generateChunks()
        {
            for(var x = 0; x < NUM_CHUNKS_WIDTH; x++)
                for(var z = 0; z < NUM_CHUNKS_DEPTH; z++)
                {
                    var offset = x * NUM_CHUNKS_DEPTH * NUM_CHUNKS_HEIGHT + z * NUM_CHUNKS_HEIGHT;

                    for (var y = 0; y < NUM_CHUNKS_HEIGHT; y++)
                    {
                        _chunks[offset + y] = new Chunk(_game, new Vector3(x, y, z));
                        _game.Components.Add(_chunks[offset + y]);
                    }
                }
        }

        #endregion
    }
}
