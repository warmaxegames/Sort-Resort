using UnityEngine;
using UnityEngine.SceneManagement;

namespace SortResort
{
    public class Bootstrap : MonoBehaviour
    {
        [Header("Manager Prefabs")]
        [SerializeField] private GameObject gameManagerPrefab;
        [SerializeField] private GameObject audioManagerPrefab;
        [SerializeField] private GameObject saveManagerPrefab;
        [SerializeField] private GameObject transitionManagerPrefab;

        [Header("Startup")]
        [SerializeField] private string firstSceneName = "MainMenu";
        [SerializeField] private bool skipToScene = true;

        private void Awake()
        {
            InitializeManagers();
        }

        private void Start()
        {
            if (skipToScene && !string.IsNullOrEmpty(firstSceneName))
            {
                LoadFirstScene();
            }
        }

        private void InitializeManagers()
        {
            // Create managers if they don't exist
            if (GameManager.Instance == null)
            {
                if (gameManagerPrefab != null)
                {
                    Instantiate(gameManagerPrefab);
                }
                else
                {
                    CreateDefaultGameManager();
                }
            }

            if (AudioManager.Instance == null)
            {
                if (audioManagerPrefab != null)
                {
                    Instantiate(audioManagerPrefab);
                }
                else
                {
                    CreateDefaultAudioManager();
                }
            }

            if (SaveManager.Instance == null)
            {
                if (saveManagerPrefab != null)
                {
                    Instantiate(saveManagerPrefab);
                }
                else
                {
                    CreateDefaultSaveManager();
                }
            }

            if (TransitionManager.Instance == null)
            {
                if (transitionManagerPrefab != null)
                {
                    Instantiate(transitionManagerPrefab);
                }
                else
                {
                    CreateDefaultTransitionManager();
                }
            }

            Debug.Log("[Bootstrap] All managers initialized");
        }

        private void CreateDefaultGameManager()
        {
            var obj = new GameObject("GameManager");
            obj.AddComponent<GameManager>();
        }

        private void CreateDefaultAudioManager()
        {
            var obj = new GameObject("AudioManager");
            obj.AddComponent<AudioManager>();
        }

        private void CreateDefaultSaveManager()
        {
            var obj = new GameObject("SaveManager");
            obj.AddComponent<SaveManager>();
        }

        private void CreateDefaultTransitionManager()
        {
            var obj = new GameObject("TransitionManager");
            obj.AddComponent<TransitionManager>();
        }

        private void LoadFirstScene()
        {
            if (TransitionManager.Instance != null)
            {
                TransitionManager.Instance.LoadScene(firstSceneName);
            }
            else
            {
                SceneManager.LoadScene(firstSceneName);
            }
        }
    }
}
