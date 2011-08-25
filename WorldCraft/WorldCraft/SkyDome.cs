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
    public class SkyDome : Microsoft.Xna.Framework.DrawableGameComponent
    {
        #region Properties

        private Game1 _game;
        private Effect _effect;
        private Model _skyDome;
        private Texture2D _cloudMap;


        #endregion

        #region GameComponent

        public SkyDome(Game1 game)
            : base(game)
        {
            _game = game;
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _effect = _game.Content.Load<Effect>("Effects/skyEffect");
            _cloudMap = _game.Content.Load<Texture2D>("Textures/cloudMap");
            _skyDome = _game.Content.Load<Model>("Models/skyDome");
            _skyDome.Meshes[0].MeshParts[0].Effect = _effect;

            base.LoadContent();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            var rs = new RasterizerState();
            rs.CullMode = CullMode.CullCounterClockwiseFace;
            GraphicsDevice.RasterizerState = rs;

            Matrix[] modelTransforms = new Matrix[_skyDome.Bones.Count];
            _skyDome.CopyAbsoluteBoneTransformsTo(modelTransforms);

            Matrix wMatrix = Matrix.CreateTranslation(0, -0.3f, 0) * Matrix.CreateScale(100) * Matrix.CreateTranslation(_game.Camera.Position);
            foreach (ModelMesh mesh in _skyDome.Meshes)
            {
                foreach (Effect currentEffect in mesh.Effects)
                {
                    Matrix worldMatrix = modelTransforms[mesh.ParentBone.Index] * wMatrix;
                    currentEffect.CurrentTechnique = currentEffect.Techniques["Textured"];
                    currentEffect.Parameters["xWorld"].SetValue(worldMatrix);
                    currentEffect.Parameters["xView"].SetValue(_game.Camera.View);
                    currentEffect.Parameters["xProjection"].SetValue(_game.Camera.Projection);
                    currentEffect.Parameters["xTexture"].SetValue(_cloudMap);
                    currentEffect.Parameters["xEnableLighting"].SetValue(false);
                }
                mesh.Draw();
            }

            rs = new RasterizerState();
            rs.CullMode = CullMode.CullClockwiseFace;
            GraphicsDevice.RasterizerState = rs;

            base.Draw(gameTime);
        }

        #endregion
    }
}
