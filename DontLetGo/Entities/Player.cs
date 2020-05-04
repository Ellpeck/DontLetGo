using System;
using System.Collections.Generic;
using System.Linq;
using Coroutine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MLEM.Animations;
using MLEM.Extended.Tiled;
using MLEM.Extensions;
using MLEM.Misc;
using MLEM.Startup;
using MLEM.Textures;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;
using Penumbra;

namespace DontLetGo.Entities {
    public class Player : Entity {

        public Direction2 Direction;

        private readonly Light light;
        private readonly SpriteAnimationGroup animation;

        private Vector2 lastPosition;
        private float walkPercentage;
        private readonly List<ActiveCoroutine> activationCutscenes = new List<ActiveCoroutine>();

        public Player(Map map) : base(map) {
            this.light = new PointLight {
                ShadowType = ShadowType.Occluded,
                Intensity = 0.8F
            };
            map.Penumbra.Lights.Add(this.light);

            var tex = new UniformTextureAtlas(MlemGame.LoadContent<Texture2D>("Textures/Player"), 4, 4);
            this.animation = new SpriteAnimationGroup();
            this.animation.Add(new SpriteAnimation(1, tex[0, 0]), () => this.Direction == Direction2.Down);
            this.animation.Add(new SpriteAnimation(1, tex[1, 0]), () => this.Direction == Direction2.Up);
            this.animation.Add(new SpriteAnimation(1, tex[2, 0]), () => this.Direction == Direction2.Left);
            this.animation.Add(new SpriteAnimation(1, tex[3, 0]), () => this.Direction == Direction2.Right);
            this.animation.Add(new SpriteAnimation(0.15F, tex[0, 0], tex[0, 1], tex[0, 2], tex[0, 3]), () => this.walkPercentage > 0 && this.Direction == Direction2.Down, 10);
            this.animation.Add(new SpriteAnimation(0.15F, tex[1, 0], tex[1, 1], tex[1, 2], tex[1, 3]), () => this.walkPercentage > 0 && this.Direction == Direction2.Up, 10);
            this.animation.Add(new SpriteAnimation(0.15F, tex[2, 0], tex[2, 1], tex[2, 2], tex[2, 3]), () => this.walkPercentage > 0 && this.Direction == Direction2.Left, 10);
            this.animation.Add(new SpriteAnimation(0.15F, tex[3, 0], tex[3, 1], tex[3, 2], tex[3, 3]), () => this.walkPercentage > 0 && this.Direction == Direction2.Right, 10);
        }

        public override void Update(GameTime time) {
            if (MlemGame.Input.IsAnyPressed(Keys.R, Buttons.Start)) {
                MlemGame.LoadContent<SoundEffect>("Sounds/Restart").Play();
                GameImpl.Instance.Fade(0.01F, g => {
                    g.SetMap(this.Map.Name);
                    g.Fade(0.01F);
                });
                return;
            }

            if (this.walkPercentage > 0) {
                var next = Math.Max(0, this.walkPercentage - time.GetElapsedSeconds() * 2.5F);
                if (this.walkPercentage > 0.5F != next >= 0.5F)
                    this.OnWalkedOffOf(this.lastPosition.ToPoint());
                this.walkPercentage = next;
                this.Position = Vector2.Lerp(this.lastPosition, this.lastPosition + this.Direction.Offset().ToVector2(), 1 - this.walkPercentage);
                if (this.walkPercentage <= 0)
                    this.OnWalkedOnto(this.Position.ToPoint());
            }

            if (this.walkPercentage <= 0) {
                this.lastPosition = this.Position;
                if (MlemGame.Input.IsAnyDown(Keys.Up, Buttons.DPadUp, Buttons.LeftThumbstickUp)) {
                    this.Direction = Direction2.Up;
                } else if (MlemGame.Input.IsAnyDown(Keys.Down, Buttons.DPadDown, Buttons.LeftThumbstickDown)) {
                    this.Direction = Direction2.Down;
                } else if (MlemGame.Input.IsAnyDown(Keys.Left, Buttons.DPadLeft, Buttons.LeftThumbstickLeft)) {
                    this.Direction = Direction2.Left;
                } else if (MlemGame.Input.IsAnyDown(Keys.Right, Buttons.DPadRight, Buttons.LeftThumbstickRight)) {
                    this.Direction = Direction2.Right;
                } else if (MlemGame.Input.IsAnyDown(Keys.Space, Buttons.A)) {
                    this.activationCutscenes.RemoveAll(c => c.IsFinished);
                    if (this.activationCutscenes.Count <= 0)
                        this.OnActivated(this.Position.ToPoint() + this.Direction.Offset());
                    return;
                } else {
                    return;
                }
                var (nextX, nextY) = this.Position.ToPoint() + this.Direction.Offset();
                var nextTile = this.Map.GetTile(nextX, nextY).GetTilesetTile(this.Map.Tiles);
                if (nextTile == null || !nextTile.Properties.GetBool("Walkable"))
                    return;
                this.walkPercentage = 1;
            }
        }

