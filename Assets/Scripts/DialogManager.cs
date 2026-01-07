using UnityEngine;
using TMPro;
using System.Collections;

public class DialogManager : MonoBehaviour
{
    public static DialogManager Instance;

    [Header("UI")]
    public GameObject dialogPanel;
    public TextMeshProUGUI dialogText;

    [Header("Typewriter Settings")]
    public float typingSpeed = 0.03f;

    private string[] currentLines;
    private int currentLineIndex;
    private bool isDialogActive;
    private bool isTyping;

    private Coroutine typingCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void StartDialog(string[] lines)
    {
        if (lines == null || lines.Length == 0)
            return;

        currentLines = lines;
        currentLineIndex = 0;

        dialogPanel.SetActive(true);
        isDialogActive = true;

        StartTypingCurrentLine();
    }

    public void NextLine()
    {
        // Wenn gerade geschrieben wird → sofort fertig anzeigen
        if (isTyping)
        {
            SkipTyping();
            return;
        }

        currentLineIndex++;

        if (currentLineIndex >= currentLines.Length)
        {
            EndDialog();
        }
        else
        {
            StartTypingCurrentLine();
        }
    }

    private void StartTypingCurrentLine()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeLine(currentLines[currentLineIndex]));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogText.text = "";

        foreach (char c in line)
        {
            dialogText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogText.text = currentLines[currentLineIndex];
        isTyping = false;
    }

    public void EndDialog()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogPanel.SetActive(false);
        dialogText.text = "";
        currentLines = null;
        currentLineIndex = 0;
        isDialogActive = false;
        isTyping = false;
    }

    public bool IsDialogActive()
    {
        return isDialogActive;
    }
}