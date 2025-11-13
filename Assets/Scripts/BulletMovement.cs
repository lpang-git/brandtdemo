using UnityEngine;

public class BulletMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    
    [SerializeField] private float speed = 4;
    
    private void Start()
    {
        rb.linearVelocity = transform.right * speed;
    }

    private void OnTriggerEnter2D(Collider2D hitInfo)
    {
        if (hitInfo.CompareTag("Enemy"))
        {
            Debug.Log(hitInfo.name);
            Destroy(gameObject);
        }

        if (hitInfo.CompareTag("ProjectileObstacle"))
        {
            Debug.Log(hitInfo.name);
            Destroy(gameObject);
        }
    }
}