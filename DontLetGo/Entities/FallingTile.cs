using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Extended.Tiled;
using MLEM.Textures;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;
using Penumbra;

namespace DontLetGo.Entities {
    public class FallingTile : Entity {

        private readonly Light light;
        private readonly TextureRegion texture;
        private float scale = 1;

        public FallingTile(Map map, TiledMapTile tile, Vector2 position) : base(map) {
            this.Position = position;
            var tilesetTile = tile.GetTilesetTile(map.Tiles);
            var tileset = tile.GetTileset(map.Tiles);
            this.texture = new TextureRegion(tileset.Texture, tileset.GetTextureRegion(tilesetTile));

            this.light = map.CreateTileLight(position.X + 0.5F, position.Y + 0.5F, tilesetTile);
            if (this.light != null)
                this.Map.Penumbra.Lights.Add(this.light);
        }

        public override void Update(GameTime time) {
            base.Update(time);
            this.scale -= time.GetElapsedSeconds();
            if (this.light != null)
                this.light.Intensity = this.scale;
            if (this.scale <= 0) {
                if (this.light != null)
                    this.Map.Penumbra.Lights.Remove(this.light);
                this.Map.Entities.Remove(this);
            }
        }

        public override void Draw(SpriteBatch batch, GameTime time) {
            var origin = this.Map.TileSize / 2;
            batch.Draw(this.texture, this.Position * this.Map.TileSize + origin, Color.White,
                1 - this.scale, origin, this.scale, SpriteEffects.None, 0.25F);
        }

    }
}