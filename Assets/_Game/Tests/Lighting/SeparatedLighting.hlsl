#ifndef JAMIE_SEPARATED_LIGHTING_INCLUDED
#define JAMIE_SEPARATED_LIGHTING_INCLUDED

// Test-only convention: 0 = room lights, 1 = sight mask, 2 = flashlight.
float4 _JamieTestSeenLightRange;

half3 JamieTestRoomLight(half2 uv)
{
#if USE_SHAPE_LIGHT_TYPE_0
    return SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, uv).rgb;
#else
    return half3(0, 0, 0);
#endif
}

half3 JamieTestTorchLight(half2 uv)
{
#if USE_SHAPE_LIGHT_TYPE_2
    return SAMPLE_TEXTURE2D(_ShapeLightTexture2, sampler_ShapeLightTexture2, uv).rgb;
#else
    return half3(0, 0, 0);
#endif
}

half3 JamieTestPhysicalLight(half2 uv)
{
    return max(half3(0, 0, 0),
        (JamieTestRoomLight(uv) + JamieTestTorchLight(uv)) * _HDREmulationScale);
}

half JamieTestReadableLight(half2 uv)
{
    half3 light = JamieTestPhysicalLight(uv);
    half brightness = max(light.r, max(light.g, light.b));
    return smoothstep(_JamieTestSeenLightRange.x,
        max(_JamieTestSeenLightRange.y, _JamieTestSeenLightRange.x + 0.001), brightness);
}

half JamieTestTorchHighlight(half2 uv)
{
    half3 light = JamieTestTorchLight(uv);
    return smoothstep(0.02h, 0.10h, max(light.r, max(light.g, light.b)));
}

half4 JamieTestLitFragment(Varyings input, half4 color)
{
    half4 main = color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
    return half4(main.rgb * JamieTestPhysicalLight(input.lightingUV), main.a);
}

#endif
