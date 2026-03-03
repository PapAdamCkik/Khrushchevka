using Raylib_cs;
using System.Numerics;
using System.IO;
using System;

namespace Khrushchevka_RPG;

class SettingsMenu
{
    private Font font;
    private Texture2D background;
    private int selectedOption;
    private bool waitingForKey;
    private string[] settingsOptions;
    private int language;
    
    // Stored for display in Draw()
    private KeyboardKey displayUp    = KeyboardKey.W;
    private KeyboardKey displayDown  = KeyboardKey.S;
    private KeyboardKey displayLeft  = KeyboardKey.A;
    private KeyboardKey displayRight = KeyboardKey.D;
    private KeyboardKey displayAction = KeyboardKey.Space;
    private int displayDifficulty = 2;
    private int displayLanguage   = 0;
    
    public SettingsMenu(Font font, Texture2D bg)
    {
        this.font = font;
        this.background = bg;
        this.selectedOption = 0;
        this.waitingForKey = false;
        this.language = 0;
    }
    
    public void SetLanguage(int lang)
    {
        language = lang;
    }
    
    public void Reset()
    {
        selectedOption = 0;
        waitingForKey = false;
    }
    
    public (bool exit, bool changed) Update(ref KeyboardKey upKey, ref KeyboardKey downKey, ref KeyboardKey leftKey, ref KeyboardKey rightKey, ref KeyboardKey actionKey, ref int difficulty, ref int language)
    {
        bool changed = false;
        
        if (waitingForKey)
        {
            KeyboardKey pressedKey = (KeyboardKey)Raylib.GetKeyPressed();
            
            if (pressedKey != 0)
            {
                switch (selectedOption)
                {
                    case 0: upKey = pressedKey; break;
                    case 1: downKey = pressedKey; break;
                    case 2: leftKey = pressedKey; break;
                    case 3: rightKey = pressedKey; break;
                    case 4: actionKey = pressedKey; break;
                }
                
                waitingForKey = false;
                changed = true;
            }
            
            return (false, changed);
        }
        
        if (Raylib.IsKeyPressed(downKey))
            selectedOption = (selectedOption + 1) % 8;
        
        if (Raylib.IsKeyPressed(upKey))
            selectedOption = (selectedOption - 1 + 8) % 8;
        
        if (Raylib.IsKeyPressed(actionKey))
        {
            if (selectedOption >= 0 && selectedOption <= 4)
            {
                waitingForKey = true;
            }
            else if (selectedOption == 5)
            {
                difficulty = (difficulty + 1) % 3;
                changed = true;
            }
            else if (selectedOption == 6)
            {
                language = (language + 1) % 2;
                this.language = language;
                changed = true;
            }
            else if (selectedOption == 7)
            {
                return (true, changed);
            }
        }
        
        if (Raylib.IsKeyPressed(KeyboardKey.Escape))
        {
            return (true, changed);
        }
        
        // Keep display values in sync
        displayUp       = upKey;
        displayDown     = downKey;
        displayLeft     = leftKey;
        displayRight    = rightKey;
        displayAction   = actionKey;
        displayDifficulty = difficulty;
        displayLanguage   = language;
        
        return (false, changed);
    }
    
    public void Draw()
    {
        Raylib.DrawTexturePro(
            background,
            new Rectangle(0, 0, background.Width, background.Height),
            new Rectangle(0, 0, 800, 450),
            new Vector2(0, 0),
            0.0f,
            new Color(255, 255, 255, 100)
        );
        
        Raylib.DrawRectangle(0, 0, 800, 450, new Color(0, 0, 0, 200));
        
        string title = Localization.Get("settings_title", language);
        Vector2 titleSize = Raylib.MeasureTextEx(font, title, 40, 2);
        Raylib.DrawTextEx(font, title, new Vector2((800 - titleSize.X) / 2, 30), 40, 2, Color.White);
        
        settingsOptions = new string[]
        {
            Localization.Get("settings_up_key", language),
            Localization.Get("settings_down_key", language),
            Localization.Get("settings_left_key", language),
            Localization.Get("settings_right_key", language),
            Localization.Get("settings_action_key", language),
            Localization.Get("settings_difficulty", language),
            Localization.Get("settings_language", language),
            Localization.Get("settings_return", language)
        };
        
        for (int i = 0; i < settingsOptions.Length; i++)
        {
            Rectangle buttonRect = new Rectangle(200, 100 + i * 45, 400, 40);
            Color bgColor   = i == selectedOption ? Color.DarkGray : new Color(219, 189, 162, 255);
            Color textColor = i == selectedOption ? Color.White    : Color.Black;
            
            Raylib.DrawRectangleRec(buttonRect, bgColor);
            Raylib.DrawRectangleLinesEx(buttonRect, 2, Color.Black);
            
            // Label — centered
            string labelText = settingsOptions[i];
            if (i == selectedOption && waitingForKey)
                labelText = Localization.Get("settings_press_key", language);
            
            Vector2 labelSize = Raylib.MeasureTextEx(font, labelText, 22, 1);
            float labelX = buttonRect.X + (buttonRect.Width - labelSize.X) / 2;
            float labelY = buttonRect.Y + (buttonRect.Height - labelSize.Y) / 2;
            Raylib.DrawTextEx(font, labelText, new Vector2(labelX, labelY), 22, 1, textColor);
            
            // Right-side value (key binding or current setting value)
            if (!waitingForKey || i != selectedOption)
            {
                string valueText = i switch
                {
                    0 => displayUp.ToString(),
                    1 => displayDown.ToString(),
                    2 => displayLeft.ToString(),
                    3 => displayRight.ToString(),
                    4 => displayAction.ToString(),
                    5 => displayDifficulty switch { 0 => Localization.Get("difficulty_easy", language), 1 => Localization.Get("difficulty_medium", language), _ => Localization.Get("difficulty_hard", language) },
                    6 => displayLanguage == 0 ? Localization.Get("language_english", language) : Localization.Get("language_hungarian", language),
                    _ => ""
                };
                
                if (valueText != "")
                {
                    Vector2 valSize = Raylib.MeasureTextEx(font, valueText, 20, 1);
                    float valX = buttonRect.X + buttonRect.Width - valSize.X - 10;
                    float valY = buttonRect.Y + (buttonRect.Height - valSize.Y) / 2;
                    Color valColor = i == selectedOption ? new Color((byte)200, (byte)220, (byte)255, (byte)255) : new Color((byte)80, (byte)80, (byte)80, (byte)255);
                    Raylib.DrawTextEx(font, valueText, new Vector2(valX, valY), 20, 1, valColor);
                }
            }
        }
    }
}

public static class SettingsManager
{
    public static void SaveSettings(KeyboardKey up, KeyboardKey down, KeyboardKey left, KeyboardKey right, KeyboardKey action, int difficulty, int language)
    {
        using var writer = new StreamWriter("settings.txt");
        writer.WriteLine($"up={((int)up)}");
        writer.WriteLine($"down={((int)down)}");
        writer.WriteLine($"left={((int)left)}");
        writer.WriteLine($"right={((int)right)}");
        writer.WriteLine($"action={((int)action)}");
        writer.WriteLine($"difficulty={difficulty}");
        writer.WriteLine($"language={language}");
    }
    
    public static void LoadSettings(ref KeyboardKey up, ref KeyboardKey down, ref KeyboardKey left, ref KeyboardKey right, ref KeyboardKey action, ref int diff, ref int lang)
    {
        try
        {
            if (!File.Exists("settings.txt"))
            {
                // Set WASD as defaults
                up = KeyboardKey.W;
                down = KeyboardKey.S;
                left = KeyboardKey.A;
                right = KeyboardKey.D;
                action = KeyboardKey.Space;
                return;
            }
            
            foreach (var line in File.ReadLines("settings.txt"))
            {
                var parts = line.Split('=');
                if (parts.Length != 2) continue;
                
                switch (parts[0])
                {
                    case "up": up = (KeyboardKey)int.Parse(parts[1]); break;
                    case "down": down = (KeyboardKey)int.Parse(parts[1]); break;
                    case "left": left = (KeyboardKey)int.Parse(parts[1]); break;
                    case "right": right = (KeyboardKey)int.Parse(parts[1]); break;
                    case "action": action = (KeyboardKey)int.Parse(parts[1]); break;
                    case "difficulty": diff = int.Parse(parts[1]); break;
                    case "language": lang = int.Parse(parts[1]); break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading settings: {e.Message}");
        }
    }
    
    public static void SaveProgress(Dictionary<string, bool> unlocks)
    {
        using var writer = new StreamWriter("savefile.txt");
        foreach (var unlock in unlocks)
        {
            writer.WriteLine($"{unlock.Key}={unlock.Value}");
        }
    }
    
    public static void LoadProgress(Dictionary<string, bool> unlocks)
    {
        try
        {
            if (!File.Exists("savefile.txt"))
                return;
            
            foreach (var line in File.ReadLines("savefile.txt"))
            {
                var parts = line.Split('=');
                if (parts.Length != 2) continue;
                
                string key = parts[0];
                // Handle both legacy "0"/"1" and current "True"/"False" formats
                bool value;
                if (parts[1] == "0" || parts[1] == "1")
                    value = parts[1] == "1";
                else
                    value = bool.Parse(parts[1]);
                
                if (unlocks.ContainsKey(key))
                {
                    unlocks[key] = value;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading progress: {e.Message}");
        }
    }
}