        private void OnActivated(Point pos) {
            foreach (var layer in this.Map.Tiles.TileLayers) {
                var tile = layer.GetTile(pos.X, pos.Y);
                if (tile.IsBlank)
                    continue;
                var tilesetTile = tile.GetTilesetTile(this.Map.Tiles);

                // switches
                if (tilesetTile.Properties.GetBool("Switch")) {
                    foreach (var other in this.Map.Tiles.TileLayers) {
                        if (other.Properties.GetInt("ActivatorX") != pos.X || other.Properties.GetInt("ActivatorY") != pos.Y)
                            continue;
                        var activated = other.Properties.GetBool("Activated");
                        var co = activated ? this.Map.RemoveLayerFromGround(other) : this.Map.AddLayerToGround(other);
                        this.activationCutscenes.Add(CoroutineHandler.Start(co));
                        other.Properties["Activated"] = (!activated).ToString();
                    }
                    var active = tilesetTile.Properties.GetInt("ActiveState");
                    if (active > 0)
                        this.Map.SetTile(pos.X, pos.Y, active, layer.Name);
                    MlemGame.LoadContent<SoundEffect>("Sounds/Button").Play();
                }
            }
        }

        private void OnWalkedOffOf(Point pos) {
            var currTile = this.Map.GetTile(pos.X, pos.Y);
            if (currTile.GlobalIdentifier == 1) {
                this.Map.SetTile(pos.X, pos.Y, 9);
            } else if (currTile.GlobalIdentifier == 9) {
                this.Map.SetTile(pos.X, pos.Y, 0);
                this.Map.Entities.Add(new FallingTile(this.Map, currTile, pos.ToVector2()));
            }
        }

