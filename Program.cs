using Raylib_cs;
using System.Numerics;
using System.Collections.Generic;

namespace Khrushchevka_RPG;

class Program
{
    public enum GameState { MainMenu, Playing, Settings }
    
    static void Main(string[] args)
{
    Raylib.InitWindow(800, 450, "Khrushchevka RPG");
    Raylib.SetExitKey(KeyboardKey.Null);  // Disable ESC closing the window
    Raylib.SetTargetFPS(60);
    
    Texture2D background = Raylib.LoadTexture("images/menu-background.jpeg");
    Texture2D logo = Raylib.LoadTexture("images/logo.png");
    Font customFont = Raylib.LoadFont("fonts/Terminus-Bold.ttf");
    
    
        
        GameState currentState = GameState.MainMenu;
        GameState previousState = GameState.MainMenu;
        
        // Settings variables - Game controls
        KeyboardKey upKey = KeyboardKey.Up;
        KeyboardKey downKey = KeyboardKey.Down;
        KeyboardKey leftKey = KeyboardKey.Left;
        KeyboardKey rightKey = KeyboardKey.Right;
        KeyboardKey actionKey = KeyboardKey.Space;
        int difficulty = 2;
        int language = 0;
        
        // Game progress - unlocks
        Dictionary<string, bool> unlocks = new Dictionary<string, bool>()
        {
            { "level_2", false },
            { "level_3", false },
            { "weapon_sword", false },
            { "weapon_axe", false },
            { "armor_leather", false },
            { "floor_7_boss", false },  // Add this
            { "floor_8_boss", false },  // Add this
            { "floor_9_boss", false }   // Add this
        };
        
        // Load saved settings and progress
        SettingsManager.LoadSettings(ref upKey, ref downKey, ref leftKey, ref rightKey, ref actionKey, ref difficulty, ref language);
        SettingsManager.LoadProgress(unlocks);
        
        
        // Create menu and settings instances
        Menu menu = new Menu(background, logo, customFont);
        SettingsMenu settingsMenu = new SettingsMenu(customFont, background);
        Game game = new Game();
        game.SetFont(customFont);  // Pass font to game for pause menu
        game.LoadProgress(unlocks);
        
        while (!Raylib.WindowShouldClose())
        {
            // UPDATE
            switch (currentState)
            {
                case GameState.MainMenu:
                    GameState newState = menu.Update(upKey, downKey, actionKey);
                    if (newState != currentState)
                    {
                        currentState = newState;
                        if (currentState == GameState.Settings)
                        {
                            previousState = GameState.MainMenu;
                            settingsMenu.Reset();
                        }
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
                    
                case GameState.Settings:
                    (bool settingsExit, bool settingsChanged) = settingsMenu.Update(
                        ref upKey, ref downKey, ref leftKey, ref rightKey, ref actionKey, 
                        ref difficulty, ref language);
                    
                    if (settingsChanged)
                    {
                        SettingsManager.SaveSettings(upKey, downKey, leftKey, rightKey, actionKey, difficulty, language);
                    }
                    
                    if (settingsExit)
                    {
                        currentState = previousState;  // Return to previous state
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
                    
                case GameState.Settings:
                    // Draw the previous state in background
                    if (previousState == GameState.Playing)
                        game.Draw();
                    else
                        menu.Draw();
                    
                    // Draw settings overlay
                    settingsMenu.Draw();
                    break;
            }
            
            Raylib.EndDrawing();
        }
        
        Raylib.UnloadTexture(background);
        Raylib.UnloadTexture(logo);
        Raylib.UnloadFont(customFont);
        Raylib.CloseWindow();
    }
}