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
using MLEM.Extended.Tiled;
using MLEM.Font;
using MLEM.Startup;
using MLEM.Ui;
using MLEM.Ui.Elements;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;
using Penumbra;
using ColorExtensions = MLEM.Extensions.ColorExtensions;

namespace DontLetGo {
    public class GameImpl : MlemGame {

        public static readonly string[] Levels = Enumerable.Range(1, 6).Select(i => "Level" + i).ToArray();
        public static GameImpl Instance { get; private set; }
        private ContentManager mapContent;
        private Map map;
        private Camera camera;
        private PenumbraComponent penumbra;
        private Player player;
        private ActiveCoroutine cutscene;

        private Group fade;
        private Group caption;
        private Paragraph trigger;

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

            this.UiSystem.AutoScaleWithScreen = true;
            this.UiSystem.Style.Font = new GenericSpriteFont(LoadContent<SpriteFont>("Fonts/Font"));
            this.UiSystem.Style.TextScale = 0.3F;

            this.fade = new Group(Anchor.TopLeft, Vector2.One, false) {
                OnDrawn = (e, time, batch, alpha) => {
                    batch.FillRectangle(e.DisplayArea.ToExtended(), Color.Black * alpha);
                }
            };
            this.UiSystem.Add("Fade", this.fade).Priority = 10;
            this.caption = new Group(Anchor.Center, Vector2.One);
            this.UiSystem.Add("Caption", this.caption).Priority = 20;
            this.trigger = new Paragraph(Anchor.TopLeft, 1, "", true) {
                OnUpdated = (e, time) => {
                    var pos = this.player.Position + new Vector2(0.5F, -0.45F);
                    var trans = this.camera.ToCameraPos(pos * this.map.TileSize);
                    trans.X -= e.DisplayArea.Width / 2;
                    trans.Y -= e.Root.Element.DisplayArea.Height;
                    e.Root.Transform = Matrix.CreateTranslation(trans.X, trans.Y, 0);
                },
                DrawAlpha = 0,
                TextScale = 0.15F
            };
            this.UiSystem.Add("Trigger", this.trigger);

            this.StartMap(Levels[5], g => g.Fade(0.01F));
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

            this.cutscene = CoroutineHandler.Start(FadeImpl());
        }

        private void DisplayCaption(string[] text, Action<GameImpl> afterDisplay = null) {
            IEnumerator<IWait> CaptionImpl() {
                foreach (var par in text) {
                    this.caption.RemoveChildren();
                    var lines = par.Split("\n");
                    var paragraphs = new Paragraph[lines.Length];
                    for (var i = 0; i < lines.Length; i++)
                        paragraphs[i] = this.caption.AddChild(new Paragraph(Anchor.AutoCenter, 1, lines[i], true));

                    this.caption.DrawAlpha = 0;
                    while (this.caption.DrawAlpha < 1) {
                        this.caption.DrawAlpha += 0.01F;
                        yield return new WaitEvent(CoroutineEvents.Update);
                    }
                    yield return new WaitSeconds(3);
                    while (this.caption.DrawAlpha > 0) {
                        this.caption.DrawAlpha -= 0.01F;
                        yield return new WaitEvent(CoroutineEvents.Update);
                    }
                    yield return new WaitSeconds(1);
                }
                afterDisplay?.Invoke(this);
            }

            this.cutscene = CoroutineHandler.Start(CaptionImpl());
        }

        public IEnumerator<IWait> DisplayTrigger(string text) {
            this.trigger.Text = text;
            while (this.trigger.DrawAlpha < 1) {
                this.trigger.DrawAlpha += 0.01F;
                yield return new WaitEvent(CoroutineEvents.Update);
            }
            yield return new WaitSeconds(2);
            while (this.trigger.DrawAlpha > 0) {
                this.trigger.DrawAlpha -= 0.01F;
                yield return new WaitEvent(CoroutineEvents.Update);
            }
        }

        public void StartMap(string name, Action<GameImpl> finished = null) {
            this.SetMap(name);
            this.DisplayCaption(this.map.Caption, g => finished?.Invoke(g));
        }

        public void SetMap(string name) {
            this.penumbra.Hulls.Clear();
            this.penumbra.Lights.Clear();
            this.trigger.DrawAlpha = 0;

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

            if (this.cutscene == null || this.cutscene.IsFinished)
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