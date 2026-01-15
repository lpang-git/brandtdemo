using UnityEngine;

public class BulletMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    
    [SerializeField] private float speed = 20;
    [SerializeField] private float maxDistance = 10f;
    
    private Vector2 _spawnPos;
    
    private void Start()
    {
        rb.linearVelocity = transform.right * speed;
        _spawnPos = transform.position;
    }
    
    private void Update()
    {
        if (Vector2.Distance(_spawnPos, transform.position) > maxDistance)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D hitInfo)
    {
        Debug.Log(hitInfo.name);
        Destroy(gameObject);
    }
}