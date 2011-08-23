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
    public class Camera : Microsoft.Xna.Framework.DrawableGameComponent
    {
        #region Properties

        private Vector3 _position;
        private Quaternion _rotation;

        private Matrix _view;
        private bool _isViewDirty;

        public Vector3 Position { 
            get { return _position; } 
            set { _position = value; _isViewDirty = true; } 
        }
        public Quaternion Rotation { 
            get { return _rotation; } 
            set { _rotation = value; _isViewDirty = true; } 
        }
        public Matrix Projection { get; protected set; }
        public Matrix View
        {
            get
            {
                if (_isViewDirty)
                {
                    _view = Matrix.Invert(Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateTranslation(Position));
                    _isViewDirty = false;
                }

                return _view;
            }
        }    

        #endregion

        public bool InViewFrustrum(BoundingBox boundingBox)
        {
            return new BoundingFrustum(View * Projection).Intersects(boundingBox);
        }

        #region GameComponent

        public Camera(Game1 game)
            : base(game)
        {
            _isViewDirty = true;

            Position = Vector3.Zero;
            Rotation = Quaternion.Identity;
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio , 0.1f, 1000.0f);

        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        #endregion

        #region Actions
        /// <summary>
        /// Pans the camera along an axis
        /// </summary>
        /// <param name="axis">A Vector3 containing the axis along which to translate the camera.</param>
        /// <param name="distance">The amount distance to pan the camera. Units are assumed to be world units.</param>
        public void Translate(Vector3 axis, float fDistance)
        {
            var distance = axis * fDistance;
            Position += Vector3.Transform(distance, Matrix.CreateFromQuaternion(Rotation));

            _isViewDirty = true;
        }

        /// <summary>
        /// Pans the camera along the X, Y, and Z directions
        /// </summary>
        /// <param name="vecDistance">The amount of X, Y, and Z distance to pan the camera. Units are assumed to be world units.</param>
        public void Translate(Vector3 distance)
        {
            Position += Vector3.Transform(distance, Matrix.CreateFromQuaternion(Rotation));

            _isViewDirty = true;
        }

        /// <summary>
        /// Rotates the camera along an arbitrary axis, relative to the position of the camera, not the world.
        /// </summary>
        /// <param name="axis">A Vector3 containing the axis along which to rotate the camera.</param>
        /// <param name="fDegrees">The number of degrees to rotate the camera by.</param>
        public void Rotate(Vector3 axis, float fDegrees)
        {
            axis = Vector3.Transform(axis, Matrix.CreateFromQuaternion(Rotation));

            //Because we use an inverted matrix, we flip the sign on the degrees or the camera will go in
            //the opposite direction that we expect.
            Rotation = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(axis, MathHelper.ToRadians(-fDegrees)) * Rotation);

            _isViewDirty = true;
        }

        #endregion
    }
}
