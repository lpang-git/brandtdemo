using UnityEngine;

public class CheckpointLogic : MonoBehaviour
{
    [SerializeField] private GameObject playerSpawn;
    [SerializeField] private Transform newPlayerSpawn;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            OnCheckpointReached();
        }
    }

    private void OnCheckpointReached()
    {
        // Moving spawn point to checkpoint
        playerSpawn.transform.position = newPlayerSpawn.position;
        // sfx and vfx
        Debug.Log("Checkpoint reached at:" + playerSpawn.transform.position);
    }
}
