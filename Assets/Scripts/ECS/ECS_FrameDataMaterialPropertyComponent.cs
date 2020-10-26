using Unity.Rendering;
using Unity.Entities;
using Unity.Mathematics;

[MaterialProperty("_ECS_FrameData", MaterialPropertyFormat.Float4)]
public struct ECS_FrameDataMaterialPropertyComponent : IComponentData
{
    public float4 Value;
}