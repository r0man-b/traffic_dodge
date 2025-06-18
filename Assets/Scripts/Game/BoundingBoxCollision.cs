using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BoundingBoxCollision : MonoBehaviour
{
    public GameObject sparksPrefab;  // Reference to the sparks GameObject prefab.
    public AudioClip[] audioClips = new AudioClip[5];  // Array of audio clips with a fixed length of 5.

    private AudioSource audioSource;  // Private audio source (no need to set it in the inspector).
    private PlayerController playerController;

    private void Awake()
    {
        // Automatically grab the AudioSource component attached to this GameObject.
        audioSource = GetComponent<AudioSource>();
        audioSource.volume = 0.35f * SaveManager.Instance.SaveData.EffectsVolumeMultiplier;

        // Get the playerController component for in lane split and accel variables.
        if (SceneManager.GetActiveScene().buildIndex != 0)
        {
            GameObject PlayerCarObject = GameObject.Find("PlayerCar");
            playerController = PlayerCarObject.GetComponent<PlayerController>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playerController.currentlyLaneSplitting) return;
        if (other.CompareTag("TrafficBoundingBox"))
        {
            sparksPrefab.SetActive(true);  // Activate the sparks when entering a traffic bounding box.
            PlayRandomAudioClip();  // Play a random clip from the array.
            StartCoroutine(DeactivateSparksAfterDelay(0.1f));  // Start the coroutine to deactivate after 0.1 seconds.
        }
    }

    private IEnumerator DeactivateSparksAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);  // Wait for the specified delay.
        sparksPrefab.SetActive(false);  // Deactivate the sparks.
    }

    private void PlayRandomAudioClip()
    {
        if (audioClips.Length > 0 && audioSource != null)
        {
            // Choose a random audio clip from the array.
            int randomIndex = Random.Range(0, audioClips.Length);
            AudioClip selectedClip = audioClips[randomIndex];
            audioSource.volume = (0.35f + 0.05f * playerController.accel) * SaveManager.Instance.SaveData.EffectsVolumeMultiplier;
            if (selectedClip != null)
            {
                audioSource.clip = selectedClip;  // Set the chosen clip to the audio source.
                audioSource.Play();  // Play the selected audio clip.
            }
        }
    }
}
