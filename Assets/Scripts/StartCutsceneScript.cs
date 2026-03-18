using System;
using UnityEngine;

public class StartCutsceneScript : MonoBehaviour
{
    private static readonly int BossTrigger = Animator.StringToHash("bossTrigger");
    
    [SerializeField] private Animator cutscene;

    private void Start()
    {
        cutscene.SetTrigger(BossTrigger);
    }
}
