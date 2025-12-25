using Platformer.Mechanics;
using UnityEngine;

public class LethalFallTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var player = other.GetComponent<PlayerController>();
        player.OnDramaticFall();
    }
}