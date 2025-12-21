using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("References")]
    [SerializeField] private TextMeshProUGUI charName;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Image icon;
    //[SerializeField] private Animator animator;
    
    [Space]
    [Header("Settings")]
    [SerializeField] private float textTypingSpeed = 0.2f;
    
    private Queue<DialogueLine> _dialogueLines;
    private bool _isDialogueActive;
    
    [SerializeField] private AudioClip dialogueSkip;

    private void Start()
    {
        Instance = this;
    }

    internal void StartDialogue(Dialogue dialogue)
    {
        _isDialogueActive = true;
        
        //animator.SetBool("dialogueActive", true);
        _dialogueLines.Clear();

        foreach (var c in dialogue.lines)
        {
            _dialogueLines.Enqueue(c);
        }

        DisplayNextDialogueLine();
    }

    private void DisplayNextDialogueLine()
    {
        if (_dialogueLines.Count == 0)
        {
            EndDialogue();
            return;
        }
        
        var currentLine = _dialogueLines.Dequeue();

        icon.sprite = currentLine.character.sprite;
        charName.text = currentLine.character.name;
        
        StopAllCoroutines();
        
        StartCoroutine(TypeSentence(currentLine));
    }

    private IEnumerator TypeSentence(DialogueLine dialogueLine)
    {
        dialogueText.text = "";
        foreach (var letter in dialogueLine.line.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(textTypingSpeed);
        }
    }

    private void EndDialogue()
    {
        _isDialogueActive = false;
        //animator.SetBool("dialogueActive", false);
    }
    
    /*private bool IsMouseOverUI()
    {
        // Check if the mouse is over a UI element
        if (!EventSystem.current.IsPointerOverGameObject()) return false; // Not over a UI element
        
        // Perform a raycast to detect the specific UI element under the mouse
        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        // Check if the clicked UI element is the speech bubble
        foreach (var result in results)
        {
            if (result.gameObject.CompareTag("SpeechBubble"))
            {
                return false; // Allow dialogue to continue
            }
        }

        return true; // Block other UI elements

    }*/
}
