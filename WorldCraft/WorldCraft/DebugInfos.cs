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
            _font = _game.Content.Load<SpriteFont>("ArialFont");
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
            var playerPosition = String.Format("Player position : {0}, {1}, {2}", _game.Player.Position.X, _game.Player.Position.Y, _game.Player.Position.Z);
            var playerMapPosition = String.Format("Player map position : {0}, {1}, {2}", _game.Player.MapPosition.X, _game.Player.MapPosition.Y, _game.Player.MapPosition.Z);
            var playerBlockAim = String.Format("Player block aim : {0}, {1}, {2}", _game.Player.BlockAim.X, _game.Player.BlockAim.Y, _game.Player.BlockAim.Z);

            _game.SpriteBatch.Begin();
            
            _game.SpriteBatch.DrawString(_font, playerPosition, new Vector2(20, 45), Color.White);
            _game.SpriteBatch.DrawString(_font, playerMapPosition, new Vector2(20, 65), Color.White);
            _game.SpriteBatch.DrawString(_font, playerBlockAim, new Vector2(20, 85), Color.White);

            _game.SpriteBatch.End();

            base.Draw(gameTime);

            var state = new DepthStencilState();
            state.DepthBufferEnable = true;
            _game.GraphicsDevice.DepthStencilState = state;
        }

        #endregion
    }
}
