using UnityEngine;

public class PlaySound : MonoBehaviour
{
    [SerializeField] private AudioSource windSound;
    [SerializeField] private AudioSource basaltSound;
    
    public void PlayWind()
    {
        windSound.Play();
    }

    public void PlayBasalt()
    {
        basaltSound.Play();
    }
}
