using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueCharacter
{
    public string name;
    public Sprite sprite;
}

[Serializable]
public class DialogueLine
{
    public DialogueCharacter character;
    [TextArea(3, 10)]
    public string line;
}

[Serializable]
public class Dialogue
{
    public List<DialogueLine> lines =  new();
}

public class DialogueTrigger : MonoBehaviour
{
    public Dialogue dialogue;

    private void TriggerDialogue()
    {
        DialogueManager.Instance.StartDialogue(dialogue);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TriggerDialogue();
        }
    }
}