using System;
using System.Collections.Generic;
using System.Linq;
using DontLetGo.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Extended.Tiled;
using MonoGame.Extended.Tiled;
using Penumbra;
using RectangleF = MonoGame.Extended.RectangleF;

namespace DontLetGo {
    public class Map {

        private readonly IndividualTiledMapRenderer renderer;
        public readonly TiledMap Tiles;
        public Vector2 DrawSize => new Vector2(this.Tiles.WidthInPixels, this.Tiles.HeightInPixels);
        public Vector2 TileSize => this.Tiles.GetTileSize();
        public List<Entity> Entities = new List<Entity>();

        public Map(TiledMap tiles, PenumbraComponent penumbra) {
            this.Tiles = tiles;
            this.renderer = new IndividualTiledMapRenderer(tiles, (tile, layer, index, position) => 0.5F);

            penumbra.Hulls.Clear();
            penumbra.Lights.Clear();

            var tileSize = this.Tiles.GetTileSize();
            for (var x = 0; x < this.Tiles.Width; x++) {
                for (var y = 0; y < this.Tiles.Height; y++) {
                    foreach (var tile in this.Tiles.GetTiles(x, y)) {
                        foreach (var obj in tile.GetTilesetTile(this.Tiles).Objects) {
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

        public TiledMapTile GetTile(int x, int y) {
            return this.Tiles.GetTile("Ground", x, y);
        }

        public void SetTile(int x, int y, int tile) {
            var index = this.Tiles.GetTileLayerIndex("Ground");
            this.Tiles.TileLayers[index].SetTile((ushort) x, (ushort) y, (uint) tile);
            this.renderer.UpdateDrawInfo(index, x, y);
        }

        public void Update(GameTime time) {
            this.renderer.UpdateAnimations(time);
            for (var i = this.Entities.Count - 1; i >= 0; i--)
                this.Entities[i].Update(time);
        }

        public void Draw(SpriteBatch batch, GameTime time, RectangleF frustum) {
            this.renderer.Draw(batch, frustum);
            foreach (var entity in this.Entities)
                entity.Draw(batch, time);
        }

    }
}