using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;

using Random = Unity.Mathematics.Random;

/// <summary>
/// Author:Aoicocoon
/// Date:20200907
/// 实体生成系统
/// </summary>
namespace Aoi.ECS
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public sealed class ECS_AnimatorEntitySpawnerSystem : SystemBase
    {
        BeginInitializationEntityCommandBufferSystem m_EntityCommandBufferSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EntityCommandBufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var command = m_EntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();

            //Instantiate 士兵预制体
            Entities
                .WithName("ECS_AmimatorEntitySpawnerSystem")
                .WithBurst(Unity.Burst.FloatMode.Default, Unity.Burst.FloatPrecision.Standard, true)
                .ForEach(
                (Entity entity, int entityInQueryIndex, int nativeThreadIndex, in ECS_AnimatorSpawner spawner, in LocalToWorld location) =>
                {
                    for (var x = 0; x < spawner.CountX; x++)
                    {
                        for (var y = 0; y < spawner.CountY; y++)
                        {
                            var instance = command.Instantiate(entityInQueryIndex, spawner.Prefab);

                            //Aniamtor出生位置
                            var position = math.transform(location.Value,
                            new float3(x * 1.3F, 0, y * 1.3F));
                            command.SetComponent(entityInQueryIndex, instance, new Translation() { Value = position });

                            //模型部件分拆 需要独立实例化，比如骑兵的 马和人 是2个attach
                            for (int attach = 0; attach < spawner.AttachCount; attach++)
                            {
                                //初始化每个attach的 lod 网格
                                var lodEntity = Entity.Null;
                                for (int lod = 0; lod < 2; lod++)
                                {
                                    lodEntity = command.Instantiate(entityInQueryIndex, spawner.AttachLOD.Value.ArrayData[attach * 2 + lod]);

                                    //建立 MeshLODComponent-> 到 MeshLODGroup 的父子级关系
                                    command.SetComponent(entityInQueryIndex, lodEntity, new MeshLODComponent { Group = instance, LODMask = spawner.GetLODMask(lod) });
                                    //建立 AnimatorAttach-> 到 Animator 的父子级关系
                                    command.AddComponent(entityInQueryIndex, lodEntity, new ECS_SkinnedMatrixAnimatorAttach() { Parent = instance, AttachIndex = attach });
                                    command.AddComponent(entityInQueryIndex, lodEntity, new ECS_FrameDataMaterialPropertyComponent());
                                } 
                            }
                        }
                    }

                    //清理预制体
                    command.DestroyEntity(entityInQueryIndex, entity);
                    // command.DestroyEntity(entityInQueryIndex, spawner.Prefab);

                    for (int lod = 0; lod < spawner.AttachLOD.Value.ArrayData.Length; lod++)
                    {
                        command.DestroyEntity(entityInQueryIndex, spawner.AttachLOD.Value.ArrayData[lod]);
                    }
                }).ScheduleParallel();

            m_EntityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
        }
    }
}
