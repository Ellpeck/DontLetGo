using Microsoft.Xna.Framework;
using MLEM.Misc;

namespace DontLetGo {
    public static class Program {

        public static void Main() {
            TextInputWrapper.Current = new TextInputWrapper.DesktopGl<TextInputEventArgs>((w, c) => w.TextInput += c);
            using var game = new GameImpl();
            game.Run();
        }

    }
}