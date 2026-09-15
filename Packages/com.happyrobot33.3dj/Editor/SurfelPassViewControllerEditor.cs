namespace com.happyrobot33.holographicreprojector.Editor {
    using System.Collections.Generic;
    using TMPro;
    using UdonSharpEditor;
    using UnityEditor;
    using UnityEditor.Events;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;
    using VRC.SDK3.Components;
    using VRC.Udon;

    [CustomEditor(typeof(SurfelPassViewController))]
    public class SurfelPassViewControllerEditor : Editor {
        const float HeaderH = 46f;
        const float ToggleH = 38f;
        const float SliderH = 42f;
        const float DropdownH = 50f;

        static readonly Color LabelColor = new Color(0.16f, 0.16f, 0.18f, 1f);

        static DefaultControls.Resources uiResources;
        static TMP_DefaultControls.Resources tmpResources;
        static SerializedObject serializedObject;
        static UdonBehaviour udonBehaviour;
        static List<TMP_Text> sliderLabelList;
        static int uiLayer;
        static int order;

        void OnEnable() {
            BuildControls((SurfelPassViewController)target);
        }

        public override void OnInspectorGUI() {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) {
                return;
            }

            DrawDefaultInspector();
        }

        static void BuildControls(SurfelPassViewController component) {
            if (TMP_Settings.instance == null) {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                Debug.LogWarning("Surfel Pass View Controller: imported TMP Essentials; reselect the component.");
                return;
            }

            GameObject panelGameObject = component.gameObject;
            GameObject canvasGameObject = panelGameObject;
            Canvas canvas = component.GetComponentInParent<Canvas>();

            if (canvas != null) {
                canvasGameObject = canvas.gameObject;
            }

            VerticalLayoutGroup verticalLayoutGroup = panelGameObject.GetComponent<VerticalLayoutGroup>();

            if (verticalLayoutGroup == null) {
                verticalLayoutGroup = panelGameObject.AddComponent<VerticalLayoutGroup>();
            }

            verticalLayoutGroup.padding = new RectOffset(14, 14, 14, 14);
            verticalLayoutGroup.spacing = 5f;
            verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            verticalLayoutGroup.childControlWidth = true;
            verticalLayoutGroup.childControlHeight = true;
            verticalLayoutGroup.childForceExpandWidth = true;
            verticalLayoutGroup.childForceExpandHeight = false;

            ContentSizeFitter contentSizeFitter = panelGameObject.GetComponent<ContentSizeFitter>();

            if (contentSizeFitter == null) {
                contentSizeFitter = panelGameObject.AddComponent<ContentSizeFitter>();
            }

            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            if (canvasGameObject.GetComponent<GraphicRaycaster>() == null) {
                canvasGameObject.AddComponent<GraphicRaycaster>();
            }

            if (canvasGameObject.GetComponent<VRCUiShape>() == null) {
                canvasGameObject.AddComponent<VRCUiShape>();
            }

            if (canvasGameObject.GetComponent<BoxCollider>() == null) {
                canvasGameObject.AddComponent<BoxCollider>();
            }

            uiLayer = panelGameObject.layer;
            uiResources = new DefaultControls.Resources {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")
            };

            tmpResources = new TMP_DefaultControls.Resources {
                standard = uiResources.standard,
                background = uiResources.background,
                checkmark = uiResources.checkmark,
                dropdown = uiResources.dropdown,
                mask = uiResources.mask
            };

            serializedObject = new SerializedObject(component);
            udonBehaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(component);
            sliderLabelList = new List<TMP_Text>();
            order = 0;

            Transform parentTransform = component.transform;

            Bind("accumulateShader", Shader.Find("SurfelAtlas/DecodeAccumulate"));

            Header(parentTransform, "Fit");
            Bind("sobelThreshold", SliderRow(parentTransform, "sobelThreshold", "Sobel Threshold", 0f, 8f, false));
            Bind("reprojectTolerance", SliderRow(parentTransform, "reprojectTolerance", "Reproject tolerance (m)", 0f, 0.2f, false));
            Bind("reprojectViews", SliderRow(parentTransform, "reprojectViews", "Contradicting faces to reject", 1f, 6f, true));
            Bind("radiusMin", SliderRow(parentTransform, "radiusMin", "Radius min", 0f, 4f, false));
            Bind("radiusMax", SliderRow(parentTransform, "radiusMax", "Radius max", 0f, 4f, false));
            Bind("detailScale", SliderRow(parentTransform, "detailScale", "Detail scale (m)", 0f, 0.2f, false));

            Header(parentTransform, "Splat");
            Bind("seamBlend", SliderRow(parentTransform, "seamBlend", "Seam blend", 0.5f, 3f, false));
            Bind("grazingCutoff", SliderRow(parentTransform, "grazingCutoff", "Grazing cutoff", 0f, 1f, false));
            Bind("maxStretch", SliderRow(parentTransform, "maxStretch", "Max stretch", 1f, 5f, false));
            Bind("hullThreshold", SliderRow(parentTransform, "hullThreshold", "Hull threshold", 0f, 1f, false));
            Bind("hullFeather", SliderRow(parentTransform, "hullFeather", "Hull feather", 0f, 1f, false));

            Header(parentTransform, "Decode");
            Bind("blendMode", DropdownRow(parentTransform, "blendMode", "Blend mode", new[] { "TwoPass", "ZWrite", "Dither", "Cutout", "Accumulate" }));
            Bind("blueNoiseJitter", ToggleRow(parentTransform, "blueNoiseJitter", "Blue noise jitter"));
            Bind("rectangular", ToggleRow(parentTransform, "rectangular", "Shape"));
            Bind("radius", SliderRow(parentTransform, "radius", "Splat radius", 0.1f, 5f, false));
            Bind("stride", SliderRow(parentTransform, "stride", "Stride", 1f, 8f, false));
            Bind("depthOffset", SliderRow(parentTransform, "depthOffset", "Depth offset", 0f, 4f, false));
            Bind("cutoff", SliderRow(parentTransform, "cutoff", "Cutout", 0f, 1f, false));
            Bind("falloff", SliderRow(parentTransform, "falloff", "Falloff", 0.5f, 8f, false));
            Bind("falloffMult", SliderRow(parentTransform, "falloffMult", "Falloff Multiplier", 0f, 8f, false));
            Bind("fadeDistance", SliderRow(parentTransform, "fadeDistance", "Fade distance", 0f, 5f, false));
            Bind("fadeMultiplier", SliderRow(parentTransform, "fadeMultiplier", "Fade multiplier", 0f, 8f, false));
            Bind("lodDistance", SliderRow(parentTransform, "lodDistance", "LOD distance", 1f, 100f, false));
            Bind("accScale", SliderRow(parentTransform, "accScale", "Accumulation scale", 0.01f, 1f, false));
            Bind("blueNoiseMix", SliderRow(parentTransform, "blueNoiseMix", "Blue noise mix", 0f, 1f, false));
            Bind("dotScale", SliderRow(parentTransform, "dotScale", "Dot Scale", 2f, 10f, false));
            Bind("dotSizeVariability", SliderRow(parentTransform, "dotSizeVariability", "Dot Size Variability", 0f, 1f, false));
            Bind("dotContrast", SliderRow(parentTransform, "dotContrast", "Dot Contrast", 0f, 2f, false));
            Bind("stretchSmoothness", SliderRow(parentTransform, "stretchSmoothness", "Stretch Smoothness", 0f, 2f, false));
            Bind("exposure", SliderRow(parentTransform, "exposure", "Exposure", 0f, 5f, false));
            Bind("offset", SliderRow(parentTransform, "offset", "Offset", -1f, 1f, false));

            SerializedProperty sliderLabelsProperty = serializedObject.FindProperty("sliderLabels");

            if (sliderLabelsProperty != null) {
                sliderLabelsProperty.arraySize = sliderLabelList.Count;

                for (int index = 0; index < sliderLabelList.Count; index++) {
                    sliderLabelsProperty.GetArrayElementAtIndex(index).objectReferenceValue = sliderLabelList[index];
                }
            }

            serializedObject.ApplyModifiedProperties();

            if (UdonSharpEditorUtility.IsProxyBehaviour(component)) {
                try {
                    UdonSharpEditorUtility.CopyProxyToUdon(component);
                } catch (System.InvalidOperationException) {
                }
            }

            RectTransform panelRect = (RectTransform)panelGameObject.transform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);
            RectTransform canvasRect = (RectTransform)canvasGameObject.transform;
            BoxCollider boxCollider = canvasGameObject.GetComponent<BoxCollider>();
            boxCollider.center = Vector3.zero;
            boxCollider.size = new Vector3(canvasRect.rect.width, canvasRect.rect.height, 1f);
        }

        static void Bind(string fieldName, Object value) {
            SerializedProperty property = serializedObject.FindProperty(fieldName);

            if (property != null) {
                property.objectReferenceValue = value;
            }
        }

        static RectTransform Row(Transform parent, string name, float height) {
            Transform child = parent.Find(name);
            bool created = child == null;

            if (created) {
                GameObject rowGameObject = new GameObject(name, typeof(RectTransform));
                rowGameObject.transform.SetParent(parent, false);
                rowGameObject.layer = uiLayer;
                Undo.RegisterCreatedObjectUndo(rowGameObject, "Create Surfel Control Row");
                child = rowGameObject.transform;
            }

            child.SetSiblingIndex(order++);
            LayoutElement layoutElement = child.GetComponent<LayoutElement>();

            if (layoutElement == null) {
                layoutElement = child.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredHeight = height;
            layoutElement.flexibleHeight = 0f;
            return (RectTransform)child;
        }

        static void Header(Transform parent, string title) {
            RectTransform row = Row(parent, "Header_" + title, HeaderH);
            TMP_Text label = FindText(row, "Label");

            if (label == null) {
                label = MakeText(row, "Label", title, 24f, true, TextAlignmentOptions.BottomLeft);
            }

            label.text = title;
            Stretch((RectTransform)label.transform, 0f, 1f);
        }

        static Toggle ToggleRow(Transform parent, string name, string labelText) {
            RectTransform row = Row(parent, "Row_" + name, ToggleH);
            Toggle toggle = row.GetComponentInChildren<Toggle>(true);

            if (toggle == null) {
                GameObject toggleGameObject = DefaultControls.CreateToggle(uiResources);
                toggleGameObject.name = "Toggle";
                toggleGameObject.transform.SetParent(row, false);
                Transform legacyLabel = toggleGameObject.transform.Find("Label");

                if (legacyLabel != null) {
                    Object.DestroyImmediate(legacyLabel.gameObject);
                }

                SetLayer(toggleGameObject.transform, uiLayer);
                toggle = toggleGameObject.GetComponent<Toggle>();
            }

            RectTransform toggleRectTransform = (RectTransform)toggle.transform;
            toggleRectTransform.anchorMin = new Vector2(0.44f, 0.5f);
            toggleRectTransform.anchorMax = new Vector2(0.44f, 0.5f);
            toggleRectTransform.pivot = new Vector2(0f, 0.5f);
            toggleRectTransform.anchoredPosition = Vector2.zero;
            toggleRectTransform.sizeDelta = new Vector2(20f, 20f);

            TMP_Text label = FindText(row, "Label");

            if (label == null) {
                label = MakeText(row, "Label", labelText, 18f, false, TextAlignmentOptions.MidlineLeft);
            }

            label.text = labelText;
            Stretch((RectTransform)label.transform, 0f, 0.42f);

            Wire(toggle, toggle.onValueChanged, "onValueChanged");
            return toggle;
        }

        static Slider SliderRow(Transform parent, string name, string labelText, float minimumValue, float maximumValue, bool wholeNumbers) {
            RectTransform row = Row(parent, "Row_" + name, SliderH);
            Slider slider = row.GetComponentInChildren<Slider>(true);

            if (slider == null) {
                GameObject sliderGameObject = DefaultControls.CreateSlider(uiResources);
                sliderGameObject.name = "Slider";
                sliderGameObject.transform.SetParent(row, false);
                SetLayer(sliderGameObject.transform, uiLayer);
                slider = sliderGameObject.GetComponent<Slider>();
            }

            slider.minValue = minimumValue;
            slider.maxValue = maximumValue;
            slider.wholeNumbers = wholeNumbers;
            RectTransform sliderRectTransform = (RectTransform)slider.transform;
            sliderRectTransform.anchorMin = new Vector2(0.42f, 0.5f);
            sliderRectTransform.anchorMax = new Vector2(0.85f, 0.5f);
            sliderRectTransform.pivot = new Vector2(0.5f, 0.5f);
            sliderRectTransform.anchoredPosition = Vector2.zero;
            sliderRectTransform.sizeDelta = new Vector2(0f, 22f);

            TMP_Text label = FindText(row, "Label");

            if (label == null) {
                label = MakeText(row, "Label", labelText, 18f, false, TextAlignmentOptions.MidlineLeft);
            }

            label.text = labelText;
            Stretch((RectTransform)label.transform, 0f, 0.42f);

            TMP_Text value = FindText(row, "Value");

            if (value == null) {
                value = MakeText(row, "Value", "", 18f, false, TextAlignmentOptions.MidlineRight);
            }

            string valueText = slider.value.ToString("0.00");

            if (wholeNumbers) {
                valueText = slider.value.ToString("0");
            }

            value.text = valueText;
            Stretch((RectTransform)value.transform, 0.86f, 1f);
            sliderLabelList.Add(value);

            Wire(slider, slider.onValueChanged, "m_OnValueChanged");
            return slider;
        }

        static TMP_Dropdown DropdownRow(Transform parent, string name, string labelText, string[] options) {
            RectTransform row = Row(parent, "Row_" + name, DropdownH);
            TMP_Dropdown dropdown = row.GetComponentInChildren<TMP_Dropdown>(true);

            if (dropdown == null) {
                GameObject dropdownGameObject = TMP_DefaultControls.CreateDropdown(tmpResources);
                dropdownGameObject.name = "Dropdown";
                dropdownGameObject.transform.SetParent(row, false);
                SetLayer(dropdownGameObject.transform, uiLayer);
                dropdown = dropdownGameObject.GetComponent<TMP_Dropdown>();
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
            dropdown.RefreshShownValue();
            RectTransform dropdownRectTransform = (RectTransform)dropdown.transform;
            dropdownRectTransform.anchorMin = new Vector2(0.42f, 0.5f);
            dropdownRectTransform.anchorMax = new Vector2(1f, 0.5f);
            dropdownRectTransform.pivot = new Vector2(0.5f, 0.5f);
            dropdownRectTransform.anchoredPosition = Vector2.zero;
            dropdownRectTransform.sizeDelta = new Vector2(0f, 38f);

            TMP_Text label = FindText(row, "Label");

            if (label == null) {
                label = MakeText(row, "Label", labelText, 18f, false, TextAlignmentOptions.MidlineLeft);
            }

            label.text = labelText;
            Stretch((RectTransform)label.transform, 0f, 0.42f);

            Wire(dropdown, dropdown.onValueChanged, "m_OnValueChanged");
            return dropdown;
        }

        static void Wire(Object target, UnityEventBase unityEvent, string fieldName) {
            for (int index = 0; index < unityEvent.GetPersistentEventCount(); index++) {
                bool sameTarget = unityEvent.GetPersistentTarget(index) == udonBehaviour;
                bool sameMethod = unityEvent.GetPersistentMethodName(index) == "SendCustomEvent";

                if (sameTarget && sameMethod && SameStringArgument(target, fieldName, index)) {
                    return;
                }
            }

            UnityEventTools.AddStringPersistentListener(unityEvent, udonBehaviour.SendCustomEvent, "OnControlChanged");
        }

        // The persistent argument has no runtime getter.
        static bool SameStringArgument(Object target, string fieldName, int index) {
            SerializedProperty calls = new SerializedObject(target).FindProperty(fieldName + ".m_PersistentCalls.m_Calls");

            if (calls == null || index >= calls.arraySize) {
                return false;
            }

            SerializedProperty argument = calls.GetArrayElementAtIndex(index).FindPropertyRelative("m_Arguments.m_StringArgument");

            if (argument == null) {
                return false;
            }

            return argument.stringValue == "OnControlChanged";
        }

        static TMP_Text FindText(Transform row, string name) {
            Transform child = row.Find(name);
            TMP_Text result = null;

            if (child != null) {
                result = child.GetComponent<TMP_Text>();
            }

            return result;
        }

        static TMP_Text MakeText(Transform parent, string name, string content, float size, bool bold, TextAlignmentOptions alignment) {
            GameObject textGameObject = new GameObject(name, typeof(RectTransform));
            textGameObject.transform.SetParent(parent, false);
            textGameObject.layer = uiLayer;
            TextMeshProUGUI text = textGameObject.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            FontStyles fontStyle = FontStyles.Normal;

            if (bold) {
                fontStyle = FontStyles.Bold;
            }

            text.fontStyle = fontStyle;
            text.color = LabelColor;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        static void Stretch(RectTransform rectTransform, float minimumX, float maximumX) {
            rectTransform.anchorMin = new Vector2(minimumX, 0f);
            rectTransform.anchorMax = new Vector2(maximumX, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        static void SetLayer(Transform transform, int layer) {
            transform.gameObject.layer = layer;

            for (int index = 0; index < transform.childCount; index++) {
                SetLayer(transform.GetChild(index), layer);
            }
        }
    }
}
