using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;
using Random = Unity.Mathematics.Random;

/// <summary>
/// Author:Aoicocoon
/// Date:20200907
/// 实体渲染系统
/// </summary>
namespace Aoi.ECS
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public sealed class ECS_AnimatorEntityFrameDataCalcSystem : SystemBase
    {
        private EntityQuery m_calcAnimatorFrameDataGroup;
        private EntityQuery m_finalGroup;
        private float m_time;
       
        protected override void OnCreate()
        {
            base.OnCreate();

            m_calcAnimatorFrameDataGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
               {
                    typeof(LocalToWorld),
                    typeof(AnimationData),
                    ComponentType.ReadOnly<MeshLODGroupComponent>(),
                    ComponentType.ReadOnly<Translation>(),
               }
            });

            m_finalGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
              {
                      typeof(LocalToWorld),
                      typeof(AnimationData),
                      ComponentType.ReadOnly<ECS_SkinnedMatrixAnimatorAttach>(),
                      ComponentType.ReadOnly<MeshLODComponent>()
              }
            });
        }

        protected override void OnUpdate()
        {
            float dt = UnityEngine.Time.deltaTime;
            UnityEngine.Camera camera = UnityEngine.Camera.main;
            LODGroupExtensions.LODParams lodPrams = default;
            if (camera)
            {
                lodPrams = Unity.Rendering.LODGroupExtensions.CalculateLODParams(camera);
            }

            ComponentDataFromEntity<AnimationData> animationData = GetComponentDataFromEntity<AnimationData>(true);
            ComponentDataFromEntity<Translation> translationData = GetComponentDataFromEntity<Translation>(true);
            ComponentDataFromEntity<Rotation> rotationData = GetComponentDataFromEntity<Rotation>(true);
            //ComponentDataFromEntity<Scale> scaleData = GetComponentDataFromEntity<Scale>(true);

            Entities
                .WithName("ECS_AnimatorEntityFrameDataCalcSystem")
                .WithBurst(Unity.Burst.FloatMode.Default, Unity.Burst.FloatPrecision.Standard)
                .WithStoreEntityQueryInField(ref m_calcAnimatorFrameDataGroup)
                .ForEach((ref LocalToWorld location, ref AnimationData animData, ref MeshLODGroupComponent lodGrpup, in Translation translation) =>
                {
                    var lodIndex = LODGroupExtensions.CalculateCurrentLODIndex(lodGrpup.LODDistances0, translation.Value, ref lodPrams);
                    animData.ActivedLOD = lodIndex + 1;

                    int frameCount = animData.AnimationFrameCountArr.Value.ArrayData[animData.AnimationIndex];

                    animData.StartTime += dt;
                    float normalizeTime = animData.CalcNormalizeTime(frameCount, animData.StartTime);
                    animData.CurFrame = (int)(normalizeTime * frameCount);

                }).ScheduleParallel();

            

            Entities
                .WithName("ECS_AnimatorEntityAttachRenderingSystem")
                .WithBurst(Unity.Burst.FloatMode.Default, Unity.Burst.FloatPrecision.Standard)
                .WithStoreEntityQueryInField(ref m_finalGroup)
                .WithReadOnly(animationData)
                .WithReadOnly(translationData)
                .WithReadOnly(rotationData)
                //.WithReadOnly(scaleData)
                .ForEach((Entity entity, int entityInQueryIndex, ref LocalToWorld location, ref ECS_FrameDataMaterialPropertyComponent frameDataMaterialProerty, in ECS_SkinnedMatrixAnimatorAttach attach, in MeshLODComponent lod) =>
                {
                    AnimationData animData = animationData[attach.Parent];

                    //未激活的LOD attach return
                    if((lod.LODMask & animData.ActivedLOD) == 0)
                    {
                        return;
                    }

                    int frameInPixelOffset = animData.AnimationFrameOffsetArr.Value.ArrayData[(attach.AttachIndex * animData.AnimCount) + animData.AnimationIndex];
                    int smrOffset = animData.AttachSmrCountArr.Value.ArrayData[attach.AttachIndex];
                    float3 texData = texData = animData.AttachTexDataArr.Value.ArrayData[attach.AttachIndex];

                    int frameOffset = frameInPixelOffset + animData.CurFrame * smrOffset * 2;
                    float4 perFrameData = new float4(texData.x, texData.y, frameOffset, texData.z);

                    Translation animatorTranslateData = translationData[attach.Parent];
                    Rotation animatorRotationData = rotationData[attach.Parent];
                    //Scale animatorScaleData = scaleData[attach.Parent];
                    float3 position = animatorTranslateData.Value;

                    float4x4 trs = float4x4.TRS(animatorTranslateData.Value, animatorRotationData.Value, new float3(1,1,1));

                    frameDataMaterialProerty.Value = perFrameData;

                    location.Value = trs;

                }).ScheduleParallel();
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ECS_AnimatorEntityFrameDataCalcSystem))]
    public sealed class ECS_AnimatorEntityRenderingSystem : SystemBase
    {
        private bool m_switchAnimIsDirty = false;

        protected override void OnCreate()
        {
            base.OnCreate();
        }

        public void SwitchAnim()
        {
            m_switchAnimIsDirty = true;
        }
        protected override void OnUpdate()
        {
            if (m_switchAnimIsDirty)
            {
                Random random = new Random((uint)UnityEngine.Time.frameCount);
                m_switchAnimIsDirty = false;

                Entities
              .WithName("ECS_AnimatorEntityRenderingSystem_switchAnim")
              .WithBurst(Unity.Burst.FloatMode.Default, Unity.Burst.FloatPrecision.Standard)
              .ForEach((ref AnimationData animData) =>
              {
                  animData.AnimationIndex = random.NextInt(0, animData.AnimCount);
                  animData.StartTime = random.NextFloat(0.0f, 1.0f);
              }).ScheduleParallel();
            }
        }
    }
}
