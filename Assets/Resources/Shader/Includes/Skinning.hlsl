#ifndef __AOI_GPUSKINNING
#define __AOI_GPUSKINNING

TEXTURE2D(_AnimTex);
SAMPLER(sampler_AnimTex);

inline float2 BoneIndexToTexUV(float index, float4 param) {
	int row = (int)(index / param.y);
	int col = index % param.x;
	return float2(col * param.w, row * param.w);
}

inline float3 QuatMulPos(float4 rotation, float3 rhs)
{
	float3 qVec = half3(rotation.xyz);
	float3 c1 = cross(qVec, rhs);
	float3 c2 = cross(qVec, c1);

	return rhs + 2 * (c1 * rotation.w + c2);
}

inline float3 QuatMulPos(float4 real, float4 dual, float4 rhs) {
	return dual.xyz * rhs.w + QuatMulPos(real, rhs.xyz);
}

inline float4 DQTexSkinning(float4 vertex, float4 texcoord, float4 startData, Texture2D<float4> animTex, SamplerState animTexSample) {

	int index1 = startData.z + texcoord.x;
	float4 boneDataReal1 = SAMPLE_TEXTURE2D_LOD(animTex, animTexSample, BoneIndexToTexUV(index1, startData), 0);
	float4 boneDataDual1 = SAMPLE_TEXTURE2D_LOD(animTex, animTexSample, BoneIndexToTexUV(index1 + 1, startData), 0);
	float4 real1 = boneDataReal1.rgba;
	float4 dual1 = boneDataDual1.rgba;

	int index2 = startData.z + texcoord.z;
	float4 boneDataReal2 = SAMPLE_TEXTURE2D_LOD(animTex, animTexSample, BoneIndexToTexUV(index2, startData), 0);
	float4 boneDataDual2 = SAMPLE_TEXTURE2D_LOD(animTex, animTexSample, BoneIndexToTexUV(index2 + 1, startData), 0);
	float4 real2 = boneDataReal2.rgba;
	float4 dual2 = boneDataDual2.rgba;

	float3 position = (dual1.xyz * vertex.w) + QuatMulPos(real1, vertex.xyz);
	float4 t0 = float4(position, vertex.w);

	position = (dual2.xyz * vertex.w) + QuatMulPos(real2, vertex.xyz);
	float4 t1 = float4(position, vertex.w);

	return t0 * texcoord.y + t1 * texcoord.w;
}

inline void SkinningTex_float(float4 positionOS, float4 texcoord, float4 frameData, Texture2D<float4> animTex, SamplerState animTexSample, out float4 output) {
	output = float4(DQTexSkinning(positionOS, texcoord, frameData, animTex, animTexSample).xyz,1);
}

#endif