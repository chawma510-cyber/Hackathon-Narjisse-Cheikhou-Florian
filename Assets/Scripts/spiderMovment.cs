using System.Collections;
using UnityEngine;

public class SpiderMovement : MonoBehaviour
{
    [Header("Points de passage")]
    public Transform P01;

    
    [Header("Parametres")]
    public float vitesse = 2f;
    public float distanceArrivee = 0.1f;

    private void Start()
    {
        // Verifier que tous les points sont assignes
        if (P01 == null)
        {
            Debug.LogError("Certains points ne sont pas assignes dans l'Inspector !");
            return;
        }

        StartCoroutine(SuivreLeParcoursComplet());
    }

    private IEnumerator SuivreLeParcoursComplet()
    {
        // Attendre 4 secondes au debut
        yield return new WaitForSeconds(4f);

        // P01 vers P02
        yield return StartCoroutine(AllerVersPoint(P01));


        Debug.Log("Parcours termine !");
    }

    private IEnumerator AllerVersPoint(Transform pointCible)
    {
        if (pointCible == null)
        {
            Debug.LogError("Point cible est null dans AllerVersPoint!");
            yield break;
        }

        while (Vector3.Distance(transform.position, pointCible.position) > distanceArrivee)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                pointCible.position,
                vitesse * Time.deltaTime
            );
            yield return null;
        }
    }

}