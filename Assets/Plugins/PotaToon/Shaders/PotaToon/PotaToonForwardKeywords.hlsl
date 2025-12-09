#ifndef POTA_TOON_FORWARD_KEYWORDS_INCLUDED
#define POTA_TOON_FORWARD_KEYWORDS_INCLUDED

#pragma target 4.5
// -------------------------------------
// Universal Render Pipeline keywords
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
#pragma multi_compile _ _SHADOWS_SOFT
#pragma multi_compile _ _FORWARD_PLUS
#pragma multi_compile _ _LIGHT_LAYERS
#pragma multi_compile_fragment _ _LIGHT_COOKIES
#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING

#pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE
// -------------------------------------
// Unity defined keywords
#pragma multi_compile _ DIRLIGHTMAP_COMBINED
#pragma multi_compile _ LIGHTMAP_ON
#pragma multi_compile _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
#pragma multi_compile_fog

// -------------------------------------
// Material keywords
#pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
#pragma shader_feature_local_fragment _ALPHATEST_ON
#pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF

// -------------------------------------
// POTA Toon keywords
#pragma multi_compile_fragment _ _POTA_TOON_OIT
#pragma multi_compile_fragment _ _POTA_TOON_SCREEN_RIM
#pragma multi_compile_local_fragment _ _USE_FACE_SDF
#pragma multi_compile_local_fragment _ _USE_2D_FACE_SHADOW
#pragma multi_compile_local_fragment _ _USE_GLITTER
#pragma shader_feature_fragment _ _DEBUG_POTA_TOON

// #pragma enable_d3d11_debug_symbols

#endif
