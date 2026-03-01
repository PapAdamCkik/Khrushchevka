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
        // Basic ASCII (32–126)
        for (int i = 32; i <= 126; i++) cp.Add(i);
        // Hungarian / Extended Latin accented characters
        int[] extras = {
            0x00C1, // Á
            0x00E1, // á
            0x00C9, // É
            0x00E9, // é
            0x00CD, // Í
            0x00ED, // í
            0x00D3, // Ó
            0x00F3, // ó
            0x00D6, // Ö
            0x00F6, // ö
            0x0150, // Ő
            0x0151, // ő
            0x00DA, // Ú
            0x00FA, // ú
            0x00DC, // Ü
            0x00FC, // ü
            0x0170, // Ű
            0x0171, // ű
        };
        foreach (int e in extras) cp.Add(e);
        return cp.ToArray();
    }
    
    static void Main(string[] args)
    {
        Raylib.InitWindow(800, 450, "Khrushchevka RPG");
        Raylib.SetExitKey(KeyboardKey.Null);
        Raylib.SetTargetFPS(60);
        SoundManager.Init();
        
        Texture2D background = Raylib.LoadTexture("images/menu-background.jpeg");
        Texture2D logo = Raylib.LoadTexture("images/logo.png");
        // Load font with full Latin + Hungarian character set
        // LoadFont only loads ASCII — LoadFontEx with explicit codepoints covers áéíóöőúüű etc.
        int[] codepoints = BuildCodepoints();
        Font customFont = Raylib.LoadFontEx("fonts/Terminus-Bold.ttf", 32, codepoints, codepoints.Length);
        
        GameState currentState = GameState.MainMenu;
        GameState previousState = GameState.MainMenu;
        
        // Settings variables - WASD defaults
        KeyboardKey upKey = KeyboardKey.W;
        KeyboardKey downKey = KeyboardKey.S;
        KeyboardKey leftKey = KeyboardKey.A;
        KeyboardKey rightKey = KeyboardKey.D;
        KeyboardKey actionKey = KeyboardKey.Space;
        int difficulty = 2;
        int language = 0;
        
        // Game progress (simplified - no floor unlocks)
        Dictionary<string, bool> unlocks = new Dictionary<string, bool>()
        {
            { "level_2", false },
            { "level_3", false },
            { "weapon_sword", false },
            { "weapon_axe", false },
            { "armor_leather", false }
        };
        
        // Load saved settings and progress
        SettingsManager.LoadSettings(ref upKey, ref downKey, ref leftKey, ref rightKey, ref actionKey, ref difficulty, ref language);
        SettingsManager.LoadProgress(unlocks);
        
        // Create game instances
        Menu menu = new Menu(background, logo, customFont);
        SettingsMenu settingsMenu = new SettingsMenu(customFont, background);
        Game game = new Game();
        game.SetFont(customFont);
        Tutorial tutorial = new Tutorial();
        tutorial.SetFont(customFont);
        
        // Sync language to all components
        menu.SetLanguage(language);
        settingsMenu.SetLanguage(language);
        game.SetLanguage(language);
        
        while (!Raylib.WindowShouldClose())
        {
            // UPDATE
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
                    {
                        currentState = gameState;
                    }
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
                        // Sync language changes immediately
                        menu.SetLanguage(language);
                        settingsMenu.SetLanguage(language);
                        game.SetLanguage(language);
                    }
                    
                    if (settingsExit)
                    {
                        currentState = previousState;
                    }
                    break;
            }
            
            // DRAW
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);
            
            switch (currentState)
            {
                case GameState.MainMenu:
                    menu.Draw();
                    break;
                    
                case GameState.Playing:
                    game.Draw();
                    break;
                    
                case GameState.Tutorial:
                    tutorial.Draw();
                    break;
                    
                case GameState.Settings:
                    if (previousState == GameState.Playing)
                        game.Draw();
                    else
                        menu.Draw();
                    
                    settingsMenu.Draw();
                    break;
            }
            
            Raylib.EndDrawing();
        }
        
        if (Raylib.IsWindowReady())
        {
            SoundManager.Unload();
            Raylib.UnloadTexture(background);
            Raylib.UnloadTexture(logo);
            Raylib.UnloadFont(customFont);
            Raylib.CloseWindow();
        }
    }
}