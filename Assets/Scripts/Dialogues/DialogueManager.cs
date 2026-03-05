using UnityEngine;
using TMPro;
using System.Collections;
using Platformer.Mechanics;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("References")]
    [FormerlySerializedAs("dialogPanel")] [SerializeField] private GameObject dialoguePanel;
    [FormerlySerializedAs("dialogText")] [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Image npcIcon;
    [SerializeField] private PlayerController player;

    [Header("Typewriter Settings")]
    [SerializeField] private float typingSpeed = 0.03f;

    private string[] _currentLines;
    private int _currentLineIndex;
    private bool _isDialogueActive;
    private bool _isTyping;
    internal InputAction interAction;
    internal bool canInteract;

    private NPC _npc;

    private Coroutine _typingCoroutine;

    private void Awake()
    {
        interAction = InputSystem.actions.FindAction("Player/Interact");
    }
    
    private void OnEnable()
    {
        interAction.Enable();
        interAction.performed += OnInteract;
    }
    
    private void OnDisable()
    {
        interAction.Disable();
        interAction.performed -= OnInteract;
    }
    
    private void OnInteract(InputAction.CallbackContext context)
    {
        Debug.Log("interacted");
        if (!player.playerInRangeOfDialogue)
            return;
        
        if (!canInteract)
            return;

        if (!IsDialogueActive())
        {
            StartDialogue(_npc.dialogueLines, _npc.npcIcon);
        }
        else
        {
            NextLine();
        }
    }

    internal void SetNPC(NPC newNpc)
    {
        _npc = newNpc;
    }

    private void StartDialogue(string[] lines, Sprite icon)
    {
        Debug.Log("dstrat");
        if (lines == null || lines.Length == 0)
            return;

        _currentLines = lines;
        _currentLineIndex = 0;

        dialoguePanel.SetActive(true);
        _isDialogueActive = true;

        if (icon != null)
        {
            npcIcon.sprite = icon;
            npcIcon.gameObject.SetActive(true);
        }
        else
        {
            npcIcon.gameObject.SetActive(false);
        }

        StartTypingCurrentLine();
    }

    private void NextLine()
    {
        // if texting → skip to full text
        if (_isTyping)
        {
            SkipTyping();
            return;
        }

        GetComponent<AudioSource>().Play(); // next page sfx
        _currentLineIndex++;

        if (_currentLineIndex >= _currentLines.Length)
        {
            EndDialogue(true);
        }
        else
        {
            StartTypingCurrentLine();
        }
    }

    private void StartTypingCurrentLine()
    {
        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);

        _typingCoroutine = StartCoroutine(TypeLine(_currentLines[_currentLineIndex]));
    }

    private IEnumerator TypeLine(string line)
    {
        _isTyping = true;
        dialogueText.text = "";

        foreach (var c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        _isTyping = false;
    }

    private void SkipTyping()
    {
        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);

        dialogueText.text = _currentLines[_currentLineIndex];
        _isTyping = false;
    }

    internal void EndDialogue(bool dialogueWasCalledAndFinished)
    {
        if (_typingCoroutine != null)
            StopCoroutine(_typingCoroutine);

        // dialogueWasCalledAndFinished is true when the player skipped through dialogue instead of just walking out of bounds of the NPC
        if (dialogueWasCalledAndFinished)
        {
            ChangeScene(_npc.sceneNumber);
        }
        
        dialoguePanel.SetActive(false);
        dialogueText.text = "";

        npcIcon.sprite = null;
        npcIcon.gameObject.SetActive(false);

        _currentLines = null;
        _currentLineIndex = 0;
        _isDialogueActive = false;
        _isTyping = false;
    }

    private bool IsDialogueActive()
    {
        return _isDialogueActive;
    }

    private void ChangeScene(int sceneIndex)
    {
        if (sceneIndex == 0) // if an NPC has sceneIndex 0, the dialogue is not supposed to change scenes.
            return;
        
        SceneManager.LoadScene(sceneIndex);
    }
}