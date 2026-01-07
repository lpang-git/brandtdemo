using UnityEngine;

public class BossController : MonoBehaviour
{
    private static readonly int LeftAttack = Animator.StringToHash("leftAttack");
    private static readonly int RightAttack = Animator.StringToHash("rightAttack");
    private static readonly int OverheadAttack = Animator.StringToHash("roofAttack");
    
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        
    }

    #region Attacks

        private void LeftHandSmash()
        {
            _animator.SetBool(LeftAttack, true);
        }
        
        private void RightHandSmash()
        {
            _animator.SetBool(RightAttack,  true);
        }
        
        private void OverheadSmash()
        {
            _animator.SetBool(OverheadAttack, true);
        }
        
        private void OnRoofSmashed()
        {
            
        }
    
        private void OnAttackFinished()
        {
            
        }

    #endregion

    #region Monitors & Health Manager

    

    #endregion
}
