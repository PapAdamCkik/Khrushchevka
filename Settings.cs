using Raylib_cs;
using System.Numerics;
using System.IO;
using System.Collections.Generic;

namespace Khrushchevka_RPG;

class SettingsMenu
{
    private Font font;
    private Texture2D background;
    private int selectedOption;
    private string[] settingsOptions;
    private bool isRebinding;
    private int rebindingIndex;
    
    // Store current values for display
    private KeyboardKey currentUpKey;
    private KeyboardKey currentDownKey;
    private KeyboardKey currentLeftKey;
    private KeyboardKey currentRightKey;
    private KeyboardKey currentActionKey;
    private int currentDifficulty;
    private int currentLanguage;
    
    public SettingsMenu(Font font, Texture2D background)
    {
        this.font = font;
        this.background = background;
        this.selectedOption = 0;
        this.settingsOptions = new string[] { "Up Key", "Down Key", "Left Key", "Right Key", "Action Key", "Difficulty", "Language", "Return to Menu" };
        this.isRebinding = false;
        this.rebindingIndex = -1;
    }
    
    public void Reset()
    {
        selectedOption = 0;
        isRebinding = false;
        rebindingIndex = -1;
    }
    
    public (bool exit, bool settingsChanged) Update(
        ref KeyboardKey upKey, ref KeyboardKey downKey, ref KeyboardKey leftKey, 
        ref KeyboardKey rightKey, ref KeyboardKey actionKey, ref int difficulty, ref int language)
    {
        // Store current values for drawing
        currentUpKey = upKey;
        currentDownKey = downKey;
        currentLeftKey = leftKey;
        currentRightKey = rightKey;
        currentActionKey = actionKey;
        currentDifficulty = difficulty;
        currentLanguage = language;
        
        bool exit = false;
        bool settingsChanged = false;
        
        if (!isRebinding)
        {
            // Navigate settings
            if (Raylib.IsKeyPressed(downKey))
                selectedOption = (selectedOption + 1) % settingsOptions.Length;
            
            if (Raylib.IsKeyPressed(upKey))
                selectedOption = (selectedOption - 1 + settingsOptions.Length) % settingsOptions.Length;
            
            if (Raylib.IsKeyPressed(actionKey))
            {
                if (selectedOption <= 4) // Key rebinding options (0-4)
                {
                    isRebinding = true;
                    rebindingIndex = selectedOption;
                }
                else if (selectedOption == 5) // Difficulty
                {
                    difficulty = (difficulty % 3) + 1;
                    settingsChanged = true;
                }
                else if (selectedOption == 6) // Language
                {
                    language = (language + 1) % 2;
                    settingsChanged = true;
                }
                else if (selectedOption == 7) // Return to menu
                {
                    exit = true;
                }
            }
            
            if (Raylib.IsKeyPressed(KeyboardKey.Escape))
                exit = true;
        }
        else // Rebinding mode
        {
            int key = Raylib.GetKeyPressed();
            if (key != 0)
            {
                KeyboardKey newKey = (KeyboardKey)key;
                
                switch (rebindingIndex)
                {
                    case 0: upKey = newKey; break;
                    case 1: downKey = newKey; break;
                    case 2: leftKey = newKey; break;
                    case 3: rightKey = newKey; break;
                    case 4: actionKey = newKey; break;
                }
                
                isRebinding = false;
                rebindingIndex = -1;
                settingsChanged = true;
            }
        }
        
        return (exit, settingsChanged);
    }
    
    public void Draw()
    {
        // Draw background (same as menu)
        Raylib.DrawTexturePro(
            background,
            new Rectangle(0, 0, background.Width, background.Height),
            new Rectangle(0, 0, 800, 450),
            new Vector2(0, 0),
            0.0f,
            Color.White
        );
        
        Raylib.DrawTextEx(font, "SETTINGS", new Vector2(320, 50), 40, 2, Color.Black);
        
        for (int i = 0; i < settingsOptions.Length; i++)
        {
            Rectangle buttonRect = new Rectangle(200, 110 + i * 42, 400, 38);
            Color bgColor = i == selectedOption ? Color.DarkGray : new Color(219, 189, 162, 255);
            Color textColor = i == selectedOption ? Color.White : Color.Black;
            
            Raylib.DrawRectangleRec(buttonRect, bgColor);
            Raylib.DrawRectangleLinesEx(buttonRect, 2, Color.Black);
            
            // Draw label on the left side
            string labelText = settingsOptions[i];
            Vector2 labelSize = Raylib.MeasureTextEx(font, labelText, 20, 1);
            float labelX = buttonRect.X + 10;
            float labelY = buttonRect.Y + (buttonRect.Height - labelSize.Y) / 2;
            
            if (isRebinding && rebindingIndex == i)
            {
                labelText = "Press any key...";
                textColor = Color.Red;
                // Center the "Press any key..." text
                Vector2 centerSize = Raylib.MeasureTextEx(font, labelText, 20, 1);
                labelX = buttonRect.X + (buttonRect.Width - centerSize.X) / 2;
            }
            
            Raylib.DrawTextEx(font, labelText, new Vector2(labelX, labelY), 20, 1, textColor);
            
            // Draw value on the right side (if not rebinding)
            if (!isRebinding || rebindingIndex != i)
            {
                string valueText = "";
                
                if (i == 0) valueText = currentUpKey.ToString();
                else if (i == 1) valueText = currentDownKey.ToString();
                else if (i == 2) valueText = currentLeftKey.ToString();
                else if (i == 3) valueText = currentRightKey.ToString();
                else if (i == 4) valueText = currentActionKey.ToString();
                else if (i == 5)
                {
                    valueText = currentDifficulty == 1 ? "Easy" : (currentDifficulty == 2 ? "Medium" : "Hard");
                }
                else if (i == 6)
                {
                    valueText = currentLanguage == 0 ? "English" : "Hungarian";
                }
                
                if (valueText != "")
                {
                    Vector2 valueSize = Raylib.MeasureTextEx(font, valueText, 20, 1);
                    float valueX = buttonRect.X + buttonRect.Width - valueSize.X - 10;
                    float valueY = buttonRect.Y + (buttonRect.Height - valueSize.Y) / 2;
                    
                    Raylib.DrawTextEx(font, valueText, new Vector2(valueX, valueY), 20, 1, textColor);
                }
            }
        }
    }
}

// Settings Manager - handles saving/loading
static class SettingsManager
{
    public static void SaveSettings(KeyboardKey up, KeyboardKey down, KeyboardKey left, KeyboardKey right, KeyboardKey action, int diff, int lang)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter("settings.txt"))
            {
                writer.WriteLine("up=" + (int)up);
                writer.WriteLine("down=" + (int)down);
                writer.WriteLine("left=" + (int)left);
                writer.WriteLine("right=" + (int)right);
                writer.WriteLine("action=" + (int)action);
                writer.WriteLine("difficulty=" + diff);
                writer.WriteLine("language=" + lang);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error saving settings: " + e.Message);
        }
    }
    
    public static void LoadSettings(ref KeyboardKey up, ref KeyboardKey down, ref KeyboardKey left, ref KeyboardKey right, ref KeyboardKey action, ref int diff, ref int lang)
    {
        try
        {
            if (File.Exists("settings.txt"))
            {
                using (StreamReader reader = new StreamReader("settings.txt"))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length != 2) continue;
                        
                        string key = parts[0];
                        string value = parts[1];
                        
                        switch (key)
                        {
                            case "up": up = (KeyboardKey)int.Parse(value); break;
                            case "down": down = (KeyboardKey)int.Parse(value); break;
                            case "left": left = (KeyboardKey)int.Parse(value); break;
                            case "right": right = (KeyboardKey)int.Parse(value); break;
                            case "action": action = (KeyboardKey)int.Parse(value); break;
                            case "difficulty": diff = int.Parse(value); break;
                            case "language": lang = int.Parse(value); break;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error loading settings: " + e.Message);
        }
    }
    
    public static void SaveProgress(Dictionary<string, bool> unlocks)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter("savefile.txt"))
            {
                foreach (var unlock in unlocks)
                {
                    writer.WriteLine(unlock.Key + "=" + (unlock.Value ? "1" : "0"));
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error saving progress: " + e.Message);
        }
    }
    
    public static void LoadProgress(Dictionary<string, bool> unlocks)
    {
        try
        {
            if (File.Exists("savefile.txt"))
            {
                using (StreamReader reader = new StreamReader("savefile.txt"))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length != 2) continue;
                        
                        string key = parts[0];
                        string value = parts[1];
                        
                        if (unlocks.ContainsKey(key))
                        {
                            unlocks[key] = value == "1";
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Error loading progress: " + e.Message);
        }
    }
}