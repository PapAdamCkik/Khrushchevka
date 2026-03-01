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
    
    public Menu(Texture2D background, Texture2D logo, Font font)
    {
        this.background = background;
        this.logo = logo;
        this.font = font;
        this.selectedOption = 0;
        this.language = 0;
    }
    
    public void SetLanguage(int lang)
    {
        language = lang;
    }
    
    public Program.GameState Update(KeyboardKey upKey, KeyboardKey downKey, KeyboardKey leftKey, KeyboardKey rightKey, KeyboardKey actionKey)
    {
        // Navigate with movement keys OR arrow keys (shooting keys)
        bool goDown = Raylib.IsKeyPressed(downKey) || Raylib.IsKeyPressed(KeyboardKey.Down);
        bool goUp   = Raylib.IsKeyPressed(upKey)   || Raylib.IsKeyPressed(KeyboardKey.Up);
        
        if (goDown)
            selectedOption = (selectedOption + 1) % 3;
        
        if (goUp)
            selectedOption = (selectedOption - 1 + 3) % 3;
        
        if (Raylib.IsKeyPressed(actionKey))
        {
            switch (selectedOption)
            {
                case 0: return Program.GameState.Playing;
                case 1: return Program.GameState.Settings;
                case 2: Raylib.CloseWindow(); break;
            }
        }
        
        return Program.GameState.MainMenu;
    }
    
    public void Draw()
    {
        // Draw background
        Raylib.DrawTexturePro(
            background,
            new Rectangle(0, 0, background.Width, background.Height),
            new Rectangle(0, 0, 800, 450),
            new Vector2(0, 0),
            0.0f,
            Color.White
        );
        
        // Draw logo
        int logoX = (800 - logo.Width) / 2;
        Raylib.DrawTexture(logo, logoX, 50, Color.White);
        
        // Draw menu buttons
        string[] menuOptions = new string[]
        {
            Localization.Get("menu_play", language),
            Localization.Get("menu_settings", language),
            Localization.Get("menu_exit", language)
        };
        
        for (int i = 0; i < menuOptions.Length; i++)
        {
            Rectangle buttonRect = new Rectangle(300, 250 + i * 60, 200, 50);
            Color bgColor = i == selectedOption ? Color.DarkGray : new Color(219, 189, 162, 255);
            Color textColor = i == selectedOption ? Color.White : Color.Black;
            
            Raylib.DrawRectangleRec(buttonRect, bgColor);
            Raylib.DrawRectangleLinesEx(buttonRect, 2, Color.Black);
            
            Vector2 textSize = Raylib.MeasureTextEx(font, menuOptions[i], 30, 2);
            float textX = buttonRect.X + (buttonRect.Width - textSize.X) / 2;
            float textY = buttonRect.Y + (buttonRect.Height - textSize.Y) / 2;
            
            Raylib.DrawTextEx(font, menuOptions[i], new Vector2(textX, textY), 30, 2, textColor);
        }
    }
}