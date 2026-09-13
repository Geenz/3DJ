using UnityEditor;
using UnityEngine;

namespace com.happyrobot33.holographicreprojector.Editor {
    public class SurfelDecodeGUI : SurfelMaterialGUI {
        const string Base = "SurfelAtlas/Decode";
        const string Acc = "SurfelAtlas/DecodeAccumulate";

        public override void OnGUI(MaterialEditor editor, MaterialProperty[] props) {
            base.OnGUI(editor, props);

            bool swapped = false;

            foreach (var target in editor.targets) {
                var mat = target as Material;

                if (mat == null) {
                    continue;
                }

                int mode = (int)mat.GetFloat("_Mode");
                bool changed = false;

                string wantedShaderName = Base;

                if (mode == 4) {
                    wantedShaderName = Acc;
                }

                var shader = Shader.Find(wantedShaderName);

                if (shader != null && mat.shader != shader) {
                    mat.shader = shader;
                    swapped = true;
                    changed = true;
                }

                if (mode == 4) {
                    int accumulateQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

                    if (mat.renderQueue != accumulateQueue) {
                        mat.renderQueue = accumulateQueue;
                        changed = true;
                    }

                    if (changed) {
                        EditorUtility.SetDirty(mat);
                    }

                    continue;
                }

                float zWrite, srcBlend, dstBlend, stencilRef, stencilComp;
                int queue;
                BlendState(mode, out zWrite, out srcBlend, out dstBlend, out queue, out stencilRef, out stencilComp);

                if (mat.GetFloat("_ZWrite") != zWrite) {
                    mat.SetFloat("_ZWrite", zWrite);
                    changed = true;
                }

                if (mat.GetFloat("_SrcBlend") != srcBlend) {
                    mat.SetFloat("_SrcBlend", srcBlend);
                    changed = true;
                }

                if (mat.GetFloat("_DstBlend") != dstBlend) {
                    mat.SetFloat("_DstBlend", dstBlend);
                    changed = true;
                }

                if (mat.renderQueue != queue) {
                    mat.renderQueue = queue;
                    changed = true;
                }

                if (mat.GetFloat("_StencilRef") != stencilRef) {
                    mat.SetFloat("_StencilRef", stencilRef);
                    changed = true;
                }

                if (mat.GetFloat("_StencilComp") != stencilComp) {
                    mat.SetFloat("_StencilComp", stencilComp);
                    changed = true;
                }

                if (changed) {
                    EditorUtility.SetDirty(mat);
                }
            }

            if (swapped) {
                GUIUtility.ExitGUI();
            }
        }

        protected override bool Visible(string name, Material mat) {
            int mode = (int)mat.GetFloat("_Mode");
            int dither = (int)mat.GetFloat("_Dither");
            bool useFalloffTex = mat.GetFloat("_UseFalloffTex") > 0.5f;

            bool ditherVisible = false;

            if (mode == 0 || mode == 2 || mode == 4) {
                ditherVisible = true;
            }

            if (name == "_DepthOffset") {
                return mode == 0 || mode == 4;
            }

            if (name == "_Cutoff") {
                return mode == 0 || mode == 3 || mode == 4;
            }

            if (name == "_AccScale") {
                return mode == 4;
            }

            if (name == "_Dither") {
                return ditherVisible;
            }

            if (name == "_BlueNoise" || name == "_BlueNoiseMix" || name == "_BlueNoiseJitter") {
                return ditherVisible && dither == 1;
            }

            if (name == "_Scale" || name == "_SizeVariability" || name == "_Contrast" || name == "_StretchSmoothness" || name == "_InputExposure" || name == "_InputOffset") {
                return ditherVisible && dither == 2;
            }

            if (name == "_Falloff") {
                return !useFalloffTex && mode != 3;
            }

            if (name == "_Shape") {
                return !useFalloffTex;
            }

            if (name == "_FalloffTex") {
                return useFalloffTex;
            }

            return true;
        }

        // Modes 0-3 only; mode 4 (Accumulate) uses a different shader and pass setup entirely.
        public static void BlendState(int mode, out float zWrite, out float srcBlend, out float dstBlend, out int queue, out float stencilRef, out float stencilComp) {
            bool masked = mode >= 2;

            zWrite = 1f;

            if (mode == 0) {
                zWrite = 0f;
            }

            srcBlend = (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
            dstBlend = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
            queue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            if (masked) {
                srcBlend = (int)UnityEngine.Rendering.BlendMode.One;
                dstBlend = (int)UnityEngine.Rendering.BlendMode.Zero;
                queue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }

            stencilRef = 0f;
            stencilComp = (int)UnityEngine.Rendering.CompareFunction.Always;

            if (mode == 0) {
                stencilRef = 1f;
                stencilComp = (int)UnityEngine.Rendering.CompareFunction.Equal;
            }
        }
    }
}
