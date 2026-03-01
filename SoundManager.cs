using Raylib_cs;
using System.Collections.Generic;

namespace Khrushchevka_RPG;

public static class SoundManager
{
    private static Dictionary<string, Sound> sounds = new();
    private static bool initialized = false;

    // All sound file names — put .wav files in a sounds/ folder
    private static readonly string[] SoundFiles = {
        "player_hurt",
        "player_death",
        "player_step",
        "sword_swing",
        "sword_throw",
        "sword_hit",
        "enemy_hurt",
        "enemy_death",
        "enemy_shoot",
        "enemy_revive",
        "boss_enter",
        "boss_hurt",
        "boss_death",
        "room_lock",
        "room_unlock",
        "item_pickup",
        "floor_transition",
    };

    public static void Init()
    {
        if (initialized) return;
        Raylib.InitAudioDevice();
        initialized = true;

        foreach (var name in SoundFiles)
        {
            // Try .wav first, then .ogg
            string path = $"sounds/{name}.wav";
            if (!System.IO.File.Exists(path))
                path = $"sounds/{name}.ogg";
            if (!System.IO.File.Exists(path))
                continue; // skip missing files silently

            Sound s = Raylib.LoadSound(path);
            sounds[name] = s;
        }
    }

    public static void Play(string name, float volume = 1.0f)
    {
        if (!sounds.TryGetValue(name, out Sound s)) return;
        Raylib.SetSoundVolume(s, volume);
        Raylib.PlaySound(s);
    }

    // Play only if not already playing — good for looping footsteps
    public static void PlayIfNotPlaying(string name, float volume = 1.0f)
    {
        if (!sounds.TryGetValue(name, out Sound s)) return;
        if (Raylib.IsSoundPlaying(s)) return;
        Raylib.SetSoundVolume(s, volume);
        Raylib.PlaySound(s);
    }

    public static void Stop(string name)
    {
        if (!sounds.TryGetValue(name, out Sound s)) return;
        Raylib.StopSound(s);
    }

    public static void Unload()
    {
        foreach (var s in sounds.Values)
            Raylib.UnloadSound(s);
        sounds.Clear();
        if (initialized)
        {
            Raylib.CloseAudioDevice();
            initialized = false;
        }
    }
}
