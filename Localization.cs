using System.Collections.Generic;

namespace Khrushchevka_RPG;

public static class Localization
{
    private static Dictionary<string, Dictionary<string, string>> translations = new Dictionary<string, Dictionary<string, string>>()
    {
        // English (language = 0)
        {
            "en", new Dictionary<string, string>()
            {
                // Main Menu
                { "menu_play", "Play" },
                { "menu_tutorial", "Tutorial" },
                { "menu_settings", "Settings" },
                { "menu_exit", "Exit" },
                { "menu_created_by", "Created by" },
                
                // Settings Menu
                { "settings_title", "SETTINGS" },
                { "settings_up_key", "Up Key" },
                { "settings_down_key", "Down Key" },
                { "settings_left_key", "Left Key" },
                { "settings_right_key", "Right Key" },
                { "settings_action_key", "Action Key" },
                { "settings_difficulty", "Difficulty" },
                { "settings_language", "Language" },
                { "settings_return", "Return to Menu" },
                { "settings_press_key", "Press any key..." },
                { "settings_stats", "=== STATS ===" },
                
                // Difficulty
                { "difficulty_easy", "Easy" },
                { "difficulty_medium", "Medium" },
                { "difficulty_hard", "Hard" },
                
                // Language
                { "language_english", "English" },
                { "language_hungarian", "Hungarian" },
                
                // Pause Menu
                { "pause_title", "PAUSED" },
                { "pause_return", "Return to Game" },
                { "pause_settings", "Settings" },
                { "pause_main_menu", "Return to Main Menu" },
                
                // Stats
                { "stats_health", "Health" },
                { "stats_speed", "Speed" },
                { "stats_damage", "Damage" },
                { "stats_attack_speed", "Attack Speed" },
                { "stats_range", "Range" },
                
                // Death Screen
                { "death_title", "YOU ARE DEAD" },
                { "death_restart", "Restart" },
                { "death_main_menu", "Return to Menu" },
                
                // Won Screen
                { "won_title", "YOU WON!" },
                { "won_restart", "Play Again" },
                { "won_main_menu", "Return to Menu" },
                
                // Game UI
                { "ui_floor", "Floor" },
                { "ui_room", "Room" },
                { "ui_gateway", "Gateway to next floor!" },
                
                // Tutorial
                { "tutorial_move", "to move" },
                { "tutorial_attack", "to attack" },
                { "tutorial_ranged", "Hold attack key for ranged throw" },
                { "tutorial_items", "Pick up items to become stronger" },
                { "tutorial_enemy", "Defeat the enemy to finish the tutorial" },
            }
        },
        
        // Hungarian (language = 1)
        {
            "hu", new Dictionary<string, string>()
            {
                // Main Menu
                { "menu_play", "Játék" },
                { "menu_tutorial", "Tutorial" },
                { "menu_settings", "Beállítások" },
                { "menu_exit", "Kilépés" },
                { "menu_created_by", "Készítette" },
                
                // Settings Menu
                { "settings_title", "BEÁLLÍTÁSOK" },
                { "settings_up_key", "Fel gomb" },
                { "settings_down_key", "Le gomb" },
                { "settings_left_key", "Bal gomb" },
                { "settings_right_key", "Jobb gomb" },
                { "settings_action_key", "Akció gomb" },
                { "settings_difficulty", "Nehézség" },
                { "settings_language", "Nyelv" },
                { "settings_return", "Vissza a menübe" },
                { "settings_press_key", "Nyomj meg egy gombot..." },
                { "settings_stats", "=== STATISZTIKÁK ===" },
                
                // Difficulty
                { "difficulty_easy", "Könnyű" },
                { "difficulty_medium", "Közepes" },
                { "difficulty_hard", "Nehéz" },
                
                // Language
                { "language_english", "Angol" },
                { "language_hungarian", "Magyar" },
                
                // Pause Menu
                { "pause_title", "SZÜNET" },
                { "pause_return", "Vissza a játékba" },
                { "pause_settings", "Beállítások" },
                { "pause_main_menu", "Vissza a főmenübe" },
                
                // Stats
                { "stats_health", "Élet" },
                { "stats_speed", "Sebesség" },
                { "stats_damage", "Sebzés" },
                { "stats_attack_speed", "Támadási sebesség" },
                { "stats_range", "Hatótáv" },
                
                // Death Screen
                { "death_title", "MEGHALTÁL" },
                { "death_restart", "Újrakezd" },
                { "death_main_menu", "Vissza a menübe" },
                
                // Won Screen
                { "won_title", "GYŐZTÉL!" },
                { "won_restart", "Újrajátszás" },
                { "won_main_menu", "Vissza a menübe" },
                
                // Game UI
                { "ui_floor", "Szint" },
                { "ui_room", "Szoba" },
                { "ui_gateway", "Kapu a következő szintre!" },
                
                // Tutorial
                { "tutorial_move", "a mozgáshoz" },
                { "tutorial_attack", "a támadáshoz" },
                { "tutorial_ranged", "Tartsd nyomva a támadásgombot a dobáshoz" },
                { "tutorial_items", "Szedd fel a tárgyakat az erősödéshez" },
                { "tutorial_enemy", "Győzd le az ellenséget a befejezéshez" },
            }
        }
    };
    
    public static string Get(string key, int language)
    {
        string langCode = language == 0 ? "en" : "hu";
        
        if (translations.ContainsKey(langCode) && translations[langCode].ContainsKey(key))
        {
            return translations[langCode][key];
        }
        
        // Fallback to key if translation not found
        return key;
    }
}