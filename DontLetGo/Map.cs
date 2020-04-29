using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Extended.Tiled;
using MonoGame.Extended.Tiled;
using RectangleF = MonoGame.Extended.RectangleF;

namespace DontLetGo {
    public class Map {

        private readonly TiledMap map;
        private readonly IndividualTiledMapRenderer renderer;
        public Vector2 DrawSize => new Vector2(this.map.WidthInPixels, this.map.HeightInPixels);

        public Map(TiledMap map) {
            this.map = map;
            this.renderer = new IndividualTiledMapRenderer(map);
        }

        public void Update(GameTime time) {
            this.renderer.UpdateAnimations(time);
        }

        public void Draw(SpriteBatch batch, GameTime time, RectangleF frustum) {
            this.renderer.Draw(batch, frustum);
        }

    }
}