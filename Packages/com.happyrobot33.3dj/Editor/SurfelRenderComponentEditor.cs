namespace com.happyrobot33.holographicreprojector.Editor {
    using System.IO;
    using System.Reflection;
    using UdonSharpEditor;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.Rendering;

    [CustomEditor(typeof(SurfelRenderComponent))]
    public class SurfelRenderComponentEditor : Editor {
        // Every size comes from the scene Manager at bake time and from the _Udon_3DJ_* globals at runtime;
        // nothing here may hard-code a resolution, a tile size or a 3DJ asset path.
        public const string PkgRoot = "Packages/com.happyrobot33.3dj";
        public const string SurfelDir = PkgRoot + "/Runtime/Playback/Surfel";
        public const string ShaderDir = SurfelDir + "/Shaders";
        public const string GenDir = SurfelDir + "/Generated";
        public const string BlueNoisePath = ShaderDir + "/BlueNoise64.png";
        public const string Dither3DDir = ShaderDir + "/Dither3D";
        public const string DitherTexPath = Dither3DDir + "/Dither3D_8x8.asset";
        public const string DitherRampPath = Dither3DDir + "/Dither3D_8x8_Ramp.png";

        public const string MeshPrefix = "SurfelPlaybackCloud_";
        public const string DecodeMatPath = GenDir + "/SurfelPlaybackDecode.mat";
        public const string MetaMatPath = GenDir + "/SurfelPlaybackMeta.mat";
        public const string MetaSplatMatPath = GenDir + "/SurfelPlaybackMetaSplat.mat";
        public const string StripMatPath = GenDir + "/SurfelPlaybackStrip.mat";
        public const string SnapshotMatPath = GenDir + "/SurfelPlaybackSnapshot.mat";
        public const string StripCrtPath = GenDir + "/SurfelPlaybackStrip.asset";
        public const string MetaFitCrtPath = GenDir + "/SurfelPlaybackMetaFit.asset";
        public const string MetaSplatCrtPath = GenDir + "/SurfelPlaybackMetaSplat.asset";
        public const string DepthCrtPath = GenDir + "/SurfelPlaybackDepth.asset";
        public const string ColorCrtPath = GenDir + "/SurfelPlaybackColor.asset";

        public const string DecodeShaderName = "SurfelAtlas/Decode";
        public const string MetaShaderName = "SurfelAtlas/Meta";
        public const string MetaSplatShaderName = "SurfelAtlas/MetaSplat";
        public const string StripShaderName = "SurfelAtlas/ThreeDJStrip";
        public const string SnapshotShaderName = "SurfelAtlas/Snapshot";

        // DJ_Position spans +-5242.87 m and the decoder ignores our transform, so the mesh must never be culled.
        const float BoundsSize = 24000f;

        public static readonly Color MetaEmpty = new Color(0.5f, 0.5f, 0f, 0f);

        static readonly Vector3[] QuadVerts = {
            new Vector3(-1, -1, 0),
            new Vector3(1, -1, 0),
            new Vector3(1, 1, 0),
            new Vector3(-1, 1, 0)
        };

        static readonly int[] QuadTris = { 0, 1, 2, 0, 2, 3 };

        void OnEnable() {
            EnsureSetup((SurfelRenderComponent)target);
        }

        // Idempotent: re-selecting an already-configured component must never reset a tuned material.
        static void EnsureSetup(SurfelRenderComponent component) {
            Manager manager = FindManager();

            if (manager == null) {
                return;
            }

            if (!EnsureGenFolder()) {
                return;
            }

            Shader decodeShader = Shader.Find(DecodeShaderName);
            Shader metaShader = Shader.Find(MetaShaderName);
            Shader splatShader = Shader.Find(MetaSplatShaderName);
            Shader stripShader = Shader.Find(StripShaderName);
            Shader snapshotShader = Shader.Find(SnapshotShaderName);

            if (decodeShader == null || metaShader == null || splatShader == null || stripShader == null || snapshotShader == null) {
                return;
            }

            bool decodeIsNew = AssetDatabase.LoadAssetAtPath<Material>(DecodeMatPath) == null;
            bool metaIsNew = AssetDatabase.LoadAssetAtPath<Material>(MetaMatPath) == null;
            bool splatIsNew = AssetDatabase.LoadAssetAtPath<Material>(MetaSplatMatPath) == null;

            Material decodeMat = EnsureMaterial(DecodeMatPath, decodeShader);
            Material metaMat = EnsureMaterial(MetaMatPath, metaShader);
            Material splatMat = EnsureMaterial(MetaSplatMatPath, splatShader);
            Material stripMat = EnsureMaterial(StripMatPath, stripShader);
            Material snapshotMat = EnsureMaterial(SnapshotMatPath, snapshotShader);

            Material newDecode = null;
            Material newMeta = null;
            Material newSplat = null;

            if (decodeIsNew) {
                newDecode = decodeMat;
            }

            if (metaIsNew) {
                newMeta = metaMat;
            }

            if (splatIsNew) {
                newSplat = splatMat;
            }

            ConfigureDefaults(newDecode, newMeta, newSplat);

            Vector2Int size = manager.DepthTextureSize;
            Vector2Int colorSize = manager.ColorTextureSize;
            CustomRenderTexture depthCrt = EnsureCrt(DepthCrtPath, size.x, size.y, manager.DepthExtractTexture.format, snapshotMat, Color.clear, 0);
            CustomRenderTexture colorCrt = EnsureCrt(ColorCrtPath, colorSize.x, colorSize.y, manager.ColorExtractTexture.format, snapshotMat, Color.clear, 1);
            CustomRenderTexture stripCrt = EnsureCrt(StripCrtPath, 2, 1, RenderTextureFormat.ARGBFloat, stripMat, Color.clear, 0);
            CustomRenderTexture metaFitCrt = EnsureCrt(MetaFitCrtPath, size.x, size.y, RenderTextureFormat.ARGBHalf, metaMat, MetaEmpty, 0);
            CustomRenderTexture metaSplatCrt = EnsureCrt(MetaSplatCrtPath, size.x, size.y, RenderTextureFormat.ARGBHalf, splatMat, Color.clear, 0);

            if (!WireMaterials(decodeMat, metaMat, splatMat, stripCrt, metaFitCrt, metaSplatCrt, depthCrt, colorCrt, component.stride)) {
                return;
            }

            Mesh mesh = EnsureMesh(size, component.stride);

            MeshRenderer meshRenderer = component.GetComponent<MeshRenderer>();
            Undo.RecordObject(meshRenderer, "Configure Surfel Playback");
            meshRenderer.sharedMaterial = decodeMat;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty meshProp = serialized.FindProperty("mesh");

            if (meshProp.objectReferenceValue == null) {
                meshProp.objectReferenceValue = mesh;
            }

            serialized.FindProperty("strip").objectReferenceValue = stripCrt;
            serialized.FindProperty("metaFit").objectReferenceValue = metaFitCrt;
            serialized.FindProperty("metaSplat").objectReferenceValue = metaSplatCrt;
            serialized.FindProperty("depth").objectReferenceValue = depthCrt;
            serialized.FindProperty("color").objectReferenceValue = colorCrt;
            serialized.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI() {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) {
                return;
            }

            DrawDefaultInspector();

            if (GUILayout.Button("Assign as Manager playback object")) {
                AssignAsManagerPlaybackObject((SurfelRenderComponent)target);
            }
        }

        static void AssignAsManagerPlaybackObject(SurfelRenderComponent component) {
            Manager manager = FindManager();

            if (manager == null) {
                return;
            }

            MethodInfo updateTextureInternals = typeof(ManagerEditor).GetMethod("UpdateAllTextureInternals", BindingFlags.NonPublic | BindingFlags.Static);

            if (updateTextureInternals == null) {
                return;
            }

            updateTextureInternals.Invoke(null, new object[] { manager });

            GameObject previous = manager.mainPlaybackCube;
            AssignPlaybackObject(manager, component.gameObject);

            if (previous != null && previous != component.gameObject) {
                Undo.RecordObject(previous, "Disable Previous Playback Object");
                previous.SetActive(false);
            }
        }

        public static bool EnsureGenFolder() {
            if (AssetDatabase.IsValidFolder(GenDir)) {
                return true;
            }

            if (!AssetDatabase.IsValidFolder(SurfelDir)) {
                Debug.LogError("Surfel Playback: " + SurfelDir + " is not in the AssetDatabase; the package must be embedded under Packages/.");
                return false;
            }

            if (!string.IsNullOrEmpty(AssetDatabase.CreateFolder(SurfelDir, "Generated"))) {
                return true;
            }

            Debug.LogError("Surfel Playback: could not create " + GenDir + ".");
            return false;
        }

        public static CustomRenderTexture EnsureCrt(string path, int width, int height, RenderTextureFormat format, Material material, Color initColor, int shaderPass) {
            CustomRenderTexture crt = AssetDatabase.LoadAssetAtPath<CustomRenderTexture>(path);

            if (crt == null) {
                crt = new CustomRenderTexture(width, height, format, RenderTextureReadWrite.Linear) {
                    depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None,
                    updateMode = CustomRenderTextureUpdateMode.Realtime,
                    initializationMode = CustomRenderTextureUpdateMode.OnLoad,
                    initializationSource = CustomRenderTextureInitializationSource.TextureAndColor,
                    initializationColor = initColor,
                    doubleBuffered = false,
                    material = material,
                    shaderPass = shaderPass,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
                AssetDatabase.CreateAsset(crt, path);
                return crt;
            }

            if (crt.width != width || crt.height != height || crt.format != format || crt.depthStencilFormat != UnityEngine.Experimental.Rendering.GraphicsFormat.None || crt.sRGB) {
                crt.Release();
                crt.width = width;
                crt.height = height;
                crt.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormatUtility.GetGraphicsFormat(format, false);
                crt.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.None;
                crt.Create();
            }

            crt.updateMode = CustomRenderTextureUpdateMode.Realtime;
            crt.initializationMode = CustomRenderTextureUpdateMode.OnLoad;
            crt.initializationSource = CustomRenderTextureInitializationSource.TextureAndColor;
            crt.initializationColor = initColor;
            crt.doubleBuffered = false;
            crt.material = material;
            crt.shaderPass = shaderPass;
            crt.filterMode = FilterMode.Point;
            crt.wrapMode = TextureWrapMode.Clamp;
            EditorUtility.SetDirty(crt);
            return crt;
        }

        public static Material EnsureMaterial(string path, Shader shader) {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null) {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            } else if (mat.shader != shader) {
                mat.shader = shader;
            }

            return mat;
        }

        // Applied only when a material was just created; an existing material's tuned keywords and values stand.
        public static void ConfigureDefaults(Material decodeMat, Material metaMat, Material splatMat) {
            if (decodeMat != null) {
                decodeMat.SetFloat("_UseFalloffTex", 0f);
                decodeMat.DisableKeyword("_FALLOFFTEX_ON");

                decodeMat.SetFloat("_Mode", 0f);
                decodeMat.EnableKeyword("_MODE_TWOPASS");
                decodeMat.DisableKeyword("_MODE_ZWRITE");
                decodeMat.DisableKeyword("_MODE_DITHER");
                decodeMat.DisableKeyword("_MODE_CUTOUT");

                float zWrite, srcBlend, dstBlend, stencilRef, stencilComp;
                int queue;
                SurfelDecodeGUI.BlendState(0, out zWrite, out srcBlend, out dstBlend, out queue, out stencilRef, out stencilComp);
                decodeMat.SetFloat("_ZWrite", zWrite);
                decodeMat.SetFloat("_SrcBlend", srcBlend);
                decodeMat.SetFloat("_DstBlend", dstBlend);
                decodeMat.SetFloat("_StencilRef", stencilRef);
                decodeMat.SetFloat("_StencilComp", stencilComp);
                decodeMat.renderQueue = queue;

                decodeMat.SetFloat("_Dither", 1f);
                decodeMat.EnableKeyword("_DITHER_BLUENOISE");
                decodeMat.DisableKeyword("_DITHER_BAYER");
                decodeMat.DisableKeyword("_DITHER_FRACTAL");
            }

            if (metaMat != null) {
                metaMat.SetFloat("_Sobel", 1f);
                metaMat.EnableKeyword("_SOBEL_ON");
                metaMat.SetFloat("_ReprojectOn", 1f);
                metaMat.EnableKeyword("_REPROJECT_ON");
                metaMat.SetFloat("_FitWindow", 1f);
                metaMat.EnableKeyword("_FITWINDOW_R2");
                metaMat.DisableKeyword("_FITWINDOW_R1");
                metaMat.DisableKeyword("_FITWINDOW_R3");
                metaMat.DisableKeyword("_FITWINDOW_R4");
            }

            if (splatMat != null) {
                splatMat.SetFloat("_ShearOn", 1f);
                splatMat.EnableKeyword("_SHEAR_ON");
                splatMat.SetFloat("_HullOn", 1f);
                splatMat.EnableKeyword("_HULL_ON");
                splatMat.SetFloat("_HullThreshold", 0.5f);
                splatMat.SetFloat("_HullFeather", 0f);
                splatMat.SetFloat("_SeamBlend", 1.4f);
                splatMat.SetFloat("_GrazingCutoff", 0.15f);
                splatMat.SetFloat("_MaxStretch", 5f);
            }
        }

        // Runs on every material regardless of age: the atlas, stride and blue noise wiring is never a tuned value.
        public static bool WireMaterials(Material decodeMat, Material metaMat, Material splatMat,
                                    CustomRenderTexture stripCrt, CustomRenderTexture metaFitCrt, CustomRenderTexture metaSplatCrt,
                                    CustomRenderTexture depthCrt, CustomRenderTexture colorCrt, int stride) {
            decodeMat.SetTexture("_Strip3DJ", stripCrt);
            metaMat.SetTexture("_Strip3DJ", stripCrt);
            splatMat.SetTexture("_Strip3DJ", stripCrt);
            decodeMat.SetTexture("_Meta", metaFitCrt);
            splatMat.SetTexture("_Meta", metaFitCrt);
            decodeMat.SetTexture("_MetaSplat", metaSplatCrt);
            decodeMat.SetTexture("_SurfelDepth", depthCrt);
            metaMat.SetTexture("_SurfelDepth", depthCrt);
            splatMat.SetTexture("_SurfelDepth", depthCrt);
            decodeMat.SetTexture("_SurfelColor", colorCrt);
            decodeMat.SetFloat("_Stride", stride);

            Texture2D blueNoise = AssetDatabase.LoadAssetAtPath<Texture2D>(BlueNoisePath);

            if (blueNoise == null) {
                Debug.LogError("Surfel Playback: " + BlueNoisePath + " not found.");
                return false;
            }

            decodeMat.SetTexture("_BlueNoise", blueNoise);

            Texture3D ditherTex = AssetDatabase.LoadAssetAtPath<Texture3D>(DitherTexPath);

            if (ditherTex == null) {
                Debug.LogError("Surfel Playback: " + DitherTexPath + " not found.");
                return false;
            }

            Texture2D ditherRamp = AssetDatabase.LoadAssetAtPath<Texture2D>(DitherRampPath);

            if (ditherRamp == null) {
                Debug.LogError("Surfel Playback: " + DitherRampPath + " not found.");
                return false;
            }

            decodeMat.SetTexture("_DitherTex", ditherTex);
            decodeMat.SetTexture("_DitherRampTex", ditherRamp);

            EditorUtility.SetDirty(decodeMat);
            EditorUtility.SetDirty(metaMat);
            EditorUtility.SetDirty(splatMat);
            return true;
        }

        public static Manager FindManager() {
            GameObject named = GameObject.Find(Manager.MANAGERNAME);
            Manager manager = null;

            if (named != null) {
                manager = named.GetComponent<Manager>();
            }

            if (manager != null) {
                return manager;
            }

            // FindObjectOfType(bool) is 2020.3+; FindObjectsOfTypeAll also returns prefab assets, hence the scene filter.
            foreach (Manager candidate in Resources.FindObjectsOfTypeAll<Manager>()) {
                if (candidate != null && candidate.gameObject.scene.IsValid() && candidate.gameObject.scene.isLoaded) {
                    return candidate;
                }
            }

            return null;
        }

        public static string MeshName(Vector2Int size, int stride) { return MeshPrefix + "s" + stride + "_" + size.x + "x" + size.y; }

        public static Mesh EnsureMesh(Vector2Int size, int stride) {
            string path = GenDir + "/" + MeshName(size, stride) + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (mesh != null) {
                return mesh;
            }

            return BakeMesh(size, stride);
        }

        public static Mesh BakeMesh(Vector2Int size, int stride) {
            string assetName = MeshName(size, stride);
            string path = GenDir + "/" + assetName + ".asset";

            Mesh fresh = GenerateMesh(size, stride);
            fresh.name = assetName;

            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (mesh == null) {
                AssetDatabase.CreateAsset(fresh, path);
                mesh = fresh;
            } else {
                EditorUtility.CopySerialized(fresh, mesh);
                mesh.name = assetName;
                Object.DestroyImmediate(fresh);
                EditorUtility.SetDirty(mesh);
            }

            // A stride-1 cloud is hundreds of MB of YAML in a git-tracked package; only the current bake survives.
            if (Directory.Exists(GenDir)) {
                foreach (string file in Directory.GetFiles(GenDir, MeshPrefix + "*.asset")) {
                    string stale = file.Replace('\\', '/');

                    if (stale != path) {
                        AssetDatabase.DeleteAsset(stale);
                    }
                }
            }

            return mesh;
        }

        // uv0 carries the integer depth texel; the shader derives the tile and the face from the global texel size.
        public static Mesh GenerateMesh(Vector2Int size, int stride) {
            int cols = size.x / stride, rows = size.y / stride;
            long blocks = (long)cols * rows;
            var verts = new Vector3[blocks * 4];
            var uv0 = new Vector2[blocks * 4];
            var tris = new int[blocks * 6];

            int block = 0;

            for (int blockY = 0; blockY < rows * stride; blockY += stride) {
                for (int blockX = 0; blockX < cols * stride; blockX += stride) {
                    var texel = new Vector2(blockX + stride / 2, blockY + stride / 2);
                    int baseV = block * 4;

                    for (int i = 0; i < 4; i++) {
                        verts[baseV + i] = QuadVerts[i];
                        uv0[baseV + i] = texel;
                    }

                    int baseT = block * 6;

                    for (int i = 0; i < 6; i++) {
                        tris[baseT + i] = baseV + QuadTris[i];
                    }

                    block++;
                }
            }

            var mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.uv = uv0;
            mesh.triangles = tris;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * BoundsSize);
            return mesh;
        }

        static Mesh MeshOf(GameObject gameObject) {
            SurfelRenderComponent component = gameObject.GetComponent<SurfelRenderComponent>();
            Mesh result = null;

            if (component != null) {
                result = component.mesh;
            }

            return result;
        }

        public static bool BakedInfo(GameObject gameObject, out int width, out int height, out int stride) {
            width = height = stride = 0;
            Mesh mesh = MeshOf(gameObject);
            return mesh != null && TryParseMeshName(mesh.name, out width, out height, out stride);
        }

        public static bool TryParseMeshName(string name, out int width, out int height, out int stride) {
            width = height = stride = 0;

            if (string.IsNullOrEmpty(name) || !name.StartsWith(MeshPrefix)) {
                return false;
            }

            string[] parts = name.Substring(MeshPrefix.Length).Split('_');

            if (parts.Length != 2 || parts[0].Length < 2 || parts[0][0] != 's') {
                return false;
            }

            string[] sizeParts = parts[1].Split('x');

            if (sizeParts.Length != 2) {
                return false;
            }

            return int.TryParse(parts[0].Substring(1), out stride) && int.TryParse(sizeParts[0], out width) && int.TryParse(sizeParts[1], out height)
                && stride > 0 && width > 0 && height > 0;
        }

        public static void AssignPlaybackObject(Manager manager, GameObject playbackObject) {
            SerializedObject serialized = new SerializedObject(manager);
            SerializedProperty property = serialized.FindProperty("mainPlaybackCube");

            if (property == null) {
                Debug.LogError("Surfel Playback: Manager has no mainPlaybackCube field.");
                return;
            }

            property.objectReferenceValue = playbackObject;
            serialized.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }
    }
}
