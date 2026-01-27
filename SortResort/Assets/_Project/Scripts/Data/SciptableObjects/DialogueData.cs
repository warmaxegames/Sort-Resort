using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Sort Resort/Story/Dialogue")]
public class DialogueData : ScriptableObject
{
    [Header("Identity")]
    public string dialogueID = "island_intro";
    public string dialogueName = "Welcome to Island";

    [Header("World Reference")]
    public WorldData parentWorld;

    [Header("Dialogue Content")]
    public List dialogueLines = new List();

    [Header("Presentation")]
    [Tooltip("Optional custom background for this dialogue")]
    public Sprite customBackground;

    [Tooltip("Optional music override during dialogue")]
    public AudioClip backgroundMusic;

    [Tooltip("Dim the game scene behind the dialogue")]
    public bool dimBackground = true;

    [Tooltip("Pause gameplay during dialogue")]
    public bool pauseGameplay = true;

    [Header("Completion")]
    [Tooltip("Track if player has seen this dialogue")]
    public bool markAsRead = true;

    [Tooltip("Automatically play this dialogue after this one")]
    public DialogueData nextDialogue;
}

[System.Serializable]
public class DialogueLine
{
    [Header("Speaker")]
    public string speakerName = "Tiki";
    public MascotEmotion emotion = MascotEmotion.Neutral;

    [Tooltip("Override mascot sprite for this line only")]
    public Sprite customMascotSprite;

    [Header("Content")]
    [TextArea(3, 6)]
    public string dialogueText = "Hello! Welcome to Sort Resort Island!";

    [Header("Timing")]
    [Tooltip("Seconds per character for typewriter effect")]
    [UnityEngine.Range(0.01f, 0.1f)]
    public float textSpeed = 0.05f;

    [Tooltip("Auto-advance after this many seconds (0 = wait for click)")]
    public float autoAdvanceDelay = 0f;

    [Header("Audio")]
    [Tooltip("Optional voice acting clip")]
    public AudioClip voiceClip;

    [Tooltip("Optional sound effect for this line")]
    public AudioClip soundEffect;

    [Header("Special Effects")]
    public bool shakeText = false;
    public bool largeText = false;
    public Color textColor = Color.white;
    public DialogueAnimation animation = DialogueAnimation.None;
}

public enum MascotEmotion
{
    Neutral,
    Happy,
    Sad,
    Excited,
    Thinking,
    Surprised,
    Angry,
    Worried
}

public enum DialogueAnimation
{
    None,
    SlideIn,
    FadeIn,
    Bounce,
    Shake
}
