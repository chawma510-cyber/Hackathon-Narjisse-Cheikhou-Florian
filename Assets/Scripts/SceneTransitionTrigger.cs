using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("Configuration de la transition")]
    public string sceneName = "3";

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        // Évite les déclenchements multiples
        if (!hasTriggered)
        {
            Debug.Log("Objet détecté: " + other.gameObject.name + " - Changement de scène!");
            hasTriggered = true;
            LoadScene();
        }
    }

    private void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}