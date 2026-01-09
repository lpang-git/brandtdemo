using UnityEngine;

public class NPCDialog : MonoBehaviour
{
    [TextArea(3, 6)]
    public string[] dialogLines;

    public Sprite npcIcon;

    private bool playerInRange;

    private void Update()
    {
        if (!playerInRange)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!DialogManager.Instance.IsDialogActive())
            {
                DialogManager.Instance.StartDialog(dialogLines, npcIcon);
            }
            else
            {
                DialogManager.Instance.NextLine();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            DialogManager.Instance.EndDialog();
        }
    }
}