using System;
using System.Collections.Generic;
using System.Linq;
using Coroutine;
using DontLetGo.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Cameras;
using MLEM.Extended.Extensions;
using MLEM.Startup;
using MLEM.Ui;
using MLEM.Ui.Elements;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;
using Penumbra;
using ColorExtensions = MLEM.Extensions.ColorExtensions;

namespace DontLetGo {
    public class GameImpl : MlemGame {

        public static readonly string[] Levels = Enumerable.Range(1, 2).Select(i => "Level" + i).ToArray();
        public static GameImpl Instance { get; private set; }
        private ContentManager mapContent;
        private Map map;
        private Camera camera;
        private PenumbraComponent penumbra;
        private Player player;
        private Group fade;
        private ActiveCoroutine fadeCoroutine;

        public GameImpl() {
            Instance = this;
        }

        protected override void LoadContent() {
            base.LoadContent();

            this.penumbra = new PenumbraComponent(this) {
                AmbientColor = new Color(Color.Black, 0.1F)
            };
            this.penumbra.Initialize();

            this.mapContent = new ContentManager(this.Services, this.Content.RootDirectory);
            this.camera = new Camera(this.GraphicsDevice) {
                AutoScaleWithScreen = true,
                Scale = 4
            };
            this.fade = new Group(Anchor.TopLeft, Vector2.One, false) {
                OnDrawn = (e, time, batch, alpha) => {
                    batch.FillRectangle(e.DisplayArea.ToExtended(), Color.Black * alpha);
                }
            };
            this.UiSystem.Add("Fade", this.fade).Priority = 10;

            this.SetMap(Levels[0]);
            this.Fade(0.01F);
        }

        public void Fade(float speed, Action<GameImpl> afterFade = null) {
            IEnumerator<IWait> FadeImpl() {
                var fadingIn = this.fade.DrawAlpha >= 0.5F;
                if (fadingIn)
                    speed *= -1;
                while (fadingIn ? this.fade.DrawAlpha > 0 : this.fade.DrawAlpha < 1) {
                    this.fade.DrawAlpha += speed;
                    yield return new WaitEvent(CoroutineEvents.Update);
                }
                afterFade?.Invoke(this);
            }

            this.fadeCoroutine = CoroutineHandler.Start(FadeImpl());
        }

        public void SetMap(string name) {
            this.penumbra.Hulls.Clear();
            this.penumbra.Lights.Clear();

            this.mapContent.Unload();
            this.map = new Map(name, this.mapContent.Load<TiledMap>("Tiled/" + name), this.penumbra);
            this.player = new Player(this.map) {
                Position = this.map.GetSpawnPoint()
            };
            this.map.Entities.Add(this.player);
        }

        protected override void DoUpdate(GameTime gameTime) {
            base.DoUpdate(gameTime);

            this.camera.LookingPosition = this.player.Position * this.map.TileSize;
            this.camera.ConstrainWorldBounds(Vector2.Zero, this.map.DrawSize);
            this.penumbra.Transform = this.camera.ViewMatrix;

            if (this.fadeCoroutine == null || this.fadeCoroutine.IsFinished)
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