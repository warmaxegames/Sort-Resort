using System;
using System.Collections.Generic;
using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Mascot identity with voice settings
    /// </summary>
    [Serializable]
    public class MascotData
    {
        public string id;           // e.g., "cat", "alpaca", "tommy"
        public string displayName;  // e.g., "Whiskers", "Alfonso", "Tommy"
        public string worldId;      // Which world this mascot belongs to
        public float basePitch;     // Voice pitch multiplier (0.7 = deep, 1.4 = high)
        public float speakSpeed;    // Letters per second (default ~15-20)

        // Sprite references (loaded from Resources)
        public string spriteFolder; // e.g., "Sprites/Mascots/Cat"

        public MascotData()
        {
            basePitch = 1.0f;
            speakSpeed = 24f;
        }
    }

    /// <summary>
    /// A single line of dialogue
    /// </summary>
    [Serializable]
    public class DialogueLine
    {
        public string mascotId;     // Who is speaking
        public string expression;   // Sprite variant: "happy", "sad", "surprised", etc.
        public string text;         // The dialogue text

        public DialogueLine() { }

        public DialogueLine(string mascotId, string text, string expression = "default")
        {
            this.mascotId = mascotId;
            this.text = text;
            this.expression = expression;
        }
    }

    /// <summary>
    /// A sequence of dialogue lines
    /// </summary>
    [Serializable]
    public class DialogueSequence
    {
        public string id;           // Unique identifier for this dialogue
        public string triggerId;    // What triggers this dialogue (e.g., "island_level_1_complete")
        public List<DialogueLine> lines;
        public bool playOnce;       // Only show once ever

        public DialogueSequence()
        {
            lines = new List<DialogueLine>();
            playOnce = true;
        }
    }

    /// <summary>
    /// Trigger conditions for dialogue
    /// </summary>
    [Serializable]
    public class DialogueTrigger
    {
        public string id;
        public string dialogueId;
        public TriggerType type;
        public string worldId;      // Optional: specific world
        public int levelNumber;     // Optional: specific level
        public int threshold;       // For count-based triggers

        public enum TriggerType
        {
            LevelComplete,          // After completing a specific level
            WorldFirstLevel,        // First level of a world
            WorldComplete,          // All 100 levels of a world
            MatchMilestone,         // Total matches reached
            StarMilestone,          // Total stars reached
            FirstPlay,              // Very first time playing
            ReturnAfterAbsence,     // Haven't played in X days
            Achievement,            // Specific achievement unlocked
            ModeFirstPlay,          // First time entering a game mode
            HardModeUnlock          // Hard Mode unlocked for a world
        }

        public int gameMode = -1;   // Optional: specific GameMode (0-3), -1 = any
    }

    /// <summary>
    /// Container for all dialogue data (loaded from JSON)
    /// </summary>
    [Serializable]
    public class DialogueDatabase
    {
        public List<MascotData> mascots;
        public List<DialogueSequence> dialogues;
        public List<DialogueTrigger> triggers;

        public DialogueDatabase()
        {
            mascots = new List<MascotData>();
            dialogues = new List<DialogueSequence>();
            triggers = new List<DialogueTrigger>();
        }
    }

    /// <summary>
    /// Static loader for dialogue data
    /// </summary>
    public static class DialogueDataLoader
    {
        private static DialogueDatabase cachedDatabase;

        public static DialogueDatabase LoadDatabase()
        {
            if (cachedDatabase != null) return cachedDatabase;

            var textAsset = Resources.Load<TextAsset>("Data/Dialogue/dialogues");
            if (textAsset == null)
            {
                Debug.LogWarning("[DialogueDataLoader] No dialogue database found, using defaults");
                return CreateDefaultDatabase();
            }

            cachedDatabase = JsonUtility.FromJson<DialogueDatabase>(textAsset.text);
            return cachedDatabase;
        }

        public static MascotData GetMascot(string mascotId)
        {
            var db = LoadDatabase();
            return db.mascots.Find(m => m.id == mascotId);
        }

        public static MascotData GetMascotForWorld(string worldId)
        {
            var db = LoadDatabase();
            return db.mascots.Find(m => m.worldId == worldId);
        }

        public static DialogueSequence GetDialogue(string dialogueId)
        {
            var db = LoadDatabase();
            return db.dialogues.Find(d => d.id == dialogueId);
        }

        private static DialogueDatabase CreateDefaultDatabase()
        {
            var db = new DialogueDatabase();

            // Default mascots for each world
            db.mascots.Add(new MascotData {
                id = "cat",
                displayName = "Whiskers",
                worldId = "island",
                basePitch = 1.45f,
                speakSpeed = 27f,
                spriteFolder = "Sprites/Mascots/island_whiskers"
            });

            db.mascots.Add(new MascotData {
                id = "tommy",
                displayName = "Tommy",
                worldId = "supermarket",
                basePitch = 1.35f,
                speakSpeed = 24f,
                spriteFolder = "Sprites/Mascots/supermarket_tommy"
            });

            db.mascots.Add(new MascotData {
                id = "chicken",
                displayName = "Mara",
                worldId = "farm",
                basePitch = 1.5f,
                speakSpeed = 29f,
                spriteFolder = "Sprites/Mascots/farm_mara"
            });

            db.mascots.Add(new MascotData {
                id = "bartender",
                displayName = "Hog",
                worldId = "tavern",
                basePitch = 1.3f,
                speakSpeed = 21f,
                spriteFolder = "Sprites/Mascots/tavern_hog"
            });

            db.mascots.Add(new MascotData {
                id = "alien",
                displayName = "Leika",
                worldId = "space",
                basePitch = 1.55f,
                speakSpeed = 33f,
                spriteFolder = "Sprites/Mascots/space_leika"
            });

            cachedDatabase = db;
            return db;
        }

        public static void ClearCache()
        {
            cachedDatabase = null;
        }
    }
}
