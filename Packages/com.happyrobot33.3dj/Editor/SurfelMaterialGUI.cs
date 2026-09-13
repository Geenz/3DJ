using UnityEditor;
using UnityEngine;

namespace com.happyrobot33.holographicreprojector.Editor {
    public class SurfelMaterialGUI : ShaderGUI {
        public override void OnGUI(MaterialEditor editor, MaterialProperty[] props) {
            Material mat = (Material)editor.target;
            editor.SetDefaultGUIWidths();

            foreach (MaterialProperty prop in props) {
                if ((prop.flags & MaterialProperty.PropFlags.HideInInspector) != 0) {
                    continue;
                }

                if (!Visible(prop.name, mat)) {
                    continue;
                }

                GUIContent label = new GUIContent(prop.displayName, Tooltip(mat.shader, prop.name));
                editor.ShaderProperty(prop, label);
            }

            editor.RenderQueueField();
            editor.EnableInstancingField();
            editor.DoubleSidedGIField();
        }

        protected virtual bool Visible(string name, Material mat) {
            return true;
        }

        static string Tooltip(Shader shader, string name) {
            int index = shader.FindPropertyIndex(name);

            if (index < 0) {
                return "";
            }

            foreach (string attr in shader.GetPropertyAttributes(index)) {
                if (attr.StartsWith("Tooltip(") && attr.EndsWith(")")) {
                    return attr.Substring(8, attr.Length - 9);
                }
            }

            return "";
        }
    }
}
