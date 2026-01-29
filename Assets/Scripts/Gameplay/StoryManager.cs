using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using HackathonVR.Core;

namespace HackathonVR.Gameplay
{
    public class StoryManager : MonoBehaviour
    {
        public static StoryManager Instance;

        [Header("References")]
        public AudioSource audioSource;
        public AudioClip sfxFlash;
        public AudioClip sfxScream;
        
        [Header("Narjisse")]
        public GameObject narjisseObject;
        public SimpleDialogue narjisseDialogue; // Reuse the dialogue component?
        
        [Header("Telescope")]
        public Transform telescopeLookPoint;
        public float lookTriggerDistance = 0.5f;
        public float lookDuration = 2.0f;
        
        [Header("UI")]
        public GameObject flashPanel; // White screen UI

        private bool waitingForPlayerLook = false;
        private float currentLookTime = 0f;
        private Transform vrCamera;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            vrCamera = Camera.main.transform;
            if (flashPanel != null) flashPanel.SetActive(false);
        }

        // Called when Book is Closed/Released
        public void OnBookFinished()
        {
            StartCoroutine(Sequence_NarjisseEvent());
        }

        private IEnumerator Sequence_NarjisseEvent()
        {
            Debug.Log("[StoryManager] Book finished. Narjisse speaks.");
            
            // 1. Narjisse Dialogue
            if (narjisseDialogue != null)
            {
                // Ensure audio source helps sound originate from her? 
                // SimpleDialogue doesn't handle audio, but we can play sound here.
                
                var lines = new System.Collections.Generic.List<string>();
                lines.Add("Regarde le télescope ! Il s'est passé quelque chose...");
                narjisseDialogue.SetDialogue(lines);
            }
            else if (DialogueManager.Instance != null)
            {
                // Fallback
                DialogueManager.Instance.ShowMessage("Narjisse", "Regarde le télescope ! Il s'est passé quelque chose...", 4f);
            }
            
            yield return new WaitForSeconds(4f);

            // 2. Narjisse Action (Fake animation/move)
            Debug.Log("[StoryManager] Narjisse looks into telescope...");
            
            // Move Narjisse to Telescope if needed (or assume she's there)
            if (narjisseObject != null && telescopeLookPoint != null)
            {
                 // Teleport her for now as we don't have NavMesh setup for her explicitly here 
                 // or she might be static.
                 // narjisseObject.transform.position = telescopeLookPoint.position - Vector3.forward; 
            }

            yield return new WaitForSeconds(2f);
            
            // 3. Scream & Disappear
            if (audioSource != null && sfxScream != null) audioSource.PlayOneShot(sfxScream);
            Debug.Log("[StoryManager] Narjisse SCREAMS and DISAPPEARS!");
            
            // Flash Effect for Narjisse?
            yield return FlashScreen(0.2f);
            
            if (narjisseObject != null) narjisseObject.SetActive(false);
            
            yield return new WaitForSeconds(1f);
            
            // 4. Invite Player
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.ShowMessage("", "C'est à votre tour... Regardez dans le télescope.", 5f);
            }
            
            waitingForPlayerLook = true;
        }

        private void Update()
        {
            if (waitingForPlayerLook && telescopeLookPoint != null)
            {
                CheckPlayerLook();
            }
        }

        private void CheckPlayerLook()
        {
            if (vrCamera == null) return;

            float dist = Vector3.Distance(vrCamera.position, telescopeLookPoint.position);
            
            // Simple check: Is player close to 'eyepiece' and looking forward?
            if (dist < lookTriggerDistance)
            {
                currentLookTime += Time.deltaTime;
                if (currentLookTime > lookDuration)
                {
                    waitingForPlayerLook = false;
                    StartCoroutine(Sequence_TransitionToScene3());
                }
            }
            else
            {
                currentLookTime = 0f;
            }
        }

        private IEnumerator Sequence_TransitionToScene3()
        {
            Debug.Log("[StoryManager] Player looked! TRANSITIONING...");
            
            // 1. Sound
            if (audioSource != null && sfxFlash != null) audioSource.PlayOneShot(sfxFlash);
            
            // 2. Flash
            yield return FlashScreen(2f); // Long white flash
            
            // 3. Load Scene 3
            Debug.Log("[StoryManager] Loading Scene 3...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("3");
        }

        private IEnumerator FlashScreen(float duration)
        {
            if (flashPanel != null)
            {
                flashPanel.SetActive(true);
                yield return new WaitForSeconds(duration);
                flashPanel.SetActive(false); // Or keep active if scene loads
            }
        }
    }
}
