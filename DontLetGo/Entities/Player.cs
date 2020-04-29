using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MLEM.Animations;
using MLEM.Extended.Tiled;
using MLEM.Misc;
using MLEM.Startup;
using MLEM.Textures;
using MonoGame.Extended;
using Penumbra;

namespace DontLetGo.Entities {
    public class Player : Entity {

        public Direction2 Direction;

        private readonly Light light;
        private readonly SpriteAnimationGroup animation;

        private Vector2 lastPosition;
        private float walkPercentage;

        public Player(Map map, Light light) : base(map) {
            this.light = light;

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
            this.animation.Update(time);
            this.light.Position = (this.Position + new Vector2(0.5F)) * this.Map.TileSize;

            if (this.walkPercentage > 0) {
                var next = Math.Max(0, this.walkPercentage - time.GetElapsedSeconds() * 2.5F);
                // we just exited the tile we were on
                if (this.walkPercentage > 0.5F != next >= 0.5F) {
                    var (x, y) = this.lastPosition.ToPoint();
                    var currTile = this.Map.GetTile(x, y);
                    if (currTile.GlobalIdentifier == 1) {
                        this.Map.SetTile(x, y, 9);
                    } else if (currTile.GlobalIdentifier == 9) {
                        this.Map.SetTile(x, y, 0);
                        this.Map.Entities.Add(new FallingTile(this.Map, currTile) {Position = this.lastPosition});
                    }
                }
                this.walkPercentage = next;
                this.Position = Vector2.Lerp(this.lastPosition, this.lastPosition + this.Direction.Offset().ToVector2(), 1 - this.walkPercentage);
            }

            if (this.walkPercentage <= 0) {
                this.lastPosition = this.Position;
                if (MlemGame.Input.IsKeyDown(Keys.Up)) {
                    this.Direction = Direction2.Up;
                } else if (MlemGame.Input.IsKeyDown(Keys.Down)) {
                    this.Direction = Direction2.Down;
                } else if (MlemGame.Input.IsKeyDown(Keys.Left)) {
                    this.Direction = Direction2.Left;
                } else if (MlemGame.Input.IsKeyDown(Keys.Right)) {
                    this.Direction = Direction2.Right;
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

        public override void Draw(SpriteBatch batch, GameTime time) {
            batch.Draw(this.animation.CurrentRegion, (this.Position + new Vector2(0, -0.2F)) * this.Map.TileSize, Color.White,
                0, Vector2.Zero, 1, SpriteEffects.None, 0.75F);
        }

    }
}