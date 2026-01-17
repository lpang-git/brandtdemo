using UnityEngine;
using UnityEngine.Serialization;

public class AmmoSaver : MonoBehaviour
{
    internal static AmmoSaver Instance { get; set; }

    [SerializeField] internal int chambered;
    [SerializeField] internal int unchambered;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
