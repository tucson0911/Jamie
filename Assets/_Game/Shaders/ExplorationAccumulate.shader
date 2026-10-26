Shader "Jamie/Exploration Accumulate"
{
    Properties
    {
        _CurrentVisibility("Current Visibility", 2D) = "black" {}
        _PreviousMemory("Previous Memory", 2D) = "black" {}
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

            half4 Frag(Varyings input) : SV_Target
            {
                half current = SAMPLE_TEXTURE2D(
                    _CurrentVisibility,
                    sampler_CurrentVisibility,
                    input.uv).r;

                half previous = SAMPLE_TEXTURE2D(
                    _PreviousMemory,
                    sampler_PreviousMemory,
                    input.uv).r;

                half explored = max(current, previous);
                return half4(explored, explored, explored, 1.0h);
            }
            ENDHLSL
        }
    }
}