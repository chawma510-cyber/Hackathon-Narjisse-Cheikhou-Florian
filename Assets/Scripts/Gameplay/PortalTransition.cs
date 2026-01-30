using UnityEngine;
using HackathonVR.Core;

namespace HackathonVR.Gameplay
{
    public class PortalTransition : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("The name of the scene to load.")]
        public string targetSceneName = "3";

        [Tooltip("Tag of the player object.")]
        public string playerTag = "Player";

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                Debug.Log($"[PortalTransition] Player entered portal. Loading scene: {targetSceneName}");
                
                if (GameManager.Instance != null)
                {
                    // Use GameManager to handle the transition (which might include fades/saving/etc.)
                    GameManager.Instance.LoadScene(targetSceneName);
                }
                else
                {
                    Debug.LogWarning("[PortalTransition] GameManager instance not found! Falling back to SceneManager direct load.");
                    UnityEngine.SceneManagement.SceneManager.LoadScene(targetSceneName);
                }
            }
        }
    }
}
