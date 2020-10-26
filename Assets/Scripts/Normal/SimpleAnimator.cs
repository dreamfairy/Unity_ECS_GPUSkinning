using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SimpleAnimator : MonoBehaviour
{
    public static float FPS = 1.0f / 30.0f;

    protected MeshFilter[] m_subPartMF;
    protected Renderer[] m_subPartRenderer;
    protected Material[] m_subPartMaterial;

    protected float m_deltaTime;
    protected int m_playAnimIdx = 0;
    protected int m_playAnimFrameIdx = 0;
    protected int m_playedAnimFrameCount;
    protected bool m_hasInited = false;

    protected int[][] InstancingTexFrameOffset = null;
    protected int[] InstancingSmrCount = null;
    protected Vector4[] InstancingData;
    protected ECS_Provider m_Provider;
    protected MaterialPropertyBlock m_block;

    protected static int TEXSKINNING_PIXEL_START = Shader.PropertyToID("_FrameData");

    public void InitData(ECS_Provider pSkin)
    {
        m_subPartMF = new MeshFilter[pSkin.AttachDatas.Length];
        m_subPartRenderer = new Renderer[pSkin.AttachDatas.Length];
        m_subPartMaterial = new Material[pSkin.AttachDatas.Length];
        InstancingTexFrameOffset = new int[pSkin.AnimNames.Length][];
        InstancingSmrCount = new int[pSkin.AttachDatas.Length];
        InstancingData = new Vector4[pSkin.AttachDatas.Length];

        for (int i = 0; i < pSkin.AttachDatas.Length; i++)
        {
            GameObject attach = new GameObject(i.ToString(), typeof(MeshFilter), typeof(MeshRenderer));
            attach.transform.parent = this.transform;
            attach.transform.localPosition = Vector3.zero;

            Renderer renderer = attach.GetComponent<MeshRenderer>();
            MeshFilter mf = attach.GetComponent<MeshFilter>();
            Material mat = pSkin.AttachDatas[i].DrawMaterial;

            mf.sharedMesh = pSkin.AttachDatas[i].LODMesh[0];
            renderer.sharedMaterial = mat;

            m_subPartRenderer[i] = renderer;
            m_subPartMF[i] = mf;
            m_subPartMaterial[i] = mat;

            Texture2D animTex = pSkin.AttachDatas[i].AnimTexture;
            if (animTex)
            {
                InstancingData[i] = new Vector4(animTex.width, animTex.height, 0, Mathf.Min(1.0f / animTex.width, 1.0f / animTex.height));
            }

            mat.DisableKeyword("_ECS_ON");

            InstancingSmrCount[i] = pSkin.AttachDatas[i].SMRCount;
            InstancingTexFrameOffset[i] = new int[pSkin.AnimNames.Length];
            int numPixels = 0;
            for (int j = 0; j < pSkin.AnimNames.Length; j++)
            {
                string animName = pSkin.AnimNames[j];
                int frameCount = pSkin.AnimFrameCount[j];
                int curAnimPixelStart = numPixels;
                numPixels += frameCount * pSkin.AttachDatas[i].SMRCount * 2;
                InstancingTexFrameOffset[i][j] = curAnimPixelStart;
            }

            m_Provider = pSkin;
            m_playAnimIdx = -1;
            m_block = new MaterialPropertyBlock();
            m_hasInited = true;
        }
    }

    public Vector4 CalcInstancingFrameOffset(int attachIdx, int animIdx, int frameIdx)
    {
        if (null == InstancingTexFrameOffset)
        {
            return Vector4.zero;
        }

        int frameOffset = InstancingTexFrameOffset[attachIdx][animIdx] + frameIdx * InstancingSmrCount[attachIdx] * 2;

        Vector4 data = InstancingData[attachIdx];
        data.z = frameOffset;
        return data;
    }

    public void PlayAnim(int animIdx)
    {
        m_playAnimIdx = animIdx;
        m_playedAnimFrameCount = m_Provider.AnimFrameCount[animIdx];
    }

    public void Update()
    {
        if (!m_hasInited || -1 == m_playAnimIdx)
        {
            return;
        }

        m_deltaTime += Time.deltaTime;
        if(m_deltaTime > FPS)
        {
            m_deltaTime = 0;
            int frame = m_playAnimFrameIdx++ % m_playedAnimFrameCount;
            for (int i = 0; i < m_subPartRenderer.Length; i++)
            {
                Vector4 data = this.CalcInstancingFrameOffset(i, m_playAnimIdx, frame);
                m_block.SetVector(TEXSKINNING_PIXEL_START, data);
                m_subPartRenderer[i].SetPropertyBlock(m_block);
            }
        }
    }
}