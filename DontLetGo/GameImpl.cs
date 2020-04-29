using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Cameras;
using MLEM.Extended.Extensions;
using MLEM.Extensions;
using MLEM.Startup;
using MonoGame.Extended.Tiled;

namespace DontLetGo {
    public class GameImpl : MlemGame {

        public static GameImpl Instance { get; private set; }
        private Map map;
        private Camera camera;

        public GameImpl() {
            Instance = this;
        }

        protected override void LoadContent() {
            base.LoadContent();
            this.map = new Map(LoadContent<TiledMap>("Tiled/Level1"));
            this.camera = new Camera(this.GraphicsDevice) {
                AutoScaleWithScreen = true,
                Scale = 4
            };
        }

        protected override void DoUpdate(GameTime gameTime) {
            base.DoUpdate(gameTime);

            this.camera.ConstrainWorldBounds(Vector2.Zero, this.map.DrawSize);
            this.map.Update(gameTime);
        }

        protected override void DoDraw(GameTime gameTime) {
            this.GraphicsDevice.Clear(ColorExtensions.FromHex(0x161214));
            base.DoDraw(gameTime);

            this.SpriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, null, this.camera.ViewMatrix);
            this.map.Draw(this.SpriteBatch, gameTime, this.camera.GetVisibleRectangle().ToExtended());
            this.SpriteBatch.End();
        }

    }
}