Shader "Jamie/Main/Floor Visibility"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _MemoryBrightness("Memory Brightness",Range(0, 0.2)) = 0.04
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0
        [ToggleUI] _JamieIsWall("Wall Surface", Float) = 0

        // Legacy properties. For fallback usage.
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color : COLOR;
                float2 worldPosition : TEXCOORD4;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"
            #include "MainLighting.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _JamieIsWall;
                float _MemoryBrightness;
            CBUFFER_END

            TEXTURE2D(_JamieExploredTexture);
            SAMPLER(sampler_JamieExploredTexture);

            TEXTURE2D(_JamieWallCaptureTexture);
            SAMPLER(sampler_JamieWallCaptureTexture);

            float _JamieWallMemoryBrightness;
            float _JamieWallMemoryWidth;
            float _JamieWallMemorySoftness;
            float _JamieWallMemoryWobble;
            float _JamieWallMemoryWobbleSpeed;
            float4 _JamieExplorationRect;
            float _JamieMemoryEnabled;
            float _JamieUseFadingMemory;
            float _JamieMemoryCapture;
            float _JamieMemorySoftness;
            float4 _JamieMemoryTexelSize;

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonLitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                o.worldPosition = TransformObjectToWorld(input.positionOS).xy;

                return o;
            }

            float ReadMemoryMask(float2 uv)
            {
                if (any(uv < 0.0) || any(uv > 1.0))
                    return 0.0;

                float4 history = SAMPLE_TEXTURE2D_LOD(
                    _JamieExploredTexture,
                    sampler_JamieExploredTexture,
                    uv,
                    0);

                return _JamieUseFadingMemory > 0.5
                    ? history.b
                    : history.r;
            }

            float ReadSoftMemoryMask(float2 uv)
            {
                if (_JamieMemorySoftness <= 0.001)
                    return ReadMemoryMask(uv);

                float2 offset =
                    _JamieMemoryTexelSize.xy * _JamieMemorySoftness;

                float total = 0.0;

                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float weight =
                            (x == 0 ? 2.0 : 1.0) *
                            (y == 0 ? 2.0 : 1.0);

                        total += ReadMemoryMask(
                            uv + float2(x, y) * offset) * weight;
                    }
                }

                return total / 16.0;
            }

            float ReadWallMask(float2 uv)
            {
                if (any(uv < 0.0) || any(uv > 1.0))
                    return 0.0;

                return SAMPLE_TEXTURE2D_LOD(
                    _JamieWallCaptureTexture,
                    sampler_JamieWallCaptureTexture,
                    uv,
                    0).g;
            }

            float ReadSoftWallMask(float2 uv)
            {
                if (_JamieWallMemorySoftness <= 0.001)
                    return ReadWallMask(uv);

                float2 offset = _JamieMemoryTexelSize.xy *
                    _JamieWallMemorySoftness * 0.5;

                float total =
                    ReadWallMask(uv + float2(-offset.x, -offset.y)) +
                    ReadWallMask(uv + float2( offset.x, -offset.y)) +
                    ReadWallMask(uv + float2(-offset.x,  offset.y)) +
                    ReadWallMask(uv + float2( offset.x,  offset.y));

                return total * 0.25;
            }

            float ReadWallEdge(float2 uv)
            {
                float2 offset = _JamieMemoryTexelSize.xy *
                    max(_JamieWallMemoryWidth, 0.5);

                float neighbour = min(
                    ReadSoftWallMask(uv + float2(offset.x, 0.0)),
                    ReadSoftWallMask(uv - float2(offset.x, 0.0)));

                neighbour = min(neighbour,
                    ReadSoftWallMask(uv + float2(0.0, offset.y)));

                neighbour = min(neighbour,
                    ReadSoftWallMask(uv - float2(0.0, offset.y)));

                return saturate(ReadSoftWallMask(uv) - neighbour);
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                half4 sprite =
                    SAMPLE_TEXTURE2D(
                        _MainTex,
                        sampler_MainTex,
                        input.uv) * input.color;

                half torchVisibility =
                    JamieMainTorchPresence(input.lightingUV) *
                    JamieMainSightPresence(input.lightingUV);

                // Capture flashlight exposure, excluding ambient and environmental lighting.
                if (_JamieMemoryCapture > 0.5)
                {
                    return half4(
                        torchVisibility,
                        0.0h,
                        0.0h,
                        sprite.a);
                }

                half3 lighting =
                    JamieMainAmbientLight(input.lightingUV) +
                    JamieMainPhysicalLight(input.lightingUV);

                half4 result = half4(
                    sprite.rgb * lighting,
                    sprite.a);

                if (_JamieMemoryEnabled > 0.5)
                {
                    float2 mapSize = max(
                        _JamieExplorationRect.zw,
                        float2(0.001, 0.001));

                    float2 memoryUV =
                        (input.worldPosition -
                        _JamieExplorationRect.xy) / mapSize;

                    half remembered =
                        ReadSoftMemoryMask(memoryUV);

                    result.rgb += sprite.rgb *
                        (_MemoryBrightness *
                        remembered *
                        (1.0h - torchVisibility));
                }

                return result;
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4   color           : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
                float _JamieIsWall;
                float _MemoryBrightness;
            CBUFFER_END

            Varyings NormalsRenderingVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonNormalsVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;

                return o;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                return CommonNormalsFragment(input, input.color);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
          
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _JamieIsWall;
                float _MemoryBrightness;
            CBUFFER_END

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color *_Color * unity_SpriteColor;
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                return CommonUnlitFragment(input, input.color);
            }
            ENDHLSL
        }
    }
}
