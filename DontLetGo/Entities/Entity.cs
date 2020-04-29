using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DontLetGo.Entities {
    public class Entity {

        public Vector2 Position;
        protected readonly Map Map;

        public Entity(Map map) {
            this.Map = map;
        }

        public virtual void Update(GameTime time) {
        }

        public virtual void Draw(SpriteBatch batch, GameTime time) {
        }

    }
}