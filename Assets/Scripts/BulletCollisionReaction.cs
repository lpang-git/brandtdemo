using UnityEngine;

public class BulletCollisionReaction : MonoBehaviour
{
    [SerializeField] private GameObject effect;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var bullet = other.GetComponent<BulletMovement>();

        if (bullet != null)
        {
            var oppositeRotation = Quaternion.Euler(0, 0, bullet.transform.rotation.eulerAngles.z + 180);
            
            Instantiate(effect, bullet.transform.position, oppositeRotation);
        }
    }
}
