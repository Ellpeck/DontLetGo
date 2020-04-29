using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Extended.Tiled;
using MonoGame.Extended.Tiled;
using Penumbra;
using RectangleF = MonoGame.Extended.RectangleF;

namespace DontLetGo {
    public class Map {

        private readonly TiledMap map;
        private readonly IndividualTiledMapRenderer renderer;
        public Vector2 DrawSize => new Vector2(this.map.WidthInPixels, this.map.HeightInPixels);
        public Vector2 TileSize => this.map.GetTileSize();

        public Map(TiledMap map, PenumbraComponent penumbra) {
            this.map = map;
            this.renderer = new IndividualTiledMapRenderer(map);

            penumbra.Hulls.Clear();
            penumbra.Lights.Clear();

            var tileSize = this.map.GetTileSize();
            for (var x = 0; x < this.map.Width; x++) {
                for (var y = 0; y < this.map.Height; y++) {
                    foreach (var tile in this.map.GetTiles(x, y)) {
                        foreach (var obj in tile.GetTilesetTile(this.map).Objects) {
                            if (obj.Name == "Hull") {
                                penumbra.Hulls.Add(new Hull(new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)) {
                                    Position = obj.Position + new Vector2(x, y) * tileSize,
                                    Scale = obj.Size
                                });
                            }
                        }
                    }
                }
            }
        }

        public void Update(GameTime time) {
            this.renderer.UpdateAnimations(time);
        }

        public void Draw(SpriteBatch batch, GameTime time, RectangleF frustum) {
            this.renderer.Draw(batch, frustum);
        }

    }
}