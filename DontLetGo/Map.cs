using System;
using System.Collections;
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

        public readonly TiledMap Tiles;
        public readonly PenumbraComponent Penumbra;
        public readonly List<Entity> Entities = new List<Entity>();
        public readonly string Name;
        public readonly string[] Caption;
        public Vector2 DrawSize => new Vector2(this.Tiles.WidthInPixels, this.Tiles.HeightInPixels);
        public Vector2 TileSize => this.Tiles.GetTileSize();
        private readonly Dictionary<LayerPosition, Light> tileLights = new Dictionary<LayerPosition, Light>();
        private readonly Random random = new Random();
        private readonly IndividualTiledMapRenderer renderer;

        public Map(string name, TiledMap tiles, PenumbraComponent penumbra) {
            this.Tiles = tiles;
            this.Penumbra = penumbra;
            this.Name = name;
            this.Caption = tiles.Properties.Get("Caption")?.Replace("|", "\n").Split(";");
            this.renderer = new IndividualTiledMapRenderer(tiles, (tile, layer, index, position) => 0.5F + 0.001F * index);

            foreach (var layer in this.Tiles.TileLayers) {
                var copy = false;
                if (layer.Properties.GetBool("StepGrid")) {
                    layer.IsVisible = false;
                    copy = true;
                }
                if (layer.Properties.ContainsKey("Activated")) {
                    layer.IsVisible = false;
                    if (layer.Properties.GetBool("Activated"))
                        copy = true;
                }

                if (copy) {
                    foreach (var tile in layer.Tiles.Where(t => !t.IsBlank))
                        this.SetTile(tile.X, tile.Y, (int) tile.GlobalTileIdentifierWithFlags);
                }
                if (layer.IsVisible) {
                    for (var x = 0; x < this.Tiles.Width; x++) {
                        for (var y = 0; y < this.Tiles.Height; y++)
                            this.OnTileChanged(layer.Name, x, y);
                    }
                }
            }
        }

        public Vector2 GetSpawnPoint() {
            return new Vector2(this.Tiles.Properties.GetInt("SpawnX"), this.Tiles.Properties.GetInt("SpawnY"));
        }

        public TiledMapTile GetTile(int x, int y, string layer = "Ground") {
            return this.Tiles.GetTile(layer, x, y);
        }

        public void SetTile(int x, int y, int tile, string layer = "Ground") {
            var index = this.Tiles.GetTileLayerIndex(layer);
            this.Tiles.TileLayers[index].SetTile((ushort) x, (ushort) y, (uint) tile);
            this.renderer.UpdateDrawInfo(index, x, y);
            this.OnTileChanged(layer, x, y);
        }

        public IEnumerator<IWait> AddLayerToGround(TiledMapTileLayer layer) {
            var tiles = layer.Tiles.Where(t => !t.IsBlank).ToList();
            tiles.Shuffle(this.random);
            foreach (var tile in tiles) {
                yield return new WaitSeconds(0.15F);
                this.Entities.Add(new SpawningTile(this, tile, new Vector2(tile.X, tile.Y)));
            }
        }

        public IEnumerator<IWait> RemoveLayerFromGround(TiledMapTileLayer layer) {
            var tiles = layer.Tiles.Where(t => !t.IsBlank).ToList();
            tiles.Shuffle(this.random);
            foreach (var tile in tiles) {
                yield return new WaitSeconds(0.15F);
                this.SetTile(tile.X, tile.Y, 0);
                this.Entities.Add(new FallingTile(this, tile, new Vector2(tile.X, tile.Y)));
            }
        }

        private void OnTileChanged(string layer, int x, int y) {
            var layerPos = new LayerPosition(layer, x, y);
            if (this.tileLights.TryGetValue(layerPos, out var lastLight)) {
                this.Penumbra.Lights.Remove(lastLight);
                this.tileLights.Remove(layerPos);
            }

            var tile = this.GetTile(x, y, layer);
            if (tile.IsBlank)
                return;
            var tileset = tile.GetTileset(this.Tiles);
            var tilesetTile = tileset.GetTilesetTile(tile, this.Tiles);

            var light = this.CreateTileLight(x + 0.5F, y + 0.5F, tilesetTile);
            if (light != null) {
                this.tileLights.Add(layerPos, light);
                this.Penumbra.Lights.Add(light);
            }

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
            var color = tile.Properties.ContainsKey("LightColor") ? tile.Properties.GetColor("LightColor") : ColorExtensions.FromHex(0xffec969b);
            return new PointLight {
                Position = new Vector2(x, y) * this.TileSize,
                Color = color,
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