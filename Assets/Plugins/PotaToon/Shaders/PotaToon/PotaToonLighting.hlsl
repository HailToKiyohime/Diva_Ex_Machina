#ifndef POTA_TOON_LIGHTING_INCLUDED
#define POTA_TOON_LIGHTING_INCLUDED

#if _POTA_TOON_SCREEN_RIM
#include "../Common/PotaToonGlobalInput.hlsl"
#endif
#include "./PotaToonUtility.hlsl"

void HalfLambert(float3 lightDirection, float3 normalWS, float step, float smoothness, out float halfLambert, out float halfLambertStep)
{
    float M = step + smoothness;
    float m = step - smoothness;
    halfLambert = dot(lightDirection, normalWS) * 0.5 + 0.5;
    halfLambertStep = LinearStep(m, M, halfLambert);
}

half3 Specular(float3 lightDirection, half lightStrength, float3 normalWS, float3 viewDirection, float2 uv)
{
    half3 specularColor;
    float3 halfDirection = normalize(viewDirection + lightDirection);
    float NdotH = saturate(dot(halfDirection, normalWS));
    float smoothness = exp2(10 * _SpecularPower + 1);
    float modifier = PositivePow(NdotH, smoothness);
    modifier = LinearStep(0.5 - _SpecularSmoothness, 0.5 + _SpecularSmoothness, modifier);
    float4 SpecularMap = SAMPLE_TEXTURE2D_LOD(_SpecularMap, sampler_SpecularMap, uv, 0);
    float SpecularMask = SelectMask(SAMPLE_TEXTURE2D_LOD(_SpecularMask, sampler_SpecularMap, uv, 0), _SpecularMaskCH);

    specularColor = _SpecularColor.rgb * SpecularMap.rgb * (SpecularMask * lightStrength * _SpecularColor.a);
    specularColor *= modifier;
    
    return specularColor;
}

half3 MatCap(TEXTURE2D_PARAM(tex, smp), TEXTURE2D(mask), const half4 color, const float2 matcapUV, const float2 uv, const uint maskChannel)
{
    half3 matcapMap = SAMPLE_TEXTURE2D_LOD(tex, smp, matcapUV, 0).rgb;
    float matcapMask = SelectMask(SAMPLE_TEXTURE2D_LOD(mask, smp, uv, 0), maskChannel);
    return matcapMask > 0 ? matcapMap * color.rgb * color.a : -1;
}

half3 RimLighting(float3 normalWS, float3 viewDirection, half3 lighting, float facing, float2 uv, float charShadowAtten)
{
#if _USE_FACE_SDF
    return 0;
#else
    float emission = LinearStep(0.5 - _RimSmoothness, 0.5 + _RimSmoothness, pow(1 - saturate(dot(viewDirection, normalWS)), _RimPower * 8));
    half3 rimColor = _RimColor.rgb * (emission * _RimColor.a);
    float rimMask = SelectMask(SAMPLE_TEXTURE2D(_RimMask, sampler_MainTex, uv), _RimMaskCH);
    if (facing < 0.1 || charShadowAtten > 0)
    {
        rimColor = 0;
    }
    return rimColor * lighting * rimMask;
#endif
}

#if _POTA_TOON_SCREEN_RIM
void ScreenRimLighting(inout half3 color, float2 ssUV, float charShadowAtten)
{
    const float sceneDepth = SampleSceneDepth(ssUV);
    const float depthMultiplier = min(1, saturate((MaxScreenRimDist - LinearEyeDepth(sceneDepth, _ZBufferParams)) / MaxScreenRimDist));
    float3 lightDirCS = TransformWorldToHClipDir(_BrightestLightDirection.xyz);
    float sampleWidth = _ScreenRimWidth * _ScreenRimWidthMultiplier * depthMultiplier;
    
#if UNITY_UV_STARTS_AT_TOP
    lightDirCS.y = -lightDirCS.y;
#endif
    float2 rimUV = ssUV + lightDirCS.xy * sampleWidth;
    const float2 o = _ScreenSize.zw * 2;
    float maskSample = SAMPLE_TEXTURE2D_X(_PotaToonCharMask, sampler_MainTex, rimUV).r
                       + SAMPLE_TEXTURE2D_X(_PotaToonCharMask, sampler_MainTex, rimUV + float2(o.x, o.y)).r
                       + SAMPLE_TEXTURE2D_X(_PotaToonCharMask, sampler_MainTex, rimUV + float2(o.x, -o.y)).r
                       + SAMPLE_TEXTURE2D_X(_PotaToonCharMask, sampler_MainTex, rimUV + float2(-o.x, o.y)).r
                       + SAMPLE_TEXTURE2D_X(_PotaToonCharMask, sampler_MainTex, rimUV + float2(-o.x, -o.y)).r;

#if UNITY_REVERSED_Z
    bool isBehindChar = SampleSceneDepth(rimUV) < sceneDepth + 0.00001;
#else
    bool isBehindChar = SampleSceneDepth(rimUV) > sceneDepth + 0.00001;
#endif

    if (maskSample < HALF_MIN && isBehindChar)
    {
        half3 targetColor = lerp(_ScreenRimColor.rgb * _ScreenRimTint.rgb, _ScreenRimTint.rgb, _ScreenRimTintMode);
        half3 screenRimColor = targetColor * lerp(1.0, (1.0 - charShadowAtten), _ScreenRimShadowFade);
        color += screenRimColor;
    }
}
#endif

half3 Emission(float2 uv)
{
    half3 emissionMap = SAMPLE_TEXTURE2D(_EmissionMap, sampler_MainTex, uv).rgb;
    half emissionMask = SelectMask(SAMPLE_TEXTURE2D(_EmissionMask, sampler_MainTex, uv), _EmissionMaskCH);
    return _EmissionColor.rgb * emissionMap * (emissionMask * _EmissionColor.a);
}

half3 MainLighting(Light mainLight, float3 positionWS, float3 normalWS, float3 viewDirection, float2 ssUV, float2 uv, half opacity, inout float charShadowAtten, float faceSDFAtten, inout float totalAttenuation, inout half3 midTone, half aoMap)
{
    half3 currLighting = 0;
    half3 lightColor = mainLight.color * mainLight.distanceAttenuation;
    half lightStrength = 0.299 * lightColor.r + 0.587 * lightColor.g + 0.114 * lightColor.b;
    const bool isBrightestLight = _UseBrightestLight == 0 || _IsBrightestLightMain > 0;
    const half3 shadeColor = lerp(_ShadeColor.rgb, 0, _UseDarknessMode);

	// Ignore self-shadow as much as possible
	const bool needDefaultShadow = _ReceiveLightShadow > 0 && isBrightestLight;
	half mainLightShadowAtten = needDefaultShadow ? LinearStep(0, _StepSmoothness * 2, mainLight.shadowAttenuation) : 1;

    bool isMidToneArea = false;
    const half midToneAtten = aoMap * lightStrength * mainLightShadowAtten;

    // If Face type, adjust normal
    if (_ToonType == FACE_TYPE)
    {
        if (!isBrightestLight)
            normalWS = _FaceForward.xyz;
    }

    // Compute Char Shadow
    if (_DisableCharShadow == 0 && isBrightestLight)
    {
        charShadowAtten = GetCharMainShadow(ssUV, positionWS, opacity, faceSDFAtten);
        float smoothnessOffset = _CharShadowSmoothnessOffset * 0.3;
        float stepCharShadowAtten = LinearStep(0.3 - smoothnessOffset, 0.7 + smoothnessOffset, charShadowAtten);

        if (_UseMidTone > 0)
        {
            if (stepCharShadowAtten > 0)
            {
                float midToneStrength = 1.0 - LinearStep(0, _MidWidth, charShadowAtten);
                if (midToneStrength > 0)
                {
                    isMidToneArea = true;
                    midTone = _MidColor.rgb * midToneStrength * midToneAtten;
                }
            }
        }
        
        charShadowAtten = stepCharShadowAtten;
        totalAttenuation = min(totalAttenuation, 1.0 - charShadowAtten);
    }

    half3 lightDirection = isBrightestLight ? _BrightestLightDirection.xyz : mainLight.direction;
    // Diffuse
    float halfLambert, halfLambertStep;
    HalfLambert(lightDirection, normalWS, _BaseStep, _StepSmoothness, halfLambert, halfLambertStep);
    
    // MidTone
    if (_UseMidTone > 0)
    {
#if _USE_FACE_SDF
        // Apply mid tone attenuation for face sdf mid tone
        midTone *= midToneAtten;
#endif
        
		// If no character shadow
        if (charShadowAtten < HALF_MIN)
        {
            isMidToneArea = true;
            float midToneSmoothness = _StepSmoothness * _MidWidth;
            midTone += GetMidTone(halfLambert, _BaseStep, midToneSmoothness) * midToneAtten;
            halfLambertStep = LinearStep(_BaseStep - midToneSmoothness, _BaseStep + midToneSmoothness, halfLambert);
        }

        if (needDefaultShadow)
        {
            float midToneSmoothness = _MidWidth * 0.5;
            midTone += GetMidTone(mainLightShadowAtten, 0.5, midToneSmoothness) * (aoMap * lightStrength);
    		mainLightShadowAtten = LinearStep(0.5 - midToneSmoothness, 0.5 + midToneSmoothness, mainLightShadowAtten);
        }
        
        if (halfLambertStep < HALF_MIN)
            isMidToneArea = false;
        
        if (isMidToneArea == false)
            midTone = 0;
    }

    if (isBrightestLight)
    {
        totalAttenuation = min(totalAttenuation, halfLambertStep);
    }
    
    // Always apply step if main light
    currLighting = lerp(shadeColor, _BaseColor.rgb, halfLambertStep);

    // Specular
    currLighting += Specular(lightDirection, lightStrength, normalWS, viewDirection, uv);

#if _USE_FACE_SDF
    // Override to FaceSDF if enabled
    if (isBrightestLight)
        currLighting = lerp(shadeColor, _BaseColor.rgb, faceSDFAtten);
#endif
    
    // Apply Character Shadow
    if (_DisableCharShadow == 0 && isBrightestLight)
    {
        currLighting = lerp(currLighting, shadeColor, charShadowAtten);
    }

    // Apply main light shadow
    if (needDefaultShadow)
    {
        currLighting = lerp(shadeColor, currLighting, mainLightShadowAtten);
        totalAttenuation = min(totalAttenuation, mainLightShadowAtten);
    }

    return currLighting * lightColor;
}

half3 AdditionalLighting(Light light, float3 normalWS, float3 viewDirection, float2 ssUV, float2 uv, float3 positionWS, uint lightIndex, half opacity, inout float charShadowAtten, float faceSDFAtten, inout float totalAttenuation, inout half3 midTone)
{
    half3 currLighting = 0;
    half3 lightColor = light.color * light.distanceAttenuation;
    half lightStrength = 0.299 * lightColor.r + 0.587 * lightColor.g + 0.114 * lightColor.b;
    const bool isBrightestLight = _UseBrightestLight > 0 && _IsBrightestLightMain == 0 && _BrightestLightIndex == lightIndex;
    const half3 shadeColor = lerp(_ShadeColor.rgb, 0, _UseDarknessMode);
    
    // If Face type, adjust normal
    if (_ToonType == FACE_TYPE)
    {
        if (!isBrightestLight)
            normalWS = _FaceForward.xyz;
    }

    // Compute Char Shadow
    float stepLocalCharShadowAtten = 0;
    if (_DisableCharShadow == 0 && isBrightestLight)
    {
        float localCharShadowAtten = GetCharAdditionalShadow(ssUV, positionWS, opacity, lightIndex, faceSDFAtten, 0);
        float smoothnessOffset = _CharShadowSmoothnessOffset * 0.3;
        stepLocalCharShadowAtten = LinearStep(0.3 - smoothnessOffset, 0.7 + smoothnessOffset, localCharShadowAtten);
        charShadowAtten = stepLocalCharShadowAtten;
        totalAttenuation = min(totalAttenuation, 1.0 - charShadowAtten);
    }

    // Diffuse
    float halfLambert, halfLambertStep;
    HalfLambert(light.direction, normalWS, _BaseStep, _StepSmoothness * 2, halfLambert, halfLambertStep); // Assume _StepSmoothness = [0, 0.1]
    if (isBrightestLight)
    {
        totalAttenuation = min(totalAttenuation, halfLambertStep);
    }
    currLighting = lerp(shadeColor, _BaseColor.rgb, halfLambertStep);

    // Reduce MidTone intensity if lit by additional light
    if (halfLambertStep < HALF_MIN || halfLambertStep > (1.0 - HALF_MIN))
    {
        midTone *= 1.0 - saturate(lightStrength);
    }

    // Specular
    currLighting += Specular(light.direction, lightStrength, normalWS, viewDirection, uv);

#if _USE_FACE_SDF
    // Override to FaceSDF if enabled
    if (isBrightestLight)
        currLighting = lerp(shadeColor, _BaseColor.rgb, faceSDFAtten);
#endif

    // Apply Character Shadow
    if (_DisableCharShadow == 0 && isBrightestLight)
    {
        currLighting = lerp(currLighting, shadeColor, stepLocalCharShadowAtten);
    }

    if (_ReceiveLightShadow > 0 && isBrightestLight)
    {
		half lightShadowAtten = LinearStep(0, _StepSmoothness * 2, light.shadowAttenuation);
        currLighting = lerp(shadeColor, currLighting, lightShadowAtten);
        totalAttenuation = min(totalAttenuation, lightShadowAtten);
    }

    return currLighting * lightColor;
}

#endif