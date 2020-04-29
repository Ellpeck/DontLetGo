using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;

namespace DontLetGo.Entities {
    public class FallingTile : Entity {

        private readonly TiledMapTilesetTile tile;
        private float scale = 1;

        public FallingTile(Map map, TiledMapTilesetTile tile) : base(map) {
            this.tile = tile;
        }

        public override void Update(GameTime time) {
            base.Update(time);
            this.scale -= time.GetElapsedSeconds();
            if (this.scale <= 0)
                this.Map.Entities.Remove(this);
        }

        public override void Draw(SpriteBatch batch, GameTime time) {
            var tileset = this.Map.Tileset;
            var origin = this.Map.TileSize / 2;
            batch.Draw(tileset.Texture, this.Position * this.Map.TileSize + origin, tileset.GetTileRegion(this.tile.LocalTileIdentifier),
                Color.White, 1 - this.scale, origin, this.scale, SpriteEffects.None, 0.25F);
        }

    }
}