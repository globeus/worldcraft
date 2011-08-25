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
    public class DebugInfos : Microsoft.Xna.Framework.DrawableGameComponent
    {
        #region Properties
        private Game1 _game;
        private SpriteFont _font;
        #endregion

        #region GameComponent

        public DebugInfos(Game1 game)
            : base(game)
        {
            _game = game;
            _font = _game.Content.Load<SpriteFont>("Fonts/main");
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            var strings = new List<String>();
            strings.Add(String.Format("Num blocks / vertices : {0} / {1}", _game.Map.NumBlocks, _game.Map.NumVertices));
            strings.Add(String.Format("Player position : {0}, {1}, {2} ({3}, {4}, {5})",
                _game.Player.MapPosition.X, _game.Player.MapPosition.Y, _game.Player.MapPosition.Z, 
                _game.Player.Position.X, _game.Player.Position.Y, _game.Player.Position.Z));
            strings.Add(String.Format("Player block aim : {0}, {1}, {2}", _game.Player.BlockAim.X, _game.Player.BlockAim.Y, _game.Player.BlockAim.Z));

            _game.SpriteBatch.Begin();

            var height = 45;

            foreach(var str in strings)
            {
                _game.SpriteBatch.DrawString(_font, str, new Vector2(20, height), Color.White);

                height += 20;
            }

            _game.SpriteBatch.End();

            base.Draw(gameTime);

            var state = new DepthStencilState();
            state.DepthBufferEnable = true;
            _game.GraphicsDevice.DepthStencilState = state;
        }

        #endregion
    }
}
