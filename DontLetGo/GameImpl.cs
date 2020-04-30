using DontLetGo.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Cameras;
using MLEM.Extended.Extensions;
using MLEM.Extensions;
using MLEM.Startup;
using MonoGame.Extended.Tiled;
using Penumbra;

namespace DontLetGo {
    public class GameImpl : MlemGame {

        public static GameImpl Instance { get; private set; }
        private Map map;
        private Camera camera;
        private PenumbraComponent penumbra;
        private Player player;

        public GameImpl() {
            Instance = this;
        }

        protected override void LoadContent() {
            base.LoadContent();

            this.penumbra = new PenumbraComponent(this) {
                AmbientColor = new Color(Color.Black, 0.1F)
            };
            this.penumbra.Initialize();

            this.map = new Map(LoadContent<TiledMap>("Tiled/Level2"), this.penumbra);
            this.camera = new Camera(this.GraphicsDevice) {
                AutoScaleWithScreen = true,
                Scale = 4
            };

            var light = new PointLight {
                ShadowType = ShadowType.Occluded,
                Intensity = 0.8F
            };
            this.penumbra.Lights.Add(light);
            this.player = new Player(this.map, light) {
                Position = new Vector2(9, 18)
            };
            this.map.Entities.Add(this.player);
        }

        protected override void DoUpdate(GameTime gameTime) {
            base.DoUpdate(gameTime);

            this.camera.LookingPosition = this.player.Position * this.map.TileSize;
            this.camera.ConstrainWorldBounds(Vector2.Zero, this.map.DrawSize);
            this.penumbra.Transform = this.camera.ViewMatrix;

            this.map.Update(gameTime);
        }

        protected override void DoDraw(GameTime gameTime) {
            this.penumbra.BeginDraw();

            this.GraphicsDevice.Clear(ColorExtensions.FromHex(0x161214));

            this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, null, SamplerState.PointClamp, null, null, null, this.camera.ViewMatrix);
            this.map.Draw(this.SpriteBatch, gameTime, this.camera.GetVisibleRectangle().ToExtended());
            this.SpriteBatch.End();

            this.penumbra.Draw(gameTime);
            base.DoDraw(gameTime);
        }

    }
}