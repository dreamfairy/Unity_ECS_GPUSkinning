using UnityEngine;
using Unity.Entities;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Collections;

/// <summary>
/// Author:Aoicocoon
/// Date:20200907
/// Entity生成器组件转换
/// </summary>
namespace Aoi.ECS
{
    public sealed class ECS_AnimatorEntityGenerator : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
    {
        public GameObject Prefab;
        public int CountX;
        public int CountY;
        public float4 LODDistance = new float4(10f, 1000f, 1000f, 1000f);

        /// <summary>
        /// 生成结构为
        /// Primary Entity (为ECS_SkinnedMatrixAnimator,MeshLODGroupComponent组件所在)
        /// |-Attach0
        /// |       |-LOD0 (RenderMesh, RenderBound, MeshLODComponent)
        /// |       |-LOD1
        /// |
        /// |-Attach1
        /// |       |-LOD0
        /// |       |-LOD1
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="dstManager"></param>
        /// <param name="conversionSystem"></param>
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var spawnerData = new ECS_AnimatorSpawner
            {
                //主Entity 为ECS_SkinnedMatrixAnimator组件所在
                Prefab = conversionSystem.GetPrimaryEntity(Prefab),
                CountX = CountX,
                CountY = CountY,
            };

            if (true)
            {
                GeneratorAttach(ref spawnerData, dstManager);
            }

            dstManager.AddComponentData(entity, spawnerData);
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(Prefab);
        }

        void GeneratorAttach(ref ECS_AnimatorSpawner spawnerData, EntityManager dstManager)
        {
            ECS_Provider provider = Prefab.GetComponent<ECS_Provider>();

            spawnerData.AttachCount = provider.AttachDatas.Length;

            List<Entity> LODList = new List<Entity>();

            for (int attach = 0; attach < spawnerData.AttachCount; attach++)
            {
                AttachData attachData = provider.AttachDatas[attach];
                GeneratorLOD(ref spawnerData, dstManager, LODList, attach, attachData);
            }

            spawnerData.AttachLOD = GeneratorEntityArr(LODList.ToArray());
        }

        void GeneratorLOD(ref ECS_AnimatorSpawner spawnerData, EntityManager dstManager, List<Entity> lodEntityList, int attachIndex, AttachData attachData)
        {
            dstManager.AddComponentData(spawnerData.Prefab, new MeshLODGroupComponent()
            {
                LODDistances0 = LODDistance,
                LODDistances1 = LODDistance,
                LocalReferencePoint = float3.zero
            });

            for (int lod = 0; lod < 2; lod++)
            {
                Entity lodEntity = Entity.Null;
                GeneratorSubLOD(ref spawnerData, ref lodEntity, ref spawnerData.Prefab, lod, dstManager, attachIndex, attachData);
                lodEntityList.Add(lodEntity);
            }
        }

        void GeneratorSubLOD(ref ECS_AnimatorSpawner spawnData, ref Entity output, ref Entity hostEntity, int lod, EntityManager dstManager, int attachIndex, AttachData attachData)
        {
            var treeType = dstManager.CreateArchetype(
              typeof(RenderMesh), // Rendering mesh
              typeof(LocalToWorld), // Needed for rendering
              typeof(Translation), // Transform position
              typeof(Rotation), // Transform rotation
              typeof(Scale), // Transform scale (version with X, Y and Z)          
              typeof(RenderBounds), //Bounds to tell the Renderer where it is
              typeof(MeshLODComponent) //The actual LOD Component
            );

            Entity childOfGroup = dstManager.CreateEntity(treeType);

            List<Mesh> lodMesh = new List<Mesh>();
            lodMesh.Add(attachData.LODMesh[0]);

            if (attachData.LODMesh.Length > 0)
            {
                lodMesh.Add(attachData.LODMesh[1]);
            }

            //需要创建lod,且有lod数据
            if (lodMesh.Count > 1)
            {
                List<Material> lodMaterial = new List<Material>();
                for (int i = 0; i < lodMesh.Count; i++)
                {
                    lodMaterial.Add(attachData.DrawMaterial);
                }

                float4 lodDistances = LODDistance;//new float4(10f, 10000f, 10000f, 10000f);
                float3 position = new float3(0, 5, 0);

                CreateLODEntity(ref childOfGroup, ref hostEntity, lod, dstManager, lodMesh.Count, lodMesh.ToArray(), lodMaterial.ToArray(), lodDistances, position);
            }

            output = childOfGroup;
        }

        void CreateLODEntity(ref Entity childOfGroup, ref Entity hostEntity, int lodLevel, EntityManager dstManager, int lodCout, Mesh[] lodMesh, Material[] lodMat, float4 lodDistances, float3 position)
        {
            RenderMesh renderMesh = new RenderMesh
            {
                mesh = lodMesh[lodLevel],
                material = lodMat[lodLevel],
                subMesh = 0,
                castShadows = UnityEngine.Rendering.ShadowCastingMode.Off,
                receiveShadows = false,
            };

            RenderBounds bounds = new RenderBounds
            {
                Value = GetBoxRenderBounds(new float3(1, 1, 1))
            };


            //dstManager.SetDebugName(childOfGroup, "AnimatorMeshGroupPrefab");

            dstManager.SetSharedComponentData(childOfGroup, renderMesh);

            dstManager.SetComponentData(childOfGroup, new LocalToWorld() { Value = float4x4.TRS(float3.zero, quaternion.identity, new float3(1, 1, 1)) });
            dstManager.SetComponentData(childOfGroup, new Translation() { Value = position });
            dstManager.SetComponentData(childOfGroup, bounds);

            dstManager.SetComponentData(childOfGroup, new MeshLODComponent { Group = hostEntity, LODMask = GetLODMask(lodLevel) });
        }

        AABB GetBoxRenderBounds(float3 size)
        {
            var aabb = new AABB();
            aabb.Center = float3.zero;
            aabb.Extents = size;
            return aabb;
        }

        int GetLODMask(int index)
        {
            switch (index)
            {
                case 0:
                    return 0x01;

                case 1:
                    return 0x02;

                case 2:
                    return 0x04;

                case 3:
                    return 0x08;


                default:
                    return 0x08;
            }
        }

        BlobAssetReference<EntityArray> GeneratorEntityArr(Entity[] baseData)
        {
            using (var builder = new BlobBuilder(Allocator.Temp))
            {
                ref var root = ref builder.ConstructRoot<EntityArray>();
                var refArr = builder.Allocate(ref root.ArrayData, baseData.Length);
                for (int i = 0; i < baseData.Length; i++)
                {
                    refArr[i] = baseData[i];
                }
                return builder.CreateBlobAssetReference<EntityArray>(Allocator.Persistent);
            }
        }
    }
}
