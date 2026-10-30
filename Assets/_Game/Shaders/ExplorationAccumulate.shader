Shader "Jamie/Exploration Accumulate"
{
    Properties
    {
        _CurrentVisibility("Current Visibility", 2D) = "black" {}
        _PreviousMemory("Previous Memory", 2D) = "black" {}
        [HideInInspector] _MemoryTime("Memory Time", Float) = 0
        [HideInInspector] _MemoryHoldTime("Memory Hold Time", Float) = 2
        [HideInInspector] _MemoryFadeTime("Memory Fade Time", Float) = 3
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            ZWrite Off
            ZTest Always
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_CurrentVisibility);
            SAMPLER(sampler_CurrentVisibility);

            TEXTURE2D(_PreviousMemory);
            SAMPLER(sampler_PreviousMemory);

            CBUFFER_START(UnityPerMaterial)
                float _MemoryTime;
                float _MemoryHoldTime;
                float _MemoryFadeTime;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                output.positionCS =
                    GetFullScreenTriangleVertexPosition(vertexID);
                output.uv =
                    GetFullScreenTriangleTexCoord(vertexID);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float current = SAMPLE_TEXTURE2D(
                    _CurrentVisibility,
                    sampler_CurrentVisibility,
                    input.uv).r;

                float4 previous = SAMPLE_TEXTURE2D(
                    _PreviousMemory,
                    sampler_PreviousMemory,
                    input.uv);

                float lastSeen = previous.g;
                float peak = previous.r > 0.0 ? previous.a : 0.0;

                if (current > 0.001)
                {
                    lastSeen = _MemoryTime;
                    peak = max(current, previous.b);
                }

                float age = max(0.0, _MemoryTime - lastSeen);
                float fadeDuration = max(_MemoryFadeTime, 0.001);

                float fade = 1.0 - smoothstep(
                    _MemoryHoldTime,
                    _MemoryHoldTime + fadeDuration,
                    age);

                float fadingMemory = peak * fade;
                float permanentMemory = max(current, previous.r);

                if (fade <= 0.0)
                    peak = 0.0;

                return float4(
                    permanentMemory,
                    lastSeen,
                    fadingMemory,
                    peak);
            }
            ENDHLSL
        }
    }
}