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
        public GraphicsDeviceManager GraphicsDeviceManager { get; protected set; }
        public SpriteBatch SpriteBatch;
        public Camera Camera { get; protected set; }
        public Map Map { get; protected set; }
        public Player Player { get; protected set; }

        public Game1()
        {
            Content.RootDirectory = "Content";

            GraphicsDeviceManager = new GraphicsDeviceManager(this);

            GraphicsDeviceManager.PreferMultiSampling = true; // Turn on antialiasing 
            GraphicsDeviceManager.SynchronizeWithVerticalRetrace = true; // Turn on VSync
            GraphicsDeviceManager.GraphicsProfile = GraphicsProfile.HiDef; // Turn on best graphic settings

            GraphicsDeviceManager.IsFullScreen = false;

            if (GraphicsDeviceManager.IsFullScreen) // fullscreen
            {
                GraphicsDeviceManager.PreferredBackBufferWidth = 1920;
                GraphicsDeviceManager.PreferredBackBufferHeight = 1200;

                Window.AllowUserResizing = false;
            }
            else // window mode
            {
                GraphicsDeviceManager.PreferredBackBufferWidth = 1280;
                GraphicsDeviceManager.PreferredBackBufferHeight = 720;

                Window.AllowUserResizing = true;
            }

            GraphicsDeviceManager.ApplyChanges();
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
            var debugInfos = new DebugInfos(this);
            var hud = new HUD(this);

            Components.Add(Camera);
            Components.Add(Map);
            Components.Add(Player);
            Components.Add(debugInfos);
            Components.Add(hud);

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            SpriteBatch = new SpriteBatch(GraphicsDevice);

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
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

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