        public void OnWalkedOnto(Point pos) {
            var stuck = Direction2Helper.Adjacent.All(dir => {
                var offset = pos + dir.Offset();
                var tile = this.Map.GetTile(offset.X, offset.Y);
                return tile.IsBlank || !tile.GetTilesetTile(this.Map.Tiles).Properties.GetBool("Walkable");
            });
            if (stuck)
                CoroutineHandler.Start(GameImpl.Instance.DisplayTrigger("R to restart"));

            foreach (var obj in this.Map.Tiles.GetObjects("Trigger", false, true)) {
                if (obj.Properties.GetBool("Triggered"))
                    continue;
                if (!obj.GetArea(this.Map.Tiles).Contains(pos.ToVector2() + new Vector2(0.5F)))
                    continue;
                var content = obj.Properties.Get("Content");
                CoroutineHandler.Start(GameImpl.Instance.DisplayTrigger(content));
                obj.Properties.Add("Triggered", true.ToString());
                break;
            }

            foreach (var layer in this.Map.Tiles.TileLayers) {
                var tile = layer.GetTile(pos.X, pos.Y);
                if (tile.IsBlank)
                    continue;
                var tilesetTile = tile.GetTilesetTile(this.Map.Tiles);

                // buttons
                if (tilesetTile.Properties.GetBool("Activator")) {
                    foreach (var other in this.Map.Tiles.TileLayers) {
                        if (other.Properties.GetBool("Activated"))
                            continue;
                        if (other.Properties.GetInt("ActivatorX") != pos.X || other.Properties.GetInt("ActivatorY") != pos.Y)
                            continue;
                        CoroutineHandler.Start(this.Map.AddLayerToGround(other));
                        other.Properties["Activated"] = true.ToString();
                        break;
                    }
                    var active = tilesetTile.Properties.GetInt("ActiveState");
                    if (active > 0)
                        this.Map.SetTile(pos.X, pos.Y, active, layer.Name);
                    MlemGame.LoadContent<SoundEffect>("Sounds/Button").Play();
                    continue;
                }

                // bed
                if (tilesetTile.Properties.GetBool("Goal")) {
                    this.Direction = Direction2.Down;
                    var nextLevel = Array.IndexOf(GameImpl.Levels, this.Map.Name) + 1;
                    GameImpl.Instance.Fade(0.005F, g => {
                        if (GameImpl.Levels.Length > nextLevel) {
                            g.StartMap(GameImpl.Levels[nextLevel], g2 => g2.Fade(0.01F));
                        } else {
                            g.DisplayCaption(new[] {
                                "At last,\nI'm not panicking anymore.",
                                "I know it'll come back eventually,\nbut for now, I'm free."
                            }, g2 => g2.DisplayCaption(new[] {
                                "Thanks for playing this game.",
                                "If you're ever feeling depressed,\nplease reach out to someone."
                            }, g3 => {
                                g3.SetMap(null);
                                GameImpl.Save(null);
                                g3.OpenMainMenu();
                                g3.Fade(0.01F);
                            }));
                        }
                    });
                    MlemGame.LoadContent<SoundEffect>("Sounds/Bed").Play();
                    continue;
                }

                // step grid tiles
                var steppedOnOff = tile.GlobalIdentifier == 7;
                if (steppedOnOff || tile.GlobalIdentifier == 8) {
                    var grid = this.Map.Tiles.TileLayers.First(t => t.Properties.GetBool("StepGrid") && !t.GetTile(pos.X, pos.Y).IsBlank);
                    if (grid != layer) {
                        var missing = grid.Tiles.Count(t => !t.IsBlank && this.Map.GetTile(t.X, t.Y).GlobalIdentifier != 8);
                        // only change tiles if the grid isn't complete
                        if (missing > 0) {
                            // if we stepped on a disabled tile, turn it on
                            if (steppedOnOff) {
                                this.Map.SetTile(pos.X, pos.Y, 8);
                            } else {
                                // otherwise, turn the whole grid off
                                foreach (var t in grid.Tiles.Where(t => !t.IsBlank))
                                    this.Map.SetTile(t.X, t.Y, (int) t.GlobalTileIdentifierWithFlags);
                            }
                        }
                        // if there's only one missing and we just switched on, we're done
                        if (missing == 1 && steppedOnOff) {
                            foreach (var other in this.Map.Tiles.TileLayers) {
                                if (other.Properties.Get("ActivatorGrid") != grid.Name)
                                    continue;
                                CoroutineHandler.Start(this.Map.AddLayerToGround(other));
                                other.Properties["Activated"] = true.ToString();
                                MlemGame.LoadContent<SoundEffect>("Sounds/Button").Play();
                                break;
                            }
                        }
                    }
                }
            }
        }

        public override void Draw(SpriteBatch batch, GameTime time) {
            var size = 6 + this.Map.Tiles.Properties.GetFloat("LightIncrease");
            this.light.Scale = this.Map.TileSize * (size + (float) Math.Sin(time.TotalGameTime.TotalSeconds));
            this.light.Position = (this.Position + new Vector2(0.5F)) * this.Map.TileSize;

            this.animation.Update(time);
            batch.Draw(this.animation.CurrentRegion, (this.Position + new Vector2(0, -0.2F)) * this.Map.TileSize, Color.White,
                0, Vector2.Zero, 1, SpriteEffects.None, 0.75F);
        }

    }
}