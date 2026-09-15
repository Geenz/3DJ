using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace com.happyrobot33.holographicreprojector {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SurfelPassViewController : UdonSharpBehaviour {
        public Material fitMaterial;
        public Material splatMaterial;
        public Material decodeMaterial;

        [HideInInspector] public Shader accumulateShader;

        Shader decodeShader;

        public Toggle blueNoiseJitter;
        public Toggle rectangular;

        public TMP_Dropdown blendMode;

        public Slider sobelThreshold;
        public Slider reprojectTolerance;
        public Slider reprojectViews;
        public Slider radiusMin;
        public Slider radiusMax;
        public Slider detailScale;
        public Slider seamBlend;
        public Slider grazingCutoff;
        public Slider maxStretch;
        public Slider hullThreshold;
        public Slider hullFeather;
        public Slider radius;
        public Slider stride;
        public Slider depthOffset;
        public Slider cutoff;
        public Slider falloff;
        public Slider falloffMult;
        public Slider fadeDistance;
        public Slider fadeMultiplier;
        public Slider lodDistance;
        public Slider accScale;
        public Slider blueNoiseMix;
        public Slider dotScale;
        public Slider dotSizeVariability;
        public Slider dotContrast;
        public Slider stretchSmoothness;
        public Slider exposure;
        public Slider offset;

        public TMP_Text[] sliderLabels;

        [UdonSynced] bool syncedBlueNoiseJitter;
        [UdonSynced] bool syncedRectangular;

        [UdonSynced] int syncedBlendMode;

        [UdonSynced] float syncedSobelThreshold;
        [UdonSynced] float syncedReprojectTolerance;
        [UdonSynced] int syncedReprojectViews;
        [UdonSynced] float syncedRadiusMin;
        [UdonSynced] float syncedRadiusMax;
        [UdonSynced] float syncedDetailScale;
        [UdonSynced] float syncedSeamBlend;
        [UdonSynced] float syncedGrazingCutoff;
        [UdonSynced] float syncedMaxStretch;
        [UdonSynced] float syncedHullThreshold;
        [UdonSynced] float syncedHullFeather;
        [UdonSynced] float syncedRadius;
        [UdonSynced] float syncedStride;
        [UdonSynced] float syncedDepthOffset;
        [UdonSynced] float syncedCutoff;
        [UdonSynced] float syncedFalloff;
        [UdonSynced] float syncedFalloffMult;
        [UdonSynced] float syncedFadeDistance;
        [UdonSynced] float syncedFadeMultiplier;
        [UdonSynced] float syncedLodDistance;
        [UdonSynced] float syncedAccScale;
        [UdonSynced] float syncedBlueNoiseMix;
        [UdonSynced] float syncedDotScale;
        [UdonSynced] float syncedDotSizeVariability;
        [UdonSynced] float syncedDotContrast;
        [UdonSynced] float syncedStretchSmoothness;
        [UdonSynced] float syncedExposure;
        [UdonSynced] float syncedOffset;

        Toggle[] toggles;
        TMP_Dropdown[] drops;
        Slider[] sliders;

        void BuildArrays() {
            if (toggles != null) {
                return;
            }

            toggles = new Toggle[2];
            toggles[0] = blueNoiseJitter;
            toggles[1] = rectangular;

            drops = new TMP_Dropdown[1];
            drops[0] = blendMode;

            sliders = new Slider[28];
            sliders[0] = sobelThreshold;
            sliders[1] = reprojectTolerance;
            sliders[2] = reprojectViews;
            sliders[3] = radiusMin;
            sliders[4] = radiusMax;
            sliders[5] = detailScale;
            sliders[6] = seamBlend;
            sliders[7] = grazingCutoff;
            sliders[8] = maxStretch;
            sliders[9] = hullThreshold;
            sliders[10] = hullFeather;
            sliders[11] = radius;
            sliders[12] = stride;
            sliders[13] = depthOffset;
            sliders[14] = cutoff;
            sliders[15] = falloff;
            sliders[16] = falloffMult;
            sliders[17] = fadeDistance;
            sliders[18] = fadeMultiplier;
            sliders[19] = lodDistance;
            sliders[20] = accScale;
            sliders[21] = blueNoiseMix;
            sliders[22] = dotScale;
            sliders[23] = dotSizeVariability;
            sliders[24] = dotContrast;
            sliders[25] = stretchSmoothness;
            sliders[26] = exposure;
            sliders[27] = offset;
        }

        void Start() {
            if (fitMaterial == null || splatMaterial == null || decodeMaterial == null) {
                return;
            }

            decodeShader = decodeMaterial.shader;

            BuildArrays();

            bool blueNoiseJitterInitial = decodeMaterial.GetFloat("_BlueNoiseJitter") > 0.5f;
            PushToggle(0, blueNoiseJitterInitial);
            SetSyncedToggle(0, blueNoiseJitterInitial);
            bool rectangularInitial = decodeMaterial.GetFloat("_Shape") > 0.5f;
            PushToggle(1, rectangularInitial);
            SetSyncedToggle(1, rectangularInitial);

            int blendModeInitial = Mathf.RoundToInt(decodeMaterial.GetFloat("_Mode"));
            PushDropdown(0, blendModeInitial);
            syncedBlendMode = blendModeInitial;

            float sobelThresholdInitial = fitMaterial.GetFloat("_SobelThreshold");
            PushSlider(0, sobelThresholdInitial);
            SetSyncedSlider(0, sobelThresholdInitial);
            float reprojectToleranceInitial = fitMaterial.GetFloat("_ReprojectTolerance");
            PushSlider(1, reprojectToleranceInitial);
            SetSyncedSlider(1, reprojectToleranceInitial);
            float reprojectViewsInitial = fitMaterial.GetFloat("_ReprojectViews");
            PushSlider(2, reprojectViewsInitial);
            SetSyncedSlider(2, reprojectViewsInitial);
            float radiusMinInitial = fitMaterial.GetFloat("_RadiusMin");
            PushSlider(3, radiusMinInitial);
            SetSyncedSlider(3, radiusMinInitial);
            float radiusMaxInitial = fitMaterial.GetFloat("_RadiusMax");
            PushSlider(4, radiusMaxInitial);
            SetSyncedSlider(4, radiusMaxInitial);
            float detailScaleInitial = fitMaterial.GetFloat("_DetailScale");
            PushSlider(5, detailScaleInitial);
            SetSyncedSlider(5, detailScaleInitial);
            float seamBlendInitial = splatMaterial.GetFloat("_SeamBlend");
            PushSlider(6, seamBlendInitial);
            SetSyncedSlider(6, seamBlendInitial);
            float grazingCutoffInitial = splatMaterial.GetFloat("_GrazingCutoff");
            PushSlider(7, grazingCutoffInitial);
            SetSyncedSlider(7, grazingCutoffInitial);
            float maxStretchInitial = splatMaterial.GetFloat("_MaxStretch");
            PushSlider(8, maxStretchInitial);
            SetSyncedSlider(8, maxStretchInitial);
            float hullThresholdInitial = splatMaterial.GetFloat("_HullThreshold");
            PushSlider(9, hullThresholdInitial);
            SetSyncedSlider(9, hullThresholdInitial);
            float hullFeatherInitial = splatMaterial.GetFloat("_HullFeather");
            PushSlider(10, hullFeatherInitial);
            SetSyncedSlider(10, hullFeatherInitial);
            float radiusInitial = decodeMaterial.GetFloat("_Radius");
            PushSlider(11, radiusInitial);
            SetSyncedSlider(11, radiusInitial);
            float strideInitial = decodeMaterial.GetFloat("_Stride");
            PushSlider(12, strideInitial);
            SetSyncedSlider(12, strideInitial);
            float depthOffsetInitial = decodeMaterial.GetFloat("_DepthOffset");
            PushSlider(13, depthOffsetInitial);
            SetSyncedSlider(13, depthOffsetInitial);
            float cutoffInitial = decodeMaterial.GetFloat("_Cutoff");
            PushSlider(14, cutoffInitial);
            SetSyncedSlider(14, cutoffInitial);
            float falloffInitial = decodeMaterial.GetFloat("_Falloff");
            PushSlider(15, falloffInitial);
            SetSyncedSlider(15, falloffInitial);
            float falloffMultInitial = decodeMaterial.GetFloat("_FalloffMult");
            PushSlider(16, falloffMultInitial);
            SetSyncedSlider(16, falloffMultInitial);
            float fadeDistanceInitial = decodeMaterial.GetFloat("_FadeDistance");
            PushSlider(17, fadeDistanceInitial);
            SetSyncedSlider(17, fadeDistanceInitial);
            float fadeMultiplierInitial = decodeMaterial.GetFloat("_FadeMultiplier");
            PushSlider(18, fadeMultiplierInitial);
            SetSyncedSlider(18, fadeMultiplierInitial);
            float lodDistanceInitial = decodeMaterial.GetFloat("_LodDistance");
            PushSlider(19, lodDistanceInitial);
            SetSyncedSlider(19, lodDistanceInitial);
            float accScaleInitial = decodeMaterial.GetFloat("_AccScale");
            PushSlider(20, accScaleInitial);
            SetSyncedSlider(20, accScaleInitial);
            float blueNoiseMixInitial = decodeMaterial.GetFloat("_BlueNoiseMix");
            PushSlider(21, blueNoiseMixInitial);
            SetSyncedSlider(21, blueNoiseMixInitial);
            float dotScaleInitial = decodeMaterial.GetFloat("_Scale");
            PushSlider(22, dotScaleInitial);
            SetSyncedSlider(22, dotScaleInitial);
            float dotSizeVariabilityInitial = decodeMaterial.GetFloat("_SizeVariability");
            PushSlider(23, dotSizeVariabilityInitial);
            SetSyncedSlider(23, dotSizeVariabilityInitial);
            float dotContrastInitial = decodeMaterial.GetFloat("_Contrast");
            PushSlider(24, dotContrastInitial);
            SetSyncedSlider(24, dotContrastInitial);
            float stretchSmoothnessInitial = decodeMaterial.GetFloat("_StretchSmoothness");
            PushSlider(25, stretchSmoothnessInitial);
            SetSyncedSlider(25, stretchSmoothnessInitial);
            float exposureInitial = decodeMaterial.GetFloat("_InputExposure");
            PushSlider(26, exposureInitial);
            SetSyncedSlider(26, exposureInitial);
            float offsetInitial = decodeMaterial.GetFloat("_InputOffset");
            PushSlider(27, offsetInitial);
            SetSyncedSlider(27, offsetInitial);

            if (Networking.IsOwner(gameObject)) {
                RequestSerialization();
            }

            for (int i = 0; i < sliders.Length; i++) {
                WriteLabel(i);
            }

            ShowRows(blendModeInitial);
        }

        public void OnControlChanged() {
            if (fitMaterial == null || splatMaterial == null || decodeMaterial == null || toggles == null) {
                return;
            }

            bool changed = false;

            for (int i = 0; i < toggles.Length; i++) {
                Toggle toggle = toggles[i];

                if (toggle == null) {
                    continue;
                }

                bool value = toggle.isOn;

                if (value == GetSyncedToggle(i)) {
                    continue;
                }

                if (!Networking.IsOwner(gameObject)) {
                    Networking.SetOwner(Networking.LocalPlayer, gameObject);
                }

                SetSyncedToggle(i, value);
                changed = true;
                ApplyToggle(i, value);
            }

            for (int i = 0; i < drops.Length; i++) {
                TMP_Dropdown dropdown = drops[i];

                if (dropdown == null) {
                    continue;
                }

                int value = dropdown.value;

                if (value == syncedBlendMode) {
                    continue;
                }

                if (!Networking.IsOwner(gameObject)) {
                    Networking.SetOwner(Networking.LocalPlayer, gameObject);
                }

                syncedBlendMode = value;
                changed = true;
                ApplyDropdown(i, value);
            }

            for (int i = 0; i < sliders.Length; i++) {
                Slider slider = sliders[i];

                if (slider == null) {
                    continue;
                }

                float value = slider.value;

                if (value == GetSyncedSlider(i)) {
                    continue;
                }

                if (!Networking.IsOwner(gameObject)) {
                    Networking.SetOwner(Networking.LocalPlayer, gameObject);
                }

                SetSyncedSlider(i, value);
                changed = true;
                WriteLabel(i);
                ApplySlider(i, value);
            }

            if (changed) {
                RequestSerialization();
            }
        }

        public override void OnDeserialization() {
            if (fitMaterial == null || splatMaterial == null || decodeMaterial == null) {
                return;
            }

            BuildArrays();

            for (int i = 0; i < toggles.Length; i++) {
                if (toggles[i] == null) {
                    continue;
                }

                bool value = GetSyncedToggle(i);
                ApplyToggle(i, value);
                PushToggle(i, value);
            }

            for (int i = 0; i < drops.Length; i++) {
                if (drops[i] == null) {
                    continue;
                }

                ApplyDropdown(i, syncedBlendMode);
                PushDropdown(i, syncedBlendMode);
            }

            for (int i = 0; i < sliders.Length; i++) {
                if (sliders[i] == null) {
                    continue;
                }

                float value = GetSyncedSlider(i);
                ApplySlider(i, value);
                PushSlider(i, value);
                WriteLabel(i);
            }
        }

        bool GetSyncedToggle(int index) {
            if (index == 0) {
                return syncedBlueNoiseJitter;
            }

            return syncedRectangular;
        }

        void SetSyncedToggle(int index, bool value) {
            if (index == 0) {
                syncedBlueNoiseJitter = value;
            } else {
                syncedRectangular = value;
            }
        }

        float GetSyncedSlider(int index) {
            if (index == 0) {
                return syncedSobelThreshold;
            }

            if (index == 1) {
                return syncedReprojectTolerance;
            }

            if (index == 2) {
                return (float)syncedReprojectViews;
            }

            if (index == 3) {
                return syncedRadiusMin;
            }

            if (index == 4) {
                return syncedRadiusMax;
            }

            if (index == 5) {
                return syncedDetailScale;
            }

            if (index == 6) {
                return syncedSeamBlend;
            }

            if (index == 7) {
                return syncedGrazingCutoff;
            }

            if (index == 8) {
                return syncedMaxStretch;
            }

            if (index == 9) {
                return syncedHullThreshold;
            }

            if (index == 10) {
                return syncedHullFeather;
            }

            if (index == 11) {
                return syncedRadius;
            }

            if (index == 12) {
                return syncedStride;
            }

            if (index == 13) {
                return syncedDepthOffset;
            }

            if (index == 14) {
                return syncedCutoff;
            }

            if (index == 15) {
                return syncedFalloff;
            }

            if (index == 16) {
                return syncedFalloffMult;
            }

            if (index == 17) {
                return syncedFadeDistance;
            }

            if (index == 18) {
                return syncedFadeMultiplier;
            }

            if (index == 19) {
                return syncedLodDistance;
            }

            if (index == 20) {
                return syncedAccScale;
            }

            if (index == 21) {
                return syncedBlueNoiseMix;
            }

            if (index == 22) {
                return syncedDotScale;
            }

            if (index == 23) {
                return syncedDotSizeVariability;
            }

            if (index == 24) {
                return syncedDotContrast;
            }

            if (index == 25) {
                return syncedStretchSmoothness;
            }

            if (index == 26) {
                return syncedExposure;
            }

            return syncedOffset;
        }

        void SetSyncedSlider(int index, float value) {
            if (index == 0) {
                syncedSobelThreshold = value;
            } else if (index == 1) {
                syncedReprojectTolerance = value;
            } else if (index == 2) {
                syncedReprojectViews = (int)value;
            } else if (index == 3) {
                syncedRadiusMin = value;
            } else if (index == 4) {
                syncedRadiusMax = value;
            } else if (index == 5) {
                syncedDetailScale = value;
            } else if (index == 6) {
                syncedSeamBlend = value;
            } else if (index == 7) {
                syncedGrazingCutoff = value;
            } else if (index == 8) {
                syncedMaxStretch = value;
            } else if (index == 9) {
                syncedHullThreshold = value;
            } else if (index == 10) {
                syncedHullFeather = value;
            } else if (index == 11) {
                syncedRadius = value;
            } else if (index == 12) {
                syncedStride = value;
            } else if (index == 13) {
                syncedDepthOffset = value;
            } else if (index == 14) {
                syncedCutoff = value;
            } else if (index == 15) {
                syncedFalloff = value;
            } else if (index == 16) {
                syncedFalloffMult = value;
            } else if (index == 17) {
                syncedFadeDistance = value;
            } else if (index == 18) {
                syncedFadeMultiplier = value;
            } else if (index == 19) {
                syncedLodDistance = value;
            } else if (index == 20) {
                syncedAccScale = value;
            } else if (index == 21) {
                syncedBlueNoiseMix = value;
            } else if (index == 22) {
                syncedDotScale = value;
            } else if (index == 23) {
                syncedDotSizeVariability = value;
            } else if (index == 24) {
                syncedDotContrast = value;
            } else if (index == 25) {
                syncedStretchSmoothness = value;
            } else if (index == 26) {
                syncedExposure = value;
            } else {
                syncedOffset = value;
            }
        }

        void ApplyToggle(int index, bool value) {
            float flag = 0f;

            if (value) {
                flag = 1f;
            }

            if (index == 0) {
                decodeMaterial.SetFloat("_BlueNoiseJitter", flag);
            } else {
                decodeMaterial.SetFloat("_Shape", flag);
            }
        }

        void ApplyDropdown(int index, int value) {
            ApplyBlendMode(value);
            ShowRows(value);
        }

        void ApplyBlendMode(int mode) {
            decodeMaterial.SetFloat("_Mode", (float)mode);

            if (mode == 4) {
                decodeMaterial.shader = accumulateShader;
                decodeMaterial.renderQueue = 3000;
                return;
            }

            decodeMaterial.shader = decodeShader;

            if (mode == 0) {
                decodeMaterial.EnableKeyword("_MODE_TWOPASS");
            } else {
                decodeMaterial.DisableKeyword("_MODE_TWOPASS");
            }

            if (mode == 1) {
                decodeMaterial.EnableKeyword("_MODE_ZWRITE");
            } else {
                decodeMaterial.DisableKeyword("_MODE_ZWRITE");
            }

            if (mode == 2) {
                decodeMaterial.EnableKeyword("_MODE_DITHER");
            } else {
                decodeMaterial.DisableKeyword("_MODE_DITHER");
            }

            if (mode == 3) {
                decodeMaterial.EnableKeyword("_MODE_CUTOUT");
            } else {
                decodeMaterial.DisableKeyword("_MODE_CUTOUT");
            }

            bool masked = mode >= 2;
            float zWrite = 1f;

            if (mode == 0) {
                zWrite = 0f;
            }

            decodeMaterial.SetFloat("_ZWrite", zWrite);
            float srcBlend = 5f;

            if (masked) {
                srcBlend = 1f;
            }

            decodeMaterial.SetFloat("_SrcBlend", srcBlend);
            float dstBlend = 10f;

            if (masked) {
                dstBlend = 0f;
            }

            decodeMaterial.SetFloat("_DstBlend", dstBlend);
            int renderQueue = 3000;

            if (masked) {
                renderQueue = 2450;
            }

            decodeMaterial.renderQueue = renderQueue;
            float stencilRef = 0f;

            if (mode == 0) {
                stencilRef = 1f;
            }

            decodeMaterial.SetFloat("_StencilRef", stencilRef);
            float stencilComp = 8f;

            if (mode == 0) {
                stencilComp = 3f;
            }

            decodeMaterial.SetFloat("_StencilComp", stencilComp);
        }

        void ApplySlider(int index, float value) {
            if (index == 0) {
                fitMaterial.SetFloat("_SobelThreshold", value);
            } else if (index == 1) {
                fitMaterial.SetFloat("_ReprojectTolerance", value);
            } else if (index == 2) {
                fitMaterial.SetInt("_ReprojectViews", (int)value);
            } else if (index == 3) {
                fitMaterial.SetFloat("_RadiusMin", value);
            } else if (index == 4) {
                fitMaterial.SetFloat("_RadiusMax", value);
            } else if (index == 5) {
                fitMaterial.SetFloat("_DetailScale", value);
            } else if (index == 6) {
                splatMaterial.SetFloat("_SeamBlend", value);
            } else if (index == 7) {
                splatMaterial.SetFloat("_GrazingCutoff", value);
            } else if (index == 8) {
                splatMaterial.SetFloat("_MaxStretch", value);
            } else if (index == 9) {
                splatMaterial.SetFloat("_HullThreshold", value);
            } else if (index == 10) {
                splatMaterial.SetFloat("_HullFeather", value);
            } else if (index == 11) {
                decodeMaterial.SetFloat("_Radius", value);
            } else if (index == 12) {
                decodeMaterial.SetFloat("_Stride", value);
            } else if (index == 13) {
                decodeMaterial.SetFloat("_DepthOffset", value);
            } else if (index == 14) {
                decodeMaterial.SetFloat("_Cutoff", value);
            } else if (index == 15) {
                decodeMaterial.SetFloat("_Falloff", value);
            } else if (index == 16) {
                decodeMaterial.SetFloat("_FalloffMult", value);
            } else if (index == 17) {
                decodeMaterial.SetFloat("_FadeDistance", value);
            } else if (index == 18) {
                decodeMaterial.SetFloat("_FadeMultiplier", value);
            } else if (index == 19) {
                decodeMaterial.SetFloat("_LodDistance", value);
            } else if (index == 20) {
                decodeMaterial.SetFloat("_AccScale", value);
            } else if (index == 21) {
                decodeMaterial.SetFloat("_BlueNoiseMix", value);
            } else if (index == 22) {
                decodeMaterial.SetFloat("_Scale", value);
            } else if (index == 23) {
                decodeMaterial.SetFloat("_SizeVariability", value);
            } else if (index == 24) {
                decodeMaterial.SetFloat("_Contrast", value);
            } else if (index == 25) {
                decodeMaterial.SetFloat("_StretchSmoothness", value);
            } else if (index == 26) {
                decodeMaterial.SetFloat("_InputExposure", value);
            } else {
                decodeMaterial.SetFloat("_InputOffset", value);
            }
        }

        void PushToggle(int index, bool value) {
            if (toggles[index] != null) {
                toggles[index].SetIsOnWithoutNotify(value);
            }
        }

        void PushDropdown(int index, int value) {
            TMP_Dropdown dropdown = drops[index];

            if (dropdown == null) {
                return;
            }

            dropdown.SetValueWithoutNotify(value);
            dropdown.RefreshShownValue();
        }

        void PushSlider(int index, float value) {
            if (sliders[index] != null) {
                sliders[index].SetValueWithoutNotify(value);
            }
        }

        void WriteLabel(int index) {
            if (sliderLabels == null || index >= sliderLabels.Length) {
                return;
            }

            TMP_Text label = sliderLabels[index];
            Slider slider = sliders[index];

            if (label == null || slider == null) {
                return;
            }

            float value = slider.value;

            if (slider.wholeNumbers) {
                label.text = Mathf.RoundToInt(value).ToString();
                return;
            }

            int hundredths = Mathf.RoundToInt(value * 100f);
            int whole = hundredths / 100;
            int fraction = hundredths - whole * 100;

            if (fraction < 0) {
                fraction = -fraction;
            }

            string pad = "";

            if (fraction < 10) {
                pad = "0";
            }

            label.text = whole.ToString() + "." + pad + fraction.ToString();
        }

        void ShowRow(Component control, bool on) {
            if (control == null) {
                return;
            }

            control.transform.parent.gameObject.SetActive(on);
        }

        void ShowRows(int mode) {
            bool blueNoise = Mathf.RoundToInt(decodeMaterial.GetFloat("_Dither")) == 1;
            bool fractal = Mathf.RoundToInt(decodeMaterial.GetFloat("_Dither")) == 2;
            bool falloffTex = decodeMaterial.GetFloat("_UseFalloffTex") > 0.5f;
            bool twoPassOrAcc = mode == 0 || mode == 4;
            bool ditherOn = mode == 0 || mode == 2 || mode == 4;
            ShowRow(depthOffset, twoPassOrAcc);
            ShowRow(cutoff, mode == 0 || mode == 3 || mode == 4);
            ShowRow(accScale, mode == 4);
            ShowRow(blueNoiseMix, ditherOn && blueNoise);
            ShowRow(blueNoiseJitter, ditherOn && blueNoise);
            ShowRow(falloff, !falloffTex && mode != 3);
            ShowRow(rectangular, !falloffTex);
            ShowRow(dotScale, ditherOn && fractal);
            ShowRow(dotSizeVariability, ditherOn && fractal);
            ShowRow(dotContrast, ditherOn && fractal);
            ShowRow(stretchSmoothness, ditherOn && fractal);
            ShowRow(exposure, ditherOn && fractal);
            ShowRow(offset, ditherOn && fractal);
        }
    }
}
