using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;

namespace Khrushchevka_RPG;

class Program
{
    public enum GameState { MainMenu, Playing, Settings, Tutorial }
    
    static int[] BuildCodepoints()
    {
        var cp = new System.Collections.Generic.List<int>();
        for (int i = 32; i <= 126; i++) cp.Add(i);
        int[] extras = {
            0x00C1, 0x00E1, 0x00C9, 0x00E9, 0x00CD, 0x00ED,
            0x00D3, 0x00F3, 0x00D6, 0x00F6, 0x0150, 0x0151,
            0x00DA, 0x00FA, 0x00DC, 0x00FC, 0x0170, 0x0171,
        };
        foreach (int e in extras) cp.Add(e);
        return cp.ToArray();
    }
    
    static void Main(string[] args)
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(800, 450, "Khrushchevka RPG");
        Raylib.SetExitKey(KeyboardKey.Null);
        Raylib.SetTargetFPS(60);
        
        Texture2D background = Raylib.LoadTexture("images/menu-background.jpeg");
        Texture2D logo = Raylib.LoadTexture("images/logo.png");

        const string fontPath = "fonts/Terminus-Bold.ttf";
        if (!System.IO.File.Exists(fontPath))
        {
            Raylib.CloseWindow();
            throw new System.IO.FileNotFoundException(
                "Required font not found: " + fontPath + ". Place Terminus-Bold.ttf in the fonts/ folder.");
        }
        int[] codepoints = BuildCodepoints();
        Font customFont = Raylib.LoadFontEx(fontPath, 32, codepoints, codepoints.Length);
        Raylib.SetTextureFilter(customFont.Texture, TextureFilter.Point);

        // Render everything at fixed 800x450, then scale to window with letterboxing
        const int GameW = 800, GameH = 450;
        RenderTexture2D renderTarget = Raylib.LoadRenderTexture(GameW, GameH);
        
        GameState currentState = GameState.MainMenu;
        GameState previousState = GameState.MainMenu;
        
        KeyboardKey upKey = KeyboardKey.W;
        KeyboardKey downKey = KeyboardKey.S;
        KeyboardKey leftKey = KeyboardKey.A;
        KeyboardKey rightKey = KeyboardKey.D;
        KeyboardKey actionKey = KeyboardKey.Space;
        int difficulty = 2;
        int language = 0;
        
        Dictionary<string, bool> unlocks = new Dictionary<string, bool>()
        {
            { "level_2", false },
            { "level_3", false },
            { "weapon_sword", false },
            { "weapon_axe", false },
            { "armor_leather", false }
        };
        
        SettingsManager.LoadSettings(ref upKey, ref downKey, ref leftKey, ref rightKey, ref actionKey, ref difficulty, ref language);
        SettingsManager.LoadProgress(unlocks);
        
        Menu menu = new Menu(background, logo, customFont);
        SettingsMenu settingsMenu = new SettingsMenu(customFont, background);
        Game game = new Game();
        game.SetFont(customFont);
        Tutorial tutorial = new Tutorial();
        tutorial.SetFont(customFont);
        
        menu.SetLanguage(language);
        settingsMenu.SetLanguage(language);
        game.SetLanguage(language);
        
        while (!Raylib.WindowShouldClose())
        {
            // F11 toggles fullscreen
            if (Raylib.IsKeyPressed(KeyboardKey.F11))
            {
                if (Raylib.IsWindowFullscreen())
                {
                    Raylib.ToggleFullscreen();
                    Raylib.SetWindowSize(800, 450);
                }
                else
                {
                    int monitor = Raylib.GetCurrentMonitor();
                    Raylib.SetWindowSize(Raylib.GetMonitorWidth(monitor), Raylib.GetMonitorHeight(monitor));
                    Raylib.ToggleFullscreen();
                }
            }

            switch (currentState)
            {
                case GameState.MainMenu:
                    GameState newState = menu.Update(upKey, downKey, leftKey, rightKey, actionKey);
                    if (newState != currentState)
                    {
                        currentState = newState;
                        if (currentState == GameState.Settings)
                        {
                            previousState = GameState.MainMenu;
                            settingsMenu.Reset();
                        }
                        if (currentState == GameState.Tutorial)
                            tutorial.Reset(upKey, downKey, leftKey, rightKey, actionKey, language);
                    }
                    break;
                    
                case GameState.Playing:
                    var (gameState, openSettings) = game.Update(upKey, downKey, leftKey, rightKey, actionKey, difficulty, language, unlocks);
                    if (openSettings)
                    {
                        previousState = GameState.Playing;
                        currentState = GameState.Settings;
                        settingsMenu.Reset();
                    }
                    else if (gameState != GameState.Playing)
                        currentState = gameState;
                    break;
                    
                case GameState.Tutorial:
                    bool tutDone = tutorial.Update();
                    if (tutDone) currentState = GameState.MainMenu;
                    break;
                    
                case GameState.Settings:
                    (bool settingsExit, bool settingsChanged) = settingsMenu.Update(
                        ref upKey, ref downKey, ref leftKey, ref rightKey, ref actionKey,
                        ref difficulty, ref language);
                    if (settingsChanged)
                    {
                        SettingsManager.SaveSettings(upKey, downKey, leftKey, rightKey, actionKey, difficulty, language);
                        menu.SetLanguage(language);
                        settingsMenu.SetLanguage(language);
                        game.SetLanguage(language);
                    }
                    if (settingsExit) currentState = previousState;
                    break;
            }
            
            // Draw everything into the fixed-resolution render target
            Raylib.BeginTextureMode(renderTarget);
            Raylib.ClearBackground(Color.Black);

            switch (currentState)
            {
                case GameState.MainMenu: menu.Draw(); break;
                case GameState.Playing:  game.Draw();  break;
                case GameState.Tutorial: tutorial.Draw(); break;
                case GameState.Settings:
                    if (previousState == GameState.Playing) game.Draw();
                    else menu.Draw();
                    settingsMenu.Draw();
                    break;
            }

            Raylib.EndTextureMode();

            // Scale render target to window with letterboxing
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            int winW = Raylib.GetScreenWidth();
            int winH = Raylib.GetScreenHeight();
            float scale = Math.Min((float)winW / GameW, (float)winH / GameH);
            int destW = (int)(GameW * scale);
            int destH = (int)(GameH * scale);
            int destX = (winW - destW) / 2;
            int destY = (winH - destH) / 2;

            // Source rect: flip Y because RenderTexture is upside-down in Raylib
            Rectangle src  = new Rectangle(0, 0, GameW, -GameH);
            Rectangle dest = new Rectangle(destX, destY, destW, destH);
            Raylib.DrawTexturePro(renderTarget.Texture, src, dest, Vector2.Zero, 0f, Color.White);

            Raylib.EndDrawing();
        }
        
        if (Raylib.IsWindowReady())
        {
            Raylib.UnloadRenderTexture(renderTarget);
            Raylib.UnloadTexture(background);
            Raylib.UnloadTexture(logo);
            Raylib.UnloadFont(customFont);
            Raylib.CloseWindow();
        }
    }
}