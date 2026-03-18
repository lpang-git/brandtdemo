using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTrigger : MonoBehaviour
{
    private static readonly int Fade = Animator.StringToHash("fadeStart");
    
    [SerializeField] private Animator fade;
    [SerializeField] private int sceneIndex = 2;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(ChangeScene());
        }
    }

    private IEnumerator ChangeScene()
    {
        fade.SetTrigger(Fade);
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene(sceneIndex);
    }
}