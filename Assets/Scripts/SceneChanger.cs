using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Platformer.Mechanics;

public class SceneTrigger : MonoBehaviour
{
    private static readonly int Fade = Animator.StringToHash("fadeStart");
    private static readonly int HubTrigger = Animator.StringToHash("hubTrigger");
    
    [SerializeField] private Animator fade;
    [SerializeField] private Animator cutscene;
    [SerializeField] private int sceneIndex = 2;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.gameObject.GetComponent<PlayerController>();
        if (!player) return;
        
        player.controlEnabled = false;
        StartCoroutine(ChangeScene());
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