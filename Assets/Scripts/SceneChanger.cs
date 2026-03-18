using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTrigger : MonoBehaviour
{
    private static readonly int Fade = Animator.StringToHash("fadeStart");
    private static readonly int HubTrigger = Animator.StringToHash("hubTrigger");
    
    [SerializeField] private Animator fade;
    [SerializeField] private Animator cutscene;
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
        cutscene.SetTrigger(HubTrigger);
        yield return new WaitForSeconds(7);
        SceneManager.LoadScene(sceneIndex);
    }
}