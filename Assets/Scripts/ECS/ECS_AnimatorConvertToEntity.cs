using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using Unity.Rendering;
using Unity.Transforms;

/// <summary>
/// Author:Aoicocoon
/// Date:20200908
/// Aoi Animator ->ECS 转换器
/// </summary>
namespace Aoi.ECS
{
    public sealed class ECS_AnimatorConvertToEntity : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            ECS_Provider provider = this.GetComponent<ECS_Provider>();

            FillEntityWithAnimatorComponent(entity, dstManager, provider);
        }

        void FillEntityWithAnimatorComponent(Entity entity, EntityManager dstManager, ECS_Provider provider)
        {
            List<int> attachSmrCount = new List<int>();
            List<float3> attachTexData = new List<float3>();
            List<float4> frameData = new List<float4>();

            //动画帧间隔
            int[] frameOffsetArr = new int[provider.AnimNames.Length * provider.AttachDatas.Length];

            Dictionary<int, Material> needCreateNewMatDict = new Dictionary<int, Material>();

            for (int attach = 0; attach < provider.AttachDatas.Length; attach++)
            {
                Texture2D animTex = provider.AttachDatas[attach].AnimTexture;

                if (null != animTex)
                {
                    attachSmrCount.Add(provider.AttachDatas[attach].SMRCount);
                    attachTexData.Add(new float3(animTex.width, animTex.height, math.min(1.0f / animTex.width, 1.0f / animTex.height)));
                    frameData.Add(new float4());

                    int numPixels = 0;
                    for (int i = 0; i < provider.AnimNames.Length; i++)
                    {
                        int frameCount = provider.AnimFrameCount[i];
                        int curAnimPixelStart = numPixels;
                        numPixels += frameCount * provider.AttachDatas[attach].SMRCount * 2;
                        frameOffsetArr[(attach * provider.AnimNames.Length) + i] = curAnimPixelStart;
                    }

                    Material m = provider.AttachDatas[attach].DrawMaterial;
                    m.SetTexture("_AnimTex", animTex);
                    m.EnableKeyword("_ECS_ON");
                }
                else
                {
                    Debug.LogErrorFormat("Missing InstancingAnimTex" + (provider as MonoBehaviour).name);
                }
            }

            var animData = default(AnimationData);
            animData.AnimationIndex = 0;
            animData.StartTime = 0;
            animData.AnimCount = provider.AnimNames.Length;
            animData.AnimationFrameOffsetArr = GeneratorIntArr(frameOffsetArr);
            animData.AnimationFrameCountArr = GeneratorIntArr(provider.AnimFrameCount);
            animData.FPS = 1.0f / 30.0f;
            animData.AttachCount = provider.AttachDatas.Length;
            animData.AttachSmrCountArr = GeneratorIntArr(attachSmrCount.ToArray());
            animData.AttachTexDataArr = GeneratorFloat3Arr(attachTexData.ToArray());

            dstManager.AddComponentData(entity, animData);

            var animator = new ECS_SkinnedMatrixAnimator();
            dstManager.AddSharedComponentData(entity, animator);
        }

        BlobAssetReference<IntArray> GeneratorIntArr(int[] baseData)
        {
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<IntArray>();
                var refArr = builder.Allocate(ref root.ArrayData, baseData.Length);
                for (int i = 0; i < baseData.Length; i++)
                {
                    refArr[i] = baseData[i];
                }
                return builder.CreateBlobAssetReference<IntArray>(Allocator.Persistent);
            }
        }

        BlobAssetReference<Float3Array> GeneratorFloat3Arr(float3[] baseData)
        {
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<Float3Array>();
                var refArr = builder.Allocate(ref root.ArrayData, baseData.Length);
                for (int i = 0; i < baseData.Length; i++)
                {
                    refArr[i] = baseData[i];
                }
                return builder.CreateBlobAssetReference<Float3Array>(Allocator.Persistent);
            }
        }

        BlobAssetReference<Float4Array> GeneratorFloat3Arr(float4[] baseData)
        {
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<Float4Array>();
                var refArr = builder.Allocate(ref root.ArrayData, baseData.Length);
                for (int i = 0; i < baseData.Length; i++)
                {
                    refArr[i] = baseData[i];
                }
                return builder.CreateBlobAssetReference<Float4Array>(Allocator.Persistent);
            }
        }
    }
}
