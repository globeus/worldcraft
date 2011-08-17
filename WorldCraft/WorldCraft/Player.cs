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
    public class Player : Microsoft.Xna.Framework.DrawableGameComponent
    {
        #region Properties

        private Camera _camera;

        private const float MOVEMENTSPEED = 0.05f;
        private const float ROTATIONSPEED = 0.1f;

        private MouseState _mouseMoveState;
        private MouseState _mouseState;

        #endregion

        #region GameComponent

        public Vector3 Position
        {
            get
            {
                return _camera.Position;
            }
            set
            {
                _camera.Position = value;
            }
        }

        public Quaternion Rotation
        {
            get
            {
                return _camera.Rotation;
            }
            set
            {
                _camera.Rotation = value;
            }
        }

       

        public Player(Game game, Camera camera)
            : base(game)
        {
            _camera = camera;
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

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            #region Mouse move
            MouseState currentMouseState = Mouse.GetState();

            float mouseDX = currentMouseState.X - _mouseMoveState.X;
            float mouseDY = currentMouseState.Y - _mouseMoveState.Y;

            float yaw = 0;
            float pitch = 0;

            if (mouseDX != 0)
                yaw = -ROTATIONSPEED * (mouseDX / 50);

            if (mouseDY != 0)
                pitch = -ROTATIONSPEED * (mouseDY / 50);

            _camera.Rotate(yaw, pitch);

            _mouseMoveState = new MouseState(GraphicsDevice.Viewport.Width / 2,
                    GraphicsDevice.Viewport.Height / 2,
                    0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);

            Mouse.SetPosition((int)_mouseMoveState.X, (int)_mouseMoveState.Y);
            _mouseState = Mouse.GetState();

            #endregion

            #region Keyboard keys

            var currentKeyboardState = Keyboard.GetState();
            var translationStep = MOVEMENTSPEED * gameTime.ElapsedGameTime.Milliseconds;

            if (currentKeyboardState.IsKeyDown(Keys.Z))
            {
                _camera.Translate(Vector3.Forward, translationStep);
            }

            if (currentKeyboardState.IsKeyDown(Keys.S))
            {
                _camera.Translate(Vector3.Backward, translationStep);
            }

            if (currentKeyboardState.IsKeyDown(Keys.Q))
            {
                _camera.Translate(Vector3.Left, translationStep);
            }

            if (currentKeyboardState.IsKeyDown(Keys.D))
            {
                _camera.Translate(Vector3.Right, translationStep);
            }

            #endregion


            base.Update(gameTime);
        }

        #endregion
    }
}
