using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public sealed class NormalAnimatorLaunch : MonoBehaviour
{
    public void Start()
    {
        SimpleAnimator animator = this.gameObject.AddComponent<SimpleAnimator>();
        ECS_Provider provider = this.GetComponent<ECS_Provider>();
        animator.InitData(provider);
        animator.PlayAnim(0);
    }
}