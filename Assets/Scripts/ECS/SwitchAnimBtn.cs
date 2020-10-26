using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Aoi.ECS;

public sealed class SwitchAnimBtn : MonoBehaviour
{
    private void Start()
    {
       
    }

    private void OnGUI()
    {

    }

    public void OnClickBtn()
    {
        ECS_AnimatorEntityRenderingSystem renderingSys = World.DefaultGameObjectInjectionWorld.GetExistingSystem<ECS_AnimatorEntityRenderingSystem>();
        if (null != renderingSys)
        {
            renderingSys.SwitchAnim();
        }
    }
}