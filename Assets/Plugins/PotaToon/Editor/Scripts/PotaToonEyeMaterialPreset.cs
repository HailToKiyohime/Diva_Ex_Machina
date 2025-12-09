using UnityEngine;
using UnityEngine.Rendering;
using PotaToon;

namespace PotaToon.Editor
{
    [CreateAssetMenu(menuName = "PotaToon/Eye Material Preset", fileName = "PotaToonEyeMaterialPreset")]
    internal class PotaToonEyeMaterialPreset : PotaToonMaterialPresetBase
    {
        // Base Settings
        public CullMode _CullMode           = CullMode.Back;

        // Stencil
        public CompareFunction _StencilComp;
        public float _StencilRef;
        public StencilOp _StencilPass;
        public StencilOp _StencilFail;
        public StencilOp _StencilZFail;

        // Settings
        public Color _BaseColor             = Color.white;
        public float _BaseStep              = 0.5f;
        public float _StepSmoothness        = 0.01f;
        public float _Exposure              = 1f;
        public float _IndirectDimmer        = 0f;
        public int _UseRefraction           = 1;
        public float _RefractionWeight      = 0f;
        public float _MinIntensity          = 0.1f;
        public int _UseHiLight              = 0;
        public int _UseHiLightJitter        = 0;
        public Color _HiLightColor          = Color.white;
        public float _HiLightPowerR         = 1f;
        public float _HiLightPowerG         = 1f;
        public float _HiLightPowerB         = 1f;
        public float _HiLightIntensityR     = 1f;
        public float _HiLightIntensityG     = 1f;
        public float _HiLightIntensityB     = 1f;
        public MaskChannel _ClippingMaskCH  = MaskChannel.G;

        /// <summary>
        /// Sets texture only if provided (non-null).
        /// </summary>
        private void SetMaterialTextureIfNeeded(Material mat, string property, Texture tex)
        {
            if (tex != null)
                mat.SetTexture(property, tex);
        }

        /// <summary>
        /// Apply this preset to the given material.
        /// </summary>
        public override void ApplyTo(Material mat)
        {
            // Base Settings
            mat.SetInt("_ToonType", (int)_ToonType);
            mat.SetInt("_CullMode", (int)_CullMode);

            // Stencil
            mat.SetInt("_StencilComp", (int)_StencilComp);
            mat.SetFloat("_StencilRef", _StencilRef);
            mat.SetInt("_StencilPass", (int)_StencilPass);
            mat.SetInt("_StencilFail", (int)_StencilFail);
            mat.SetInt("_StencilZFail", (int)_StencilZFail);

            // Settings
            mat.SetColor("_BaseColor", _BaseColor);
            mat.SetFloat("_BaseStep", _BaseStep);
            mat.SetFloat("_StepSmoothness", _StepSmoothness);
            mat.SetFloat("_Exposure", _Exposure);
            mat.SetFloat("_IndirectDimmer", _IndirectDimmer);
            mat.SetInt("_UseRefraction", _UseRefraction);
            mat.SetFloat("_RefractionWeight", _RefractionWeight);
            mat.SetFloat("_MinIntensity", _MinIntensity);
            mat.SetInt("_UseHiLight", _UseHiLight);
            mat.SetInt("_UseHiLightJitter", _UseHiLightJitter);
            mat.SetColor("_HiLightColor", _HiLightColor);
            mat.SetFloat("_HiLightPowerR", _HiLightPowerR);
            mat.SetFloat("_HiLightPowerG", _HiLightPowerG);
            mat.SetFloat("_HiLightPowerB", _HiLightPowerB);
            mat.SetFloat("_HiLightIntensityR", _HiLightIntensityR);
            mat.SetFloat("_HiLightIntensityG", _HiLightIntensityG);
            mat.SetFloat("_HiLightIntensityB", _HiLightIntensityB);
            mat.SetInt("_ClippingMaskCH", (int)_ClippingMaskCH);
        }

        /// <summary>
        /// Save material state into this preset.
        /// </summary>
        public override void SaveFrom(Material mat)
        {
            // Base Settings
            _ToonType           = (ToonType) mat.GetInt("_ToonType");
            _CullMode           = (CullMode) mat.GetInt("_CullMode");

            // Stencil
            _StencilComp        = (CompareFunction) mat.GetInt("_StencilComp");
            _StencilRef         = mat.GetFloat("_StencilRef");
            _StencilPass        = (StencilOp) mat.GetInt("_StencilPass");
            _StencilFail        = (StencilOp) mat.GetInt("_StencilFail");
            _StencilZFail       = (StencilOp) mat.GetInt("_StencilZFail");

            // Settings
            _BaseColor          = mat.GetColor("_BaseColor");
            _BaseStep           = mat.GetFloat("_BaseStep");
            _StepSmoothness     = mat.GetFloat("_StepSmoothness");
            _Exposure           = mat.GetFloat("_Exposure");
            _IndirectDimmer     = mat.GetFloat("_IndirectDimmer");
            _UseRefraction      = mat.GetInt("_UseRefraction");
            _RefractionWeight   = mat.GetFloat("_RefractionWeight");
            _MinIntensity       = mat.GetFloat("_MinIntensity");
            _UseHiLight         = mat.GetInt("_UseHiLight");
            _UseHiLightJitter   = mat.GetInt("_UseHiLightJitter");
            _HiLightColor       = mat.GetColor("_HiLightColor");
            _HiLightPowerR      = mat.GetFloat("_HiLightPowerR");
            _HiLightPowerG      = mat.GetFloat("_HiLightPowerG");
            _HiLightPowerB      = mat.GetFloat("_HiLightPowerB");
            _HiLightIntensityR  = mat.GetFloat("_HiLightIntensityR");
            _HiLightIntensityG  = mat.GetFloat("_HiLightIntensityG");
            _HiLightIntensityB  = mat.GetFloat("_HiLightIntensityB");
            _ClippingMaskCH     = (MaskChannel) mat.GetInt("_ClippingMaskCH");
        }
    }
}
