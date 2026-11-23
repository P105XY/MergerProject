Shader "Merger/2D/Special Effect Visual"
{
    Properties
    {
        _Tint ("Tint", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex EffectVertex
            #pragma fragment EffectFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
            CBUFFER_END

            // 효과 메시의 꼭짓점을 화면에 투영한다.
            float4 EffectVertex(float3 position : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(position);
            }

            // 조명 없이 지정한 효과 색과 투명도를 출력한다.
            half4 EffectFragment() : SV_Target
            {
                return _Tint;
            }
            ENDHLSL
        }
    }
}
