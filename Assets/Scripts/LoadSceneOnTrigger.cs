using UnityEngine;
using UnityEngine.SceneManagement; // Nécessaire pour changer de scène

public class ChangeSceneOnTrigger : MonoBehaviour
{
    // Nom ou index de la scène à charger
    [SerializeField] private int sceneIndex = 3;

    // Tag optionnel pour s'assurer que c'est bien la manette
    [SerializeField] private string controllerTag = "VRController";

    private void OnTriggerEnter(Collider other)
    {
        // Vérifie si l'objet qui entre a le tag "VRController"
        if (other.CompareTag(controllerTag))
        {
            // Charge la scène 3
            SceneManager.LoadScene(sceneIndex);
        }
    }
}
