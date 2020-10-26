using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public sealed class ECS_Provider : MonoBehaviour
{
    public AttachData[] AttachDatas;
    public string[] AnimNames;
    public int[] AnimFrameCount;
}

[System.Serializable]
public class AttachData
{
    public Mesh[] LODMesh;
    public Material DrawMaterial;
    public Texture2D AnimTexture;
    public int SMRCount;
}