using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRC.SDKBase.Editor.BuildPipeline;

namespace com.happyrobot33.holographicreprojector.Editor {
    // Order 100 puts this after ManagerEditor's own callback, which resizes the extract textures first.
    public class SurfelPlaybackBuildHook : IVRCSDKBuildRequestedCallback {
        public int callbackOrder { get { return 100; } }

        public bool OnBuildRequested(VRCSDKRequestedBuildType requestedBuildType) {
            Manager manager = SurfelRenderComponentEditor.FindManager();

            if (manager == null) {
                return true;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++) {
                Scene scene = SceneManager.GetSceneAt(i);

                if (!scene.IsValid() || !scene.isLoaded) {
                    continue;
                }

                foreach (GameObject root in scene.GetRootGameObjects()) {
                    foreach (SurfelRenderComponent component in root.GetComponentsInChildren<SurfelRenderComponent>(true)) {
                        RebakeIfStale(manager, component);
                    }
                }
            }

            return true;
        }

        static void RebakeIfStale(Manager manager, SurfelRenderComponent component) {
            int width, height, stride;

            if (!SurfelRenderComponentEditor.BakedInfo(component.gameObject, out width, out height, out stride)) {
                return;
            }

            if (width == manager.DepthTextureSize.x && height == manager.DepthTextureSize.y) {
                return;
            }

            Mesh mesh = SurfelRenderComponentEditor.BakeMesh(manager.DepthTextureSize, stride);

            SerializedObject so = new SerializedObject(component);
            so.FindProperty("mesh").objectReferenceValue = mesh;
            so.ApplyModifiedProperties();
        }
    }
}
