using UnityEngine;
using UnityEngine.Serialization;

public class NPC : MonoBehaviour
{
    [FormerlySerializedAs("dialogLines")]
    [TextArea(3, 6)]
    [SerializeField] internal string[] dialogueLines;
    [SerializeField] internal Sprite npcIcon;
    [SerializeField] internal int sceneNumber = 0;
}