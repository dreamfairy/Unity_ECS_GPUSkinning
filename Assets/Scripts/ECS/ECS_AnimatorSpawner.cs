using Unity.Entities;

namespace Aoi.ECS
{
    public struct ECS_AnimatorSpawner : IComponentData
    {
        public int CountX;
        public int CountY;
        public int AttachCount;
        public Entity Prefab;

        public BlobAssetReference<EntityArray> AttachLOD;

        public int GetLODMask(int index)
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
}