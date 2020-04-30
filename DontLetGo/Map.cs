using System;
using System.Collections.Generic;
using System.Linq;
using Coroutine;
using DontLetGo.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Extended.Tiled;
using MLEM.Extensions;
using MonoGame.Extended.Collections;
using MonoGame.Extended.Tiled;
using Penumbra;
using RectangleF = MonoGame.Extended.RectangleF;

namespace DontLetGo {
    public class Map {

        private readonly Random random = new Random();
        private readonly IndividualTiledMapRenderer renderer;
        public readonly TiledMap Tiles;
        public readonly PenumbraComponent Penumbra;
        public readonly List<Entity> Entities = new List<Entity>();
        public Vector2 DrawSize => new Vector2(this.Tiles.WidthInPixels, this.Tiles.HeightInPixels);
        public Vector2 TileSize => this.Tiles.GetTileSize();

        public Map(TiledMap tiles, PenumbraComponent penumbra) {
            this.Tiles = tiles;
            this.Penumbra = penumbra;
            this.renderer = new IndividualTiledMapRenderer(tiles, (tile, layer, index, position) => 0.5F + 0.001F * index);

            penumbra.Hulls.Clear();
            penumbra.Lights.Clear();

            foreach (var layer in this.Tiles.TileLayers) {
                if (layer.Properties.ContainsKey("Activated"))
                    layer.IsVisible = layer.Properties.GetBool("Activated");

                if (layer.IsVisible) {
                    for (var x = 0; x < this.Tiles.Width; x++) {
                        for (var y = 0; y < this.Tiles.Height; y++)
                            this.OnTileChanged(layer.Name, x, y);
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
            this.OnTileChanged("Ground", x, y);
        }

        public IEnumerator<IWait> AddLayerToGround(TiledMapTileLayer layer) {
            var tiles = new List<(ushort, ushort, uint)>();
            for (ushort x = 0; x < this.Tiles.Width; x++) {
                for (ushort y = 0; y < this.Tiles.Height; y++) {
                    var tile = layer.GetTile(x, y);
                    if (tile.IsBlank)
                        continue;
                    tiles.Add((x, y, tile.GlobalTileIdentifierWithFlags));
                }
            }
            tiles.Shuffle(this.random);
            foreach (var (x, y, tile) in tiles) {
                yield return new WaitSeconds(0.15F);
                this.Entities.Add(new SpawningTile(this, new TiledMapTile(tile, x, y), new Vector2(x, y)));
            }
        }

        private void OnTileChanged(string layer, int x, int y) {
            var tile = this.Tiles.GetTile(layer, x, y);
            if (tile.IsBlank)
                return;
            var tileset = tile.GetTileset(this.Tiles);
            var tilesetTile = tileset.GetTilesetTile(tile, this.Tiles);

            var light = this.CreateTileLight(x + 0.5F, y + 0.5F, tilesetTile);
            if (light != null)
                this.Penumbra.Lights.Add(light);

            foreach (var obj in tilesetTile.Objects) {
                if (obj.Name == "Hull") {
                    this.Penumbra.Hulls.Add(new Hull(new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)) {
                        Position = obj.Position + new Vector2(x, y) * this.TileSize,
                        Scale = obj.Size
                    });
                }
            }
        }

        public Light CreateTileLight(float x, float y, TiledMapTilesetTile tile) {
            var light = tile.Properties.GetFloat("Light");
            if (light <= 0)
                return null;
            return new PointLight {
                Position = new Vector2(x, y) * this.TileSize,
                Color = ColorExtensions.FromHex(0xffec969b),
                Scale = light * this.TileSize
            };
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