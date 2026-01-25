using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTrigger : MonoBehaviour
{
    [Header("Szene Index")]
    public int sceneIndex = 2;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Prüfen ob Player den Trigger betritt
        if (other.CompareTag("Player"))
        {
            // Szene wechseln
            SceneManager.LoadScene(sceneIndex);
        }
    }
}