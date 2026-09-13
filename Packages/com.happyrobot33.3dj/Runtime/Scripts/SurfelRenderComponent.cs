using UdonSharp;
using UnityEngine;

namespace com.happyrobot33.holographicreprojector {
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SurfelRenderComponent : UdonSharpBehaviour {
        public Mesh mesh;
        public int stride = 1;
        public CustomRenderTexture strip;
        public CustomRenderTexture metaFit;
        public CustomRenderTexture metaSplat;
        public CustomRenderTexture depth, color;

        void Start() {
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }
    }
}
