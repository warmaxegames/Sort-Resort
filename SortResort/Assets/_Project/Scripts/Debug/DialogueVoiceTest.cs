using UnityEngine;

namespace SortResort
{
    /// <summary>
    /// Simple test component for dialogue voice system.
    /// Add to any GameObject and press Play to test voices.
    /// </summary>
    public class DialogueVoiceTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private string testText = "Hello! Welcome to Sort Resort!";

        [Header("Mascot Selection")]
        [SerializeField] private MascotVoice selectedMascot = MascotVoice.Cat;

        [Header("Custom Pitch (for manual testing)")]
        [SerializeField] private float customPitch = 1.0f;
        [SerializeField] private bool useCustomPitch = false;

        public enum MascotVoice
        {
            Cat,        // Island - 1.3x pitch
            Tommy,      // Supermarket - 1.0x pitch
            Chicken,    // Farm - 1.4x pitch
            Bartender,  // Tavern - 0.85x pitch
            Alien       // Space - 1.5x pitch
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 300));

            GUILayout.Label("=== Dialogue Voice Test ===", GUI.skin.box);
            GUILayout.Space(10);

            // Test text input
            GUILayout.Label("Test Text:");
            testText = GUILayout.TextField(testText, GUILayout.Width(380));
            GUILayout.Space(10);

            // Mascot buttons
            GUILayout.Label("Test Mascot Voices:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cat\n(1.3x)", GUILayout.Height(50)))
                TestMascot("cat");
            if (GUILayout.Button("Tommy\n(1.0x)", GUILayout.Height(50)))
                TestMascot("tommy");
            if (GUILayout.Button("Chicken\n(1.4x)", GUILayout.Height(50)))
                TestMascot("chicken");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bartender\n(0.85x)", GUILayout.Height(50)))
                TestMascot("bartender");
            if (GUILayout.Button("Alien\n(1.5x)", GUILayout.Height(50)))
                TestMascot("alien");
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Custom pitch slider
            GUILayout.Label($"Custom Pitch: {customPitch:F2}x");
            customPitch = GUILayout.HorizontalSlider(customPitch, 0.5f, 2.0f, GUILayout.Width(380));

            if (GUILayout.Button($"Test Custom Pitch ({customPitch:F2}x)", GUILayout.Height(40)))
            {
                TestCustomPitch();
            }

            GUILayout.Space(10);

            // Quick preset buttons
            GUILayout.Label("Quick Pitch Presets:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Deep\n0.7x")) { customPitch = 0.7f; TestCustomPitch(); }
            if (GUILayout.Button("Low\n0.85x")) { customPitch = 0.85f; TestCustomPitch(); }
            if (GUILayout.Button("Normal\n1.0x")) { customPitch = 1.0f; TestCustomPitch(); }
            if (GUILayout.Button("High\n1.3x")) { customPitch = 1.3f; TestCustomPitch(); }
            if (GUILayout.Button("Squeaky\n1.6x")) { customPitch = 1.6f; TestCustomPitch(); }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void TestMascot(string mascotId)
        {
            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[DialogueVoiceTest] DialogueManager not found! Make sure the game is running.");
                return;
            }

            Debug.Log($"[DialogueVoiceTest] Testing mascot: {mascotId}");
            DialogueManager.Instance.TestMascotVoice(mascotId, testText);
        }

        private void TestCustomPitch()
        {
            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[DialogueVoiceTest] DialogueManager not found! Make sure the game is running.");
                return;
            }

            Debug.Log($"[DialogueVoiceTest] Testing custom pitch: {customPitch:F2}x");
            DialogueManager.Instance.TestVoiceWithPitch(customPitch, testText);
        }
    }
}
