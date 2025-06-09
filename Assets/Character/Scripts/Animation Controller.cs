using System;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    private Animator animator;

    private Action _onAttackFinished;
    public Action onAttackFinished
    {
        get { return _onAttackFinished; }
        set { _onAttackFinished = value; }
    }

    private Action _onEvadeFinished;
    public Action onEvadeFinished
    {
        get { return _onEvadeFinished; }
        set { _onEvadeFinished = value; }
    }

    private Action _onGetAttackFinished;
    public Action onGetAttackFinished
    {
        get { return _onGetAttackFinished; }
        set { _onGetAttackFinished = value; }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayWalk()
    {
        animator.SetBool("isWalk", true);
    }

    public void StopWalk()
    {
        animator.SetBool("isWalk", false);
    }

    public void PlayRun()
    {
        animator.SetBool("isRun", true);
    }

    public void StopRun()
    {
        animator.SetBool("isRun", false);
    }

    public void PlayAttack()
    {
        animator.SetTrigger("IsAttacking");
    }

    public void StopAttack()
    {
        animator.SetTrigger("IsAttackFinish");
    }

    public void PlayIdle()
    {
        animator.SetFloat("Speed", 0f);
    }

    public void PlayGetAttack()
    {
        animator.SetBool("getAttack", true);
    }

    public void StopGetAttack()
    {
        animator.SetBool("getAttack", false);
    }

    public void PlayCrouch()
    {
        animator.SetBool("isCrouch", true);
    }

    public void StopCrouch()
    {
        animator.SetBool("isCrouch", false);
    }

    public void OnAttackAnimationFinished()
    {
        _onAttackFinished?.Invoke();
    }

    public void OnEvadeAnimationFinished()
    {
        _onEvadeFinished?.Invoke();
    }

    public void OnGetAttackAnimationFinished()
    {
        _onGetAttackFinished?.Invoke();
    }
}
