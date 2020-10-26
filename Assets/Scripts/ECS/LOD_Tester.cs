using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;
using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;


public sealed class LOD_Tester : MonoBehaviour
{
    //lazy configuration from the inspector

    [Tooltip("The material to apply for all detail levels.")]
    public UnityEngine.Material MeshMaterial;

    [Tooltip("The most detailed mesh, LOD Level 0.")]
    public Mesh Lod1;
    [Tooltip("The mesh for LOD Level 1.")]
    public Mesh Lod2;
    [Tooltip("The mesh for LOD Level 2.")]
    public Mesh Lod3;
    [Tooltip("The mesh for LOD Level 3.")]
    public Mesh Lod4;

    [Tooltip("Instantiate 1000 entities with random positions.")]
    public bool placeRandomTestEntities = false;

    [Tooltip("Create the LOD Distances. The first number applies from position 0 to e.g. 10f for LOD0.")]
    public float4 lodDistances = new float4(10f, 20f, 30f, 100f);

    //Temporary data from the inspector for a later ordered access.
    [System.Serializable]
    public struct LodElelement
    {
        public Mesh mesh;
        public UnityEngine.Material mat;
    }

    private void Start()
    {
        List<LodElelement> lodElements = new List<LodElelement>();
        //Get the data for the materials and the meshes to a list. The material here is always the same.
        lodElements.Add(new LodElelement { mesh = Lod1, mat = MeshMaterial });
        lodElements.Add(new LodElelement { mesh = Lod2, mat = MeshMaterial });
        lodElements.Add(new LodElelement { mesh = Lod3, mat = MeshMaterial });
        lodElements.Add(new LodElelement { mesh = Lod4, mat = MeshMaterial });



        //Crate a combined LOD entity. This will crate 5 single entities.
        CreateLodEntity(new float3(0, 5, 0), lodElements, lodDistances);

        if (placeRandomTestEntities == true)
        {
            float3 min = new float3(-10, -10, -10);
            float3 max = new float3(10, 10, 10);

            for (int i = 0; i < 1000; i++)
            {
                float3 myVector = new Vector3(UnityEngine.Random.Range(min.x, max.x), UnityEngine.Random.Range(min.y, max.y), UnityEngine.Random.Range(min.z, max.z));
                CreateLodEntity(myVector, lodElements, lodDistances);
            }
        }
    }

    void CreateLodEntity(float3 position, List<LodElelement> lodElements, float4 lodDistances)
    {


        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        //Create a LOD group Component.
        var lodGroupType = entityManager.CreateArchetype(
           typeof(LocalToWorld), // Even if the group entity here has no renderer, it requires LocalToWorld
           typeof(Translation), // Transform position. The Lod-Distance is calculated against this.
                                //typeof(Rotation), // Transform rotation - doesn't seem to be required
                                //typeof(Scale), // Transform scale (version with X, Y and Z)  - doesn't seem to be required
           typeof(MeshLODGroupComponent) //MeshLODGroupComponent defines the distances and the local reference point.
       );

        // Create an entity which represents the LOD group
        Entity lodGroupEntity = entityManager.CreateEntity(lodGroupType);

        entityManager.SetComponentData(lodGroupEntity, new Translation()
        {
            Value = position
        });

        entityManager.SetComponentData(lodGroupEntity, new MeshLODGroupComponent()
        {
            LODDistances0 = lodDistances,
            LODDistances1 = lodDistances,       //XXX:What are LODDistances1 for? Addional Detail Levels?
            LocalReferencePoint = float3.zero   //XXX:Why is a reference point needed for distance calculation? Setting this to the position of the element gives strange results.
        });

        //
        // The creation of the simple LOD group Component is finished here. Next step is adding addional entities which could be rendered.
        //

        //Generate RenderBounds for LOD Entities
        RenderBounds bounds = new RenderBounds
        {
            Value = GetBoxRenderBounds(new float3(1, 1, 1))
        };

        float3 pos = position;
        Quaternion rot = Quaternion.identity;
        float scale = 1f;

        //Create the Archetype for the LOD Trees (The things actually rendered)
        var treeType = entityManager.CreateArchetype(
         typeof(RenderMesh), // Rendering mesh
         typeof(LocalToWorld), // Needed for rendering
         typeof(Translation), // Transform position
         typeof(Rotation), // Transform rotation
         typeof(Scale), // Transform scale (version with X, Y and Z)          
         typeof(RenderBounds), //Bounds to tell the Renderer where it is
         typeof(MeshLODComponent), //The actual LOD Component
         typeof(PerInstanceCullingTag) // Required for Occlusion Culling
     );
        //Create an Array of the LOD entities
        NativeArray<Entity> lods = new NativeArray<Entity>(4, Allocator.Temp);

        //Create all the LOD Entities in bulk
        entityManager.CreateEntity(treeType, lods);

        //Loop through each entity of a detail level
        for (int i = 0; i < 4; i++)
        {
            CreateLOD(lods[i], lodGroupEntity, lodElements[i].mesh, lodElements[i].mat, GetLODMask(i), pos, rot, scale, bounds, entityManager);
        }

        lods.Dispose();
    }

    //Create a manual render bound, for testing purposes.
    AABB GetBoxRenderBounds(float3 size)
    {
        var aabb = new AABB();
        aabb.Center = float3.zero;
        aabb.Extents = size;
        return aabb;
    }

    /// <summary>
    /// Sets up the various information for a single LOD entity.
    /// </summary>
    /// <param name="lodLevelEntity"> An rendered Entity for a specific detail level </param>
    /// <param name="lodGroupEntity"> The manager for the LOD group. A single entity with 'MeshLODGroupComponent' </param>
    /// <param name="mesh">The mesh for this detail level</param>
    /// <param name="mat">The material for this detail level</param>
    /// <param name="LODMask">The LODMask (does this define which view range applies in some kind of bitfield-manner?) </param>
    /// <param name="position">The mesh world position</param>
    /// <param name="rotation">The mesh rotation</param>
    /// <param name="size">The mesh scale</param>
    /// <param name="bounds">The render bounds of the mesh</param>
    /// <param name="manager">The entity manager to set component data</param>
    void CreateLOD(Entity lodLevelEntity, Entity lodGroupEntity, Mesh mesh, Material mat, int LODMask, float3 position, quaternion rotation, float size, RenderBounds bounds, EntityManager manager)
    {
        //Create Render Mesh fpr the Index
        RenderMesh renderMesh = new RenderMesh
        {
            mesh = mesh,
            material = mat,
            subMesh = 0,
            castShadows = ShadowCastingMode.Off,
            receiveShadows = false

        };
        //Set all Data
        manager.SetComponentData(lodLevelEntity, new Translation { Value = position });
        manager.SetComponentData(lodLevelEntity, new Scale { Value = size });
        manager.SetSharedComponentData(lodLevelEntity, renderMesh);
        manager.SetComponentData(lodLevelEntity, bounds);
        manager.SetComponentData(lodLevelEntity, new Rotation { Value = rotation });

        //Setup the LOD Component. Set the Group to be the Parent, and get a Mask for the LOD
        manager.SetComponentData(lodLevelEntity, new MeshLODComponent { Group = lodGroupEntity, LODMask = LODMask });
    }

    /// <summary>
    /// Get an appropriate mask for the LOD, Max number is 3 , returns mask as Hexidecimal
    /// </summary>
    /// <param name="index">LOD Index between 0 and 3 inclusive</param>
    /// <returns>Hexidecimal Mask</returns>
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
}