using UnityEngine;

public class PlaySound : MonoBehaviour
{
    [SerializeField] private AudioSource sound;
    
    public void PlayEerie()
    {
        sound.Play();
    }
}
