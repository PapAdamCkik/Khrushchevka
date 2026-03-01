using Raylib_cs;
using System.Numerics;

namespace Khrushchevka_RPG;

class Menu
{
    private Texture2D background;
    private Texture2D logo;
    private Font font;
    private int selectedOption;
    private int language;
    private const int OptionCount = 4;
    
    public Menu(Texture2D background, Texture2D logo, Font font)
    {
        this.background = background;
        this.logo = logo;
        this.font = font;
        this.selectedOption = 0;
        this.language = 0;
    }
    
    public void SetLanguage(int lang) { language = lang; }
    
    public Program.GameState Update(KeyboardKey upKey, KeyboardKey downKey, KeyboardKey leftKey, KeyboardKey rightKey, KeyboardKey actionKey)
    {
        bool goDown = Raylib.IsKeyPressed(downKey) || Raylib.IsKeyPressed(KeyboardKey.Down);
        bool goUp   = Raylib.IsKeyPressed(upKey)   || Raylib.IsKeyPressed(KeyboardKey.Up);
        if (goDown) selectedOption = (selectedOption + 1) % OptionCount;
        if (goUp)   selectedOption = (selectedOption - 1 + OptionCount) % OptionCount;
        
        if (Raylib.IsKeyPressed(actionKey))
        {
            switch (selectedOption)
            {
                case 0: return Program.GameState.Playing;
                case 1: return Program.GameState.Tutorial;
                case 2: return Program.GameState.Settings;
                case 3: Raylib.CloseWindow(); break;
            }
        }
        return Program.GameState.MainMenu;
    }
    
    public void Draw()
    {
        Raylib.DrawTexturePro(background,
            new Rectangle(0, 0, background.Width, background.Height),
            new Rectangle(0, 0, 800, 450),
            new Vector2(0, 0), 0.0f, Color.White);
        
        int logoX = (800 - logo.Width) / 2;
        Raylib.DrawTexture(logo, logoX, 30, Color.White);
        
        string[] menuOptions =
        {
            Localization.Get("menu_play",     language),
            Localization.Get("menu_tutorial", language),
            Localization.Get("menu_settings", language),
            Localization.Get("menu_exit",     language)
        };
        
        for (int i = 0; i < menuOptions.Length; i++)
        {
            Rectangle buttonRect = new Rectangle(300, 220 + i * 55, 200, 45);
            Color bgColor   = i == selectedOption ? Color.DarkGray : new Color(219, 189, 162, 255);
            Color textColor = i == selectedOption ? Color.White    : Color.Black;
            Raylib.DrawRectangleRec(buttonRect, bgColor);
            Raylib.DrawRectangleLinesEx(buttonRect, 2, Color.Black);
            Vector2 sz = Raylib.MeasureTextEx(font, menuOptions[i], 26, 2);
            Raylib.DrawTextEx(font, menuOptions[i],
                new Vector2(buttonRect.X + (buttonRect.Width - sz.X) / 2,
                            buttonRect.Y + (buttonRect.Height - sz.Y) / 2),
                26, 2, textColor);
        }
        // Credit — bottom right
        string credit = $"{Localization.Get("menu_created_by", language)}: Pap Ádám, Claude Sonnet 4.6";
        Vector2 creditSz = Raylib.MeasureTextEx(font, credit, 14, 1);
        float cx = 800 - creditSz.X - 8;
        float cy = 450 - creditSz.Y - 8;
        Raylib.DrawRectangle((int)(cx - 3), (int)(cy - 3), (int)(creditSz.X + 6), (int)(creditSz.Y + 6), Color.Black);
        Raylib.DrawTextEx(font, credit, new Vector2(cx, cy), 14, 1, Color.White);
    }
}