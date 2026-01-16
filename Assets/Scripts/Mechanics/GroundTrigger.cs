using Platformer.Mechanics;
using UnityEngine;

public class GroundTrigger : MonoBehaviour
{
    [Tooltip("0 = Gravel, 1 = Basalt")]
    [SerializeField] private int groundType;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.gameObject.GetComponent<PlayerController>();

        if (!player) 
            return;

        if (groundType == player.groundType)
            return;
        
        Debug.Log("Changed ground to " + groundType);
        player.SetGroundType(groundType);
    }
}
