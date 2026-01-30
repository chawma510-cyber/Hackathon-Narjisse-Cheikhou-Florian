using System.Collections;
using UnityEngine;

public class BeeMovement : MonoBehaviour
{
    [Header("Points de passage")]
    public Transform P01;
    public Transform P02;
    public Transform P03;
    public Transform P04;
    public Transform P05;
    public Transform P06;
    public Transform P07;
    public Transform P08;
    public Transform P09;
    public Transform P10;

    [Header("Parametres")]
    public float vitesse = 2f;
    public float vitesseRotation = 100f;
    public float distanceArrivee = 0.1f;

    private void Start()
    {
        // Verifier que tous les points sont assignes
        if (P01 == null || P02 == null || P03 == null || P04 == null ||
            P06 == null || P07 == null || P08 == null || P09 == null || P10 == null)
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
        yield return StartCoroutine(AllerVersPoint(P02));

        // Rotation 40 degres gauche puis P03
        yield return StartCoroutine(Rotation(-40f));
        yield return StartCoroutine(AllerVersPoint(P03));

        // P04
        yield return StartCoroutine(Rotation(40f));
        yield return StartCoroutine(AllerVersPoint(P04));

        // Rotation 90 degres droite puis P06
        yield return StartCoroutine(Rotation(90f));
        yield return StartCoroutine(AllerVersPoint(P06));

        // Rotation 180 degres puis P07
        yield return StartCoroutine(Rotation(180f));
        yield return StartCoroutine(AllerVersPoint(P07));

        // Rotation 90 degres droite puis P08
        yield return StartCoroutine(Rotation(90f));
        yield return StartCoroutine(AllerVersPoint(P08));

        // Rotation 35 degres droite puis P09
        yield return StartCoroutine(Rotation(35f));
        yield return StartCoroutine(AllerVersPoint(P09));

        // Rotation 35 degres droite puis P10
        yield return StartCoroutine(Rotation(46f));
        yield return StartCoroutine(AllerVersPoint(P10));

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

    private IEnumerator Rotation(float angle)
    {
        float rotationRestante = Mathf.Abs(angle);
        float direction = Mathf.Sign(angle);

        while (rotationRestante > 0.1f)
        {
            float rotationCeTour = Mathf.Min(vitesseRotation * Time.deltaTime, rotationRestante);
            transform.Rotate(0, direction * rotationCeTour, 0);
            rotationRestante -= rotationCeTour;
            yield return null;
        }
    }
}