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

        private Camera _camera;
        private BasicEffect _effect; 
        
        public VertexBuffer SolidVertexBuffer { get; protected set; }
        public IndexBuffer SolidIndexBuffer { get; protected set; }
        public List<int> SolidIndexList { get; protected set; }
        public List<VertexPositionColor> SolidVertexList { get; protected set; }

        #endregion

        #region GameComponent

        public Map(Game game, Camera camera)
            : base(game)
        {
            _camera = camera;

            SolidVertexList = new List<VertexPositionColor>();
            SolidIndexList = new List<int>();
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
            this._effect = new BasicEffect(GraphicsDevice);
            this._effect.VertexColorEnabled = true;

            var rnd = new Random();
            for (var x = 0; x < 24; x += 2)
            {
                for (var z = 0; z < 24; z += 2)
                {
                    for (var y = 0; y < rnd.Next(0, 24); y += 2)
                        buildVertices(x, y, z);
                }
            }

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
            this._effect.World = Matrix.Identity;
            this._effect.View = this._camera.View;
            this._effect.Projection = this._camera.Projection;

            // Initialize vertex buffer if it was not done before
            if (SolidVertexBuffer == null)
            {
                SolidVertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), SolidVertexList.Count, BufferUsage.WriteOnly);
                SolidVertexBuffer.SetData(SolidVertexList.ToArray());
            }

            // Initialize index buffer if it was not done before
            if (SolidIndexBuffer == null)
            {
                SolidIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, SolidIndexList.Count, BufferUsage.WriteOnly);
                SolidIndexBuffer.SetData(SolidIndexList.ToArray());
            }

            foreach (EffectPass pass in this._effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.SetVertexBuffer(SolidVertexBuffer);
                GraphicsDevice.Indices = SolidIndexBuffer;
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, SolidVertexList.Count, 0, SolidIndexList.Count / 3);
            }

            base.Draw(gameTime);
        }

        #endregion

        private void buildVertices(float x, float y, float z)
        {
            var vertexList = new VertexPositionColor[] {
                new VertexPositionColor( new Vector3(1+x , 1+y , -1+z)    ,Color.Red),
                new VertexPositionColor( new Vector3(1+x , -1+y , -1+z)   ,Color.Red),
                new VertexPositionColor( new Vector3(-1+x , -1+y , -1+z)  ,Color.Red),
                new VertexPositionColor( new Vector3(-1+x , 1+y , -1+z)   ,Color.Red),

                new VertexPositionColor( new Vector3(1+x , 1+y , 1+z)     ,Color.Green),
                new VertexPositionColor( new Vector3(-1+x , 1+y , 1+z)    ,Color.Green),
                new VertexPositionColor( new Vector3(-1+x , -1+y , 1+z)   ,Color.Green),
                new VertexPositionColor( new Vector3(1+x , -1+y , 1+z)    ,Color.Green),

                new VertexPositionColor( new Vector3(1+x , 1+y , -1+z)    ,Color.Blue),
                new VertexPositionColor( new Vector3(1+x , 1+y , 1+z)     ,Color.Blue),
                new VertexPositionColor( new Vector3(1+x , -1+y , 1+z)    ,Color.Blue),
                new VertexPositionColor( new Vector3(1+x , -1+y , -1+z)   ,Color.Blue),

                new VertexPositionColor( new Vector3(1+x , -1+y , -1+z)   ,Color.Orange),
                new VertexPositionColor( new Vector3(1+x , -1+y , 1+z)    ,Color.Orange),
                new VertexPositionColor( new Vector3(-1+x , -1+y , 1+z)   ,Color.Orange),
                new VertexPositionColor( new Vector3(-1+x , -1+y , -1+z)  ,Color.Orange),

                new VertexPositionColor( new Vector3(-1+x , -1+y , -1+z)  ,Color.Purple),
                new VertexPositionColor( new Vector3(-1+x , -1+y , 1+z)   ,Color.Purple),
                new VertexPositionColor( new Vector3(-1+x , 1+y , 1+z)    ,Color.Purple),
                new VertexPositionColor( new Vector3(-1+x , 1+y , -1+z)   ,Color.Purple),

                new VertexPositionColor( new Vector3(1+x , 1+y , 1+z)     ,Color.Yellow),
                new VertexPositionColor( new Vector3(1+x , 1+y , -1+z)    ,Color.Yellow),
                new VertexPositionColor( new Vector3(-1+x , 1+y , -1+z)   ,Color.Yellow),
                new VertexPositionColor( new Vector3(-1+x , 1+y , 1+z)    ,Color.Yellow)
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
