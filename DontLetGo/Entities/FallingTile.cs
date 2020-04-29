using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Extended.Tiled;
using MLEM.Textures;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;

namespace DontLetGo.Entities {
    public class FallingTile : Entity {

        private readonly TextureRegion texture;
        private float scale = 1;

        public FallingTile(Map map, TiledMapTile tile) : base(map) {
            var tilesetTile = tile.GetTilesetTile(map.Tiles);
            var tileset = tile.GetTileset(map.Tiles);
            this.texture = new TextureRegion(tileset.Texture, tileset.GetTextureRegion(tilesetTile));
        }

        public override void Update(GameTime time) {
            base.Update(time);
            this.scale -= time.GetElapsedSeconds();
            if (this.scale <= 0)
                this.Map.Entities.Remove(this);
        }

        public override void Draw(SpriteBatch batch, GameTime time) {
            var origin = this.Map.TileSize / 2;
            batch.Draw(this.texture, this.Position * this.Map.TileSize + origin, Color.White,
                1 - this.scale, origin, this.scale, SpriteEffects.None, 0.25F);
        }

    }
}