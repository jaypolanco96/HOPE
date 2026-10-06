// Dedicated sprite shader. Character/cloth scene shaders remain unchanged.
Texture2D<float4> diffuse : register(t0);
SamplerState smp_clamp : register(s1);
float4 ps_main(
#ifdef HOPE_VULKAN
    [[vk::location(1)]]
#endif
    float2 uv : TEXCOORD1) : SV_Target {
  float4 sprite = diffuse.Sample(smp_clamp, uv);
#ifdef HDR
  // Same inverse tone curve as scene_common.hlsli PassGamma.
  float3 tm = sprite.rgb * sprite.rgb * (2.0 / (1.41 * 1.41));
  float3 lo = 1.0 - sqrt(saturate(1.0 - tm));
  float3 hi = 4.0 * tm - 3.0;
  sprite.rgb = lerp(lo, hi, step(1.0, tm));
#endif
  return sprite;
}
