using Platformer.Mechanics;
using UnityEngine;

public class SFXOffTrigger : MonoBehaviour
{
    [SerializeField] private AudioSource sfx;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player =  other.GetComponent<PlayerController>();

        if (!player) 
            return;
        
        sfx.Stop();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (!player)
            return;
        
        sfx.Play();
    }
}
