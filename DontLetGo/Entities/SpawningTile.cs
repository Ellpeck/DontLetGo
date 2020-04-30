using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Extended.Tiled;
using MLEM.Extensions;
using MLEM.Textures;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;
using Penumbra;

namespace DontLetGo.Entities {
    public class SpawningTile : Entity {

        private readonly Light light;
        private readonly TiledMapTile tile;
        private readonly TextureRegion texture;
        private float alpha;

        public SpawningTile(Map map, TiledMapTile tile, Vector2 position) : base(map) {
            this.tile = tile;
            this.Position = position;

            var tilesetTile = tile.GetTilesetTile(map.Tiles);
            var tileset = tile.GetTileset(map.Tiles);
            this.texture = new TextureRegion(tileset.Texture, tileset.GetTextureRegion(tilesetTile));

            this.light = map.CreateTileLight(position.X + 0.5F, position.Y + 0.5F, tilesetTile);
            if (this.light != null) {
                this.light.Intensity = 0;
                this.Map.Penumbra.Lights.Add(this.light);
            }
        }

        public override void Update(GameTime time) {
            base.Update(time);
            this.alpha += time.GetElapsedSeconds();
            if (this.light != null)
                this.light.Intensity = this.alpha;
            if (this.alpha >= 1) {
                this.Map.SetTile(this.Position.X.Floor(), this.Position.Y.Floor(), (int) this.tile.GlobalTileIdentifierWithFlags);
                if (this.light != null)
                    this.Map.Penumbra.Lights.Remove(this.light);
                this.Map.Entities.Remove(this);
            }
        }

        public override void Draw(SpriteBatch batch, GameTime time) {
            var origin = this.Map.TileSize / 2;
            batch.Draw(this.texture, this.Position * this.Map.TileSize + origin, Color.White * this.alpha, 0, origin, 1, SpriteEffects.None, 0.25F);
        }

    }
}