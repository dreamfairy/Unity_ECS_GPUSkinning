using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using System;
using Unity.Transforms;
/// <summary>
/// Author:Aoicocooon
/// Date:20200907
/// 基于ECS实现的 SkinnedMatrixAnimator数据
/// </summary>
namespace Aoi.ECS
{
    public struct AnimationData : IComponentData
    {
        public int AnimationIndex;
        public int AttachCount;
        public int AnimCount;
        public int ActivedLOD;
        public int CurFrame;
        public float StartTime;
        public float FPS;
        public float3 Position;
        public BlobAssetReference<IntArray> AnimationFrameOffsetArr; //每个动画的帧数据偏移量索引开始值
        public BlobAssetReference<IntArray> AnimationFrameCountArr; //每个动画的总帧数
        public BlobAssetReference<IntArray> AttachSmrCountArr; //骨骼数
        public BlobAssetReference<Float3Array> AttachTexDataArr;
        

        /// <summary>
        /// 计算这帧数normalizeTime
        /// </summary>
        /// <param name="frameCount"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public float CalcNormalizeTime(int frameCount, float time)
        {
            float curAnimationLength = frameCount * FPS;
            return Mathf.Repeat(time, curAnimationLength) / curAnimationLength;
        }
    }

    public struct ECS_AttachData
    {
        public BlobAssetReference<IntArray> AnimationFrameOffsetArr; //每个动画的帧数据偏移量索引开始值
        public int SmrCountArr; //骨骼数
    }

    public struct FloatArray
    {
        public BlobArray<float> ArrayData;
    }

    public struct Float3Array
    {
        public BlobArray<float3> ArrayData;
    }

    public struct Float4Array
    {
        public BlobArray<float4> ArrayData;
    }

    public struct IntArray
    {
        public BlobArray<int> ArrayData;
    }

    public struct EntityArray
    {
        public BlobArray<Entity> ArrayData;
    }

    public struct AttachDataArr
    {
        public BlobArray<ECS_AttachData> ArrayData;
    }

    [InternalBufferCapacity(4)]
    public struct EntityBuffer : IBufferElementData
    {
        public Entity entity;
    }

    public struct AnimatorCurrentFrame : IComponentData
    {
        public int CurrentFrame;
    }

    public struct FrameData : IComponentData
    {
        //每帧数据，根据 texWidth, texHeight, frameOffset, min(1/w, 1.h) 填充
        public float4 PerFrameData;
    }

    /// <summary>
    /// 士兵类型
    /// 参照枚举 Aoi.TroopType
    /// </summary>
    public struct TroopTypeData : IComponentData
    {
        public int TypeValue;
    }

    //当前激活的LODAttach
    public struct ECS_ActivedLODAttach : IComponentData
    {

    }

    /// <summary>
    /// 所有要跟随ECS_SkinnedMatrixAnimator移动的部件，都要拥有此组件
    /// 比如Animator下 LODGroup, SubMesh 等
    /// </summary>
    [Serializable]
    [WriteGroup(typeof(LocalToWorld))]
    public struct ECS_SkinnedMatrixAnimatorAttach : IComponentData
    {
        public int AttachIndex;
        public Entity Parent;
    }

    [Serializable]
    [WriteGroup(typeof(LocalToWorld))]
    struct ECS_SkinnedMatrixAnimator : ISharedComponentData
    {
       
    }

    struct ECS_SkinnedMatrixAnimatorChildren : ISharedComponentData
    {
        
    }
}