using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Coroutine;
using DontLetGo.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using MLEM.Cameras;
using MLEM.Extended.Extensions;
using MLEM.Extended.Tiled;
using MLEM.Extensions;
using MLEM.Font;
using MLEM.Startup;
using MLEM.Textures;
using MLEM.Ui;
using MLEM.Ui.Elements;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;
using Penumbra;
using ColorExtensions = MLEM.Extensions.ColorExtensions;

namespace DontLetGo {
    public class GameImpl : MlemGame {

        public static readonly string[] Levels = Enumerable.Range(1, 7).Select(i => "Level" + i).ToArray();
        public static GameImpl Instance { get; private set; }
        private ContentManager mapContent;
        private Map map;
        private Camera camera;
        private PenumbraComponent penumbra;
        private Player player;
        private ActiveCoroutine cutscene;
        private string savedLevel;

        private Group fade;
        private Group caption;
        private Paragraph trigger;
        private Group mainMenu;

        public GameImpl() {
            Instance = this;
        }

        protected override void LoadContent() {
            base.LoadContent();
            this.savedLevel = Load();

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
            this.UiSystem.Style.ButtonTexture = this.SpriteBatch.GenerateTexture(Color.Transparent, Color.Transparent);
            this.UiSystem.OnSelectedElementDrawn = (e, time, batch, alpha) => {
                batch.FillRectangle(e.DisplayArea.ToExtended(), ColorExtensions.FromHex(0x493443));
            };
            var controls = this.UiSystem.Controls;
            controls.HandleMouse = controls.HandleTouch = false;
            UiControls.AddButtons(ref controls.LeftButtons, Keys.Left);
            UiControls.AddButtons(ref controls.RightButtons, Keys.Right);
            UiControls.AddButtons(ref controls.UpButtons, Keys.Up);
            UiControls.AddButtons(ref controls.DownButtons, Keys.Down);

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
                    if (this.map == null)
                        return;
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
            this.mainMenu = new Group(Anchor.TopLeft, Vector2.One, false);
            this.UiSystem.Add("Menu", this.mainMenu);
            var center = this.mainMenu.AddChild(new Group(Anchor.Center, Vector2.One));
            center.AddChild(new Paragraph(Anchor.AutoCenter, 1, "Don't Wake Up", true) {TextScale = 0.6F});
            center.AddChild(new VerticalSpace(100));
            center.AddChild(new Button(Anchor.AutoCenter, new Vector2(400, 70), "Start") {
                Padding = new Vector2(5),
                OnPressed = e => {
                    if (this.cutscene != null && !this.cutscene.IsFinished)
                        return;
                    this.Fade(0.007F, g => {
                        this.CloseMainMenu(g2 => {
                            g2.StartMap(Levels[0], g3 => g3.Fade(0.01F));
                        });
                    });
                }
            });
            center.AddChild(new Button(Anchor.AutoCenter, new Vector2(400, 70), "Continue") {
                Padding = new Vector2(5),
                OnUpdated = (e, time) => e.IsHidden = this.savedLevel == null,
                OnPressed = e => {
                    if (this.cutscene != null && !this.cutscene.IsFinished)
                        return;
                    this.Fade(0.007F, g => {
                        this.CloseMainMenu(g2 => {
                            this.StartMap(this.savedLevel, g3 => g3.Fade(0.01F));
                        });
                    });
                }
            });
            center.Root.SelectElement(center.GetChildren(c => c.CanBeSelected).First(), true);
            this.mainMenu.AddChild(new Paragraph(Anchor.BottomLeft, 1, "A small game by Ellpeck") {TextScale = 0.2F, Padding = new Vector2(5)});

            this.OpenMainMenu();
            this.Fade(0.01F);
        }

        private void OpenMainMenu() {
            this.mainMenu.IsHidden = false;

            SoundEffect.MasterVolume = 0.25F;
            MediaPlayer.Volume = 0.25F;
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(LoadContent<Song>("Music/MenuTheme"));
        }

        private void CloseMainMenu(Action<GameImpl> after = null) {
            this.mainMenu.IsHidden = true;

            IEnumerator<IWait> FadeMusic() {
                while (MediaPlayer.Volume > 0) {
                    MediaPlayer.Volume -= 0.007F;
                    yield return new WaitEvent(CoroutineEvents.Update);
                }
                after?.Invoke(this);
            }

            CoroutineHandler.Start(FadeMusic());
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
            Save(name);
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

            if (this.map != null) {
                this.camera.LookingPosition = this.player.Position * this.map.TileSize;
                this.camera.ConstrainWorldBounds(Vector2.Zero, this.map.DrawSize);
                this.penumbra.Transform = this.camera.ViewMatrix;

                if (this.cutscene == null || this.cutscene.IsFinished)
                    this.map.Update(gameTime);
            }
        }

        protected override void DoDraw(GameTime gameTime) {
            this.penumbra.BeginDraw();

            this.GraphicsDevice.Clear(ColorExtensions.FromHex(0x161214));

            if (this.map != null) {
                this.SpriteBatch.Begin(SpriteSortMode.FrontToBack, null, SamplerState.PointClamp, null, null, null, this.camera.ViewMatrix);
                this.map.Draw(this.SpriteBatch, gameTime, this.camera.GetVisibleRectangle().ToExtended());
                this.SpriteBatch.End();
            }

            this.penumbra.Draw(gameTime);
            base.DoDraw(gameTime);
        }

        private static string Load() {
            var file = GetSaveFile();
            if (!file.Exists)
                return null;
            using var stream = file.OpenText();
            return stream.ReadToEnd();
        }

        public static void Save(string level) {
            var file = GetSaveFile();
            if (file.Exists)
                file.Delete();
            using var stream = file.CreateText();
            stream.Write(level);
        }

        private static FileInfo GetSaveFile() {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var file = new FileInfo(Path.Combine(appData, "Don't Wake Up", "Save"));
            if (!file.Directory.Exists)
                file.Directory.Create();
            return file;
        }

    }
}