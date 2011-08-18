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
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public Camera Camera { get; protected set; }
        public Map Map { get; protected set; }
        public Player Player { get; protected set; }

        public Game1()
        {
            Content.RootDirectory = "Content";

            _graphics = new GraphicsDeviceManager(this);

            _graphics.PreferMultiSampling = true; // Turn on antialiasing 
            _graphics.SynchronizeWithVerticalRetrace = true; // Turn on VSync
            _graphics.GraphicsProfile = GraphicsProfile.HiDef; // Turn on best graphic settings

            _graphics.IsFullScreen = false;

            if (_graphics.IsFullScreen) // fullscreen
            {
                _graphics.PreferredBackBufferWidth = 1920;
                _graphics.PreferredBackBufferHeight = 1200;

                Window.AllowUserResizing = false;
            }
            else // window mode
            {
                _graphics.PreferredBackBufferWidth = 1280;
                _graphics.PreferredBackBufferHeight = 720;

                Window.AllowUserResizing = true;
            }

            _graphics.ApplyChanges();
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            Camera = new Camera(this);
            Map = new Map(this);
            Player = new Player(this);

            Components.Add(Camera);
            Components.Add(Map);
            Components.Add(Player);
            
            Player.Position = new Vector3(4, 4,-4);


            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            #region DEBUG : Change CullMode to None to debug primitives
            
            var state = new RasterizerState();
            state.CullMode = CullMode.CullCounterClockwiseFace;
            GraphicsDevice.RasterizerState = state;

            #endregion

            GraphicsDevice.Clear(Color.Black);

            base.Draw(gameTime);
        }
    }
}
