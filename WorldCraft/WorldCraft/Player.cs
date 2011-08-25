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

        private Game1 _game;

        private const float MOVEMENT_SPEED = 0.0025f;
        private const float ROTATION_SPEED = 0.1f;
        private const float JUMP_SPEED = 0.24f;

        private const float PLAYER_HEIGHT = 1.5f;
        private const float PLAYER_WIDTH = 0.5f;
        private const float PLAYER_DEPTH = 0.5f;

        private MouseState _mouseMoveState;
        private MouseState _mouseState;

        private Vector3 _position;
        private float _yaw;
        private float _pitch;

        private float _currentGravity;
        private float _gravityElapsedTime;
        private bool _jumping;
        private bool _falling;

        private MouseState _prevMouseState;

        private BlockAccessor _blockAccessor;

        public Vector3 Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
                _game.Camera.Position = value + new Vector3(0, PLAYER_HEIGHT, 0);
            }
        }

        public Quaternion Rotation
        {
            get
            {
                return _game.Camera.Rotation;
            }
            set
            {
                _game.Camera.Rotation = value;
            }
        }

        public BoundingBox BoundingBox
        {
            get
            {
                return new BoundingBox(new Vector3(-PLAYER_WIDTH / 2, 0, -PLAYER_DEPTH / 2) + _position, new Vector3(PLAYER_WIDTH / 2, PLAYER_HEIGHT, PLAYER_DEPTH / 2) + _position);
            }
        }

        public Vector3 MapPosition
        {
            get
            {
                return new Vector3((int)Position.X, (int)Position.Y, (int)Position.Z);
            }
        }

        public Vector3 BlockAim
        {
            get
            {
                var headDirection = Vector3.Transform(Vector3.Forward, Matrix.CreateFromQuaternion(Rotation));

                var curPos = _game.Camera.Position;

                for (var i = 0; i < 4000; i++)
                {
                    curPos += headDirection * 0.005f;

                    if (_blockAccessor.MoveTo((int)curPos.X, (int)curPos.Y, (int)curPos.Z).IsSelectable)
                    {
                        return new Vector3((int)curPos.X, (int)curPos.Y, (int)curPos.Z);
                    }
                }

                return Vector3.Zero;
            }
        }

        #endregion

        #region GameComponent



        public Player(Game1 game)
            : base(game)
        {
            _game = game;
            _yaw = _pitch = 0;
            _position = Vector3.Zero;
            _jumping = false;
            _currentGravity = Map.AIR_GRAVITY;

            _blockAccessor = new BlockAccessor(_game.Map);
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here

            Position = new Vector3(10, 50, 10);
            Rotate(45, 0);

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

            Vector3 oldPosition = Vector3.Zero + Position;
            Vector3 newPosition = Vector3.Zero + Position;

            if (mouseDX != 0)
                yaw = -ROTATION_SPEED * (mouseDX / 50);

            if (mouseDY != 0)
                pitch = -ROTATION_SPEED * (mouseDY / 50);

            Rotate(yaw, pitch);

            _mouseMoveState = new MouseState(GraphicsDevice.Viewport.Width / 2,
                    GraphicsDevice.Viewport.Height / 2,
                    0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);

            Mouse.SetPosition((int)_mouseMoveState.X, (int)_mouseMoveState.Y);
            _mouseState = Mouse.GetState();

            #endregion

            #region Keyboard movement keys

            int mapX = (int)newPosition.X;
            int mapY = (int)newPosition.Y;
            int mapZ = (int)newPosition.Z;

            _blockAccessor.MoveTo(mapX, mapY, mapZ);
            
            float moveSpeed;
            float gravity;
            float jumpSpeed;

            if (_blockAccessor.IsLiquid)
                gravity = Map.LIQUID_GRAVITY;
            else
                gravity = Map.AIR_GRAVITY;

            moveSpeed = gravity * MOVEMENT_SPEED;

            var currentKeyboardState = Keyboard.GetState();
            var translationStep = _currentGravity * moveSpeed * gameTime.ElapsedGameTime.Milliseconds;

            var moveTranslation = new Vector3(0, 0, 0);

            if (currentKeyboardState.IsKeyDown(Keys.Z))
                moveTranslation += Vector3.Forward;

            if (currentKeyboardState.IsKeyDown(Keys.S))
                moveTranslation += Vector3.Backward;

            if (currentKeyboardState.IsKeyDown(Keys.Q))
                moveTranslation += Vector3.Left;

            if (currentKeyboardState.IsKeyDown(Keys.D))
                moveTranslation += Vector3.Right;

            var tempPosition = Vector3.Transform(moveTranslation, Matrix.CreateFromQuaternion(Rotation));
            tempPosition.Y = 0;

            if (tempPosition != Vector3.Zero)
            {
                tempPosition.Normalize();
                newPosition += tempPosition * translationStep;
            }

            #endregion

            #region Gravity

            if (currentKeyboardState.IsKeyDown(Keys.Space) && ((!_jumping && !_falling) || _blockAccessor.IsLiquid) )                
                _jumping = true;

            if (_jumping)
                jumpSpeed = gravity * JUMP_SPEED;
            else
                jumpSpeed = 0;

            if (_jumping || _falling)
            {
                _gravityElapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
             
                if (_jumping && _gravityElapsedTime > 0)
                {
                    float delta = (jumpSpeed * _gravityElapsedTime) - (gravity * _gravityElapsedTime * _gravityElapsedTime * 0.5f);

                    newPosition.Y += delta;

                    if (delta < 0.001)
                    {
                        _jumping = false;
                        _falling = true;

                        _gravityElapsedTime = 0;
                    }
                }
                else if (_falling && _gravityElapsedTime > 0)
                {
                    float delta = _currentGravity * _gravityElapsedTime * _gravityElapsedTime * 0.5f;
                    newPosition.Y -= delta;
                }
            }
            else
            {
                _jumping = false;
                _gravityElapsedTime = 0;
            }

            #endregion

            #region Collisions

            mapX = (int)newPosition.X;
            mapY = (int)newPosition.Y;
            mapZ = (int)newPosition.Z;

            var boundingBox = new BoundingBox(new Vector3(-0.25f, 0, -0.25f) + newPosition, new Vector3(0.25f, 1.5f, 0.25f) + newPosition); ;

            // Checking Y collisions first
            if (_blockAccessor.MoveTo(mapX, mapY, mapZ).IsSolid)
            {
                var bbox = _blockAccessor.BoundingBox;

                if (bbox.Intersects(boundingBox))
                {
                    newPosition.Y = mapY + 1;
                    _falling = false;
                }
            }
            else
                _falling = newPosition.Y > 0 ? true : false;

            // a new Y coordinate could have been set so, let's set mapY again for horizontal checks
            mapY = (int)newPosition.Y;

            if (_blockAccessor.MoveTo(mapX, mapY, mapZ).Left.IsSolid)
            {
                var bbox = _blockAccessor.BoundingBox;

                if (bbox.Intersects(boundingBox))
                    newPosition.X = mapX + 0.25f;
            }

            if (_blockAccessor.MoveTo(mapX, mapY, mapZ).Right.IsSolid)
            {
                var bbox = _blockAccessor.BoundingBox;

                if (bbox.Intersects(boundingBox))
                    newPosition.X = mapX + 0.75f;
            }

            if (_blockAccessor.MoveTo(mapX, mapY, mapZ).Forward.IsSolid)
            {
                var bbox = _blockAccessor.BoundingBox;

                if (bbox.Intersects(boundingBox))
                    newPosition.Z = mapZ + 0.75f;
            }

            if (_blockAccessor.MoveTo(mapX, mapY, mapZ).Backward.IsSolid)
            {
                var bbox = _blockAccessor.BoundingBox;

                if (bbox.Intersects(boundingBox))
                    newPosition.Z = mapZ + 0.25f;
            }

            if (_blockAccessor.MoveTo(mapX, mapY, mapZ).Up.IsSolid)
            {
                var bbox = _blockAccessor.BoundingBox;

                if (bbox.Intersects(boundingBox))
                    _jumping = false;                
            }

            #endregion

            Position = newPosition;

            #region Block action

            if(currentMouseState.LeftButton == ButtonState.Released 
                && _prevMouseState != null 
                && _prevMouseState.LeftButton == ButtonState.Pressed) 
            {
                var blockAim = BlockAim;
                _blockAccessor
                    .MoveTo((int)blockAim.X, (int)blockAim.Y, (int)blockAim.Z)
                    .ReplaceWithBlock(new Block(BlockType.None));
            }

            _prevMouseState = currentMouseState;

            #endregion

            base.Update(gameTime);
        }

        public void Rotate(float yaw, float pitch)
        {
            _yaw += yaw;
            _pitch += pitch;

            // Locking camera rotation vertically between +/- 180 degrees
            if (_pitch < -1.55f)
                _pitch = -1.55f;
            else if (_pitch > 1.55f)
                _pitch = 1.55f;
            // End of locking

            Rotation = Quaternion.CreateFromYawPitchRoll(_yaw, _pitch, 0);
        }

        #endregion
    }
}
