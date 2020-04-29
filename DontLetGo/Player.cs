using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Animations;
using MLEM.Extended.Extensions;
using MLEM.Misc;
using MLEM.Startup;
using MLEM.Textures;
using Penumbra;

namespace DontLetGo {
    public class Player {

        public Vector2 Position;
        public Direction2 Direction;

        private bool isWalking;

        private readonly Map map;
        private readonly Light light;
        private readonly SpriteAnimationGroup animation;

        public Player(Map map, Light light) {
            this.map = map;
            this.light = light;

            var tex = new UniformTextureAtlas(MlemGame.LoadContent<Texture2D>("Textures/Player"), 4, 4);
            this.animation = new SpriteAnimationGroup();
            this.animation.Add(new SpriteAnimation(1, tex[0, 0]), () => this.Direction == Direction2.Down);
            this.animation.Add(new SpriteAnimation(1, tex[1, 0]), () => this.Direction == Direction2.Up);
            this.animation.Add(new SpriteAnimation(1, tex[2, 0]), () => this.Direction == Direction2.Left);
            this.animation.Add(new SpriteAnimation(1, tex[3, 0]), () => this.Direction == Direction2.Right);
            this.animation.Add(new SpriteAnimation(0.15F, tex[0, 0], tex[0, 1], tex[0, 2], tex[0, 3]), () => this.isWalking && this.Direction == Direction2.Down, 10);
            this.animation.Add(new SpriteAnimation(0.15F, tex[1, 0], tex[1, 1], tex[1, 2], tex[1, 3]), () => this.isWalking && this.Direction == Direction2.Up, 10);
            this.animation.Add(new SpriteAnimation(0.15F, tex[2, 0], tex[2, 1], tex[2, 2], tex[2, 3]), () => this.isWalking && this.Direction == Direction2.Left, 10);
            this.animation.Add(new SpriteAnimation(0.15F, tex[3, 0], tex[3, 1], tex[3, 2], tex[3, 3]), () => this.isWalking && this.Direction == Direction2.Right, 10);
        }

        public void Update(GameTime time) {
            this.animation.Update(time);
            this.light.Position = (this.Position + new Vector2(0.5F)) * this.map.TileSize;
        }

        public void Draw(SpriteBatch batch, GameTime time) {
            batch.Draw(this.animation.CurrentRegion, this.Position * this.map.TileSize, Color.White);
        }

    }
}