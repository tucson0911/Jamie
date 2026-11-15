#ifndef JAMIE_MAIN_LIGHTING_INCLUDED
#define JAMIE_MAIN_LIGHTING_INCLUDED

half3 JamieMainPhysicalLight(half2 uv)
{
    half3 light = half3(0, 0, 0);

#if USE_SHAPE_LIGHT_TYPE_0
    light += SAMPLE_TEXTURE2D(
        _ShapeLightTexture0,
        sampler_ShapeLightTexture0,
        uv).rgb;
#endif

#if USE_SHAPE_LIGHT_TYPE_2
    light += SAMPLE_TEXTURE2D(
        _ShapeLightTexture2,
        sampler_ShapeLightTexture2,
        uv).rgb;
#endif

    return max(
        light * _HDREmulationScale,
        half3(0, 0, 0));
}

half JamieMainLightPresence(half2 uv)
{
    half3 light = JamieMainPhysicalLight(uv);
    half brightness = max(light.r, max(light.g, light.b));

    return smoothstep(0.001h, 0.01h, brightness);
}

half4 JamieMainLitFragment(Varyings input, half4 color)
{
    half4 sprite =
        SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv)
        * color;

    return half4(
        sprite.rgb * JamieMainPhysicalLight(input.lightingUV),
        sprite.a);
}
half JamieMainTorchPresence(half2 uv)
{
    half3 light = half3(0, 0, 0);

#if USE_SHAPE_LIGHT_TYPE_2
    light = SAMPLE_TEXTURE2D(
        _ShapeLightTexture2,
        sampler_ShapeLightTexture2,
        uv).rgb * _HDREmulationScale;
#endif

    half brightness = max(light.r, max(light.g, light.b));

    return smoothstep(0.001h, 0.01h, brightness);
}
half JamieMainSightPresence(half2 uv)
{
    half3 sight = half3(0, 0, 0);

#if USE_SHAPE_LIGHT_TYPE_1
    sight = SAMPLE_TEXTURE2D(
        _ShapeLightTexture1,
        sampler_ShapeLightTexture1,
        uv).rgb;
#endif

    half brightness =
        max(sight.r, max(sight.g, sight.b));

    return smoothstep(0.02h, 0.10h, brightness);
}

half3 JamieMainAmbientLight(half2 uv)
{
#if USE_SHAPE_LIGHT_TYPE_3
    return SAMPLE_TEXTURE2D(
        _ShapeLightTexture3,
        sampler_ShapeLightTexture3,
        uv).rgb * _HDREmulationScale;
#else
    return half3(0, 0, 0);
#endif
}
#endif