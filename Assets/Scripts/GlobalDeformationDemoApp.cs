using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GlobalDeformationDemo
{
    public sealed class GlobalDeformationDemoApp : MonoBehaviour
    {
        public MeshDeformer Deformer { get; private set; }
        public FfdLatticeView LatticeView { get; private set; }

        private const float PanelWidth = 390f;
        private readonly Color panelColor = new Color(0.035f, 0.055f, 0.08f, 0.97f);
        private readonly Color accentColor = new Color(0.05f, 0.72f, 0.88f, 1f);
        private readonly Color buttonColor = new Color(0.09f, 0.18f, 0.25f, 1f);
        private readonly Color textColor = new Color(0.91f, 0.96f, 1f, 1f);

        private Camera sceneCamera;
        private Transform modelTransform;
        private OrbitCamera orbitCamera;
        private TMP_InputField pathInput;
        private TMP_Text statusText;
        private Slider taperSlider;
        private Slider twistSlider;
        private Slider bendSlider;
        private Slider latticeSlider;
        private TMP_Text taperValue;
        private TMP_Text twistValue;
        private TMP_Text bendValue;
        private TMP_Text latticeValue;
        private Coroutine autoDemoRoutine;

        private void Start()
        {
            BuildEnvironment();
            BuildInterface();
            LoadSampleObj();
        }

        private void BuildEnvironment()
        {
            sceneCamera = new GameObject("Main Camera").AddComponent<Camera>();
            sceneCamera.tag = "MainCamera";
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = new Color(0.015f, 0.025f, 0.04f);
            sceneCamera.fieldOfView = 48f;
            sceneCamera.nearClipPlane = 0.05f;
            orbitCamera = sceneCamera.gameObject.AddComponent<OrbitCamera>();

            GameObject keyLight = new GameObject("Key Light");
            Light key = keyLight.AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.15f;
            key.color = new Color(0.82f, 0.9f, 1f);
            keyLight.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            GameObject fillLight = new GameObject("Fill Light");
            Light fill = fillLight.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.55f;
            fill.color = new Color(1f, 0.55f, 0.3f);
            fillLight.transform.rotation = Quaternion.Euler(25f, 145f, 0f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0f, -0.05f, 0f);
            ground.transform.localScale = new Vector3(2.2f, 1f, 2.2f);
            Material groundMaterial = new Material(Shader.Find("Standard"));
            groundMaterial.name = "Ground Material";
            groundMaterial.color = new Color(0.055f, 0.075f, 0.1f);
            groundMaterial.SetFloat("_Glossiness", 0.12f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;

            GameObject model = new GameObject("Editable OBJ Model");
            modelTransform = model.transform;
            model.AddComponent<MeshFilter>();
            MeshRenderer renderer = model.AddComponent<MeshRenderer>();
            Material modelMaterial = new Material(Shader.Find("Standard"));
            modelMaterial.name = "Model Material";
            modelMaterial.color = new Color(0.08f, 0.58f, 0.86f);
            modelMaterial.SetFloat("_Metallic", 0.18f);
            modelMaterial.SetFloat("_Glossiness", 0.58f);
            renderer.sharedMaterial = modelMaterial;
            Deformer = model.AddComponent<MeshDeformer>();
            LatticeView = model.AddComponent<FfdLatticeView>();
            LatticeView.Initialize(Deformer, sceneCamera);
        }

        private void BuildInterface()
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();

            GameObject canvasObject = new GameObject("Control Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = sceneCamera;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject panel = CreateUiObject("Control Panel", canvasObject.transform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(18f, 0f);
            panelRect.sizeDelta = new Vector2(PanelWidth, -36f);
            UnityEngine.UI.Image panelImage = panel.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = panelColor;

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 18, 18);
            layout.spacing = 7f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateLabel(panel.transform, "GLOBAL DEFORMATION DEMO", 25f, FontStyles.Bold, accentColor, 40f);
            CreateLabel(panel.transform, "Class 24DM1  |  Huang Jianwen  |  202411581361", 14f, FontStyles.Normal, textColor, 25f);
            CreateLabel(panel.transform, "OBJ IMPORT · TAPER · TWIST · BEND · FFD", 12f, FontStyles.Normal, new Color(0.55f, 0.72f, 0.82f), 22f);

            pathInput = CreateInputField(panel.transform, "Full OBJ path, e.g. D:\\model.obj");
            GameObject loadRow = CreateHorizontalRow(panel.transform, 42f);
            CreateButton(loadRow.transform, "LOAD SAMPLE OBJ", LoadSampleObj);
            CreateButton(loadRow.transform, "LOAD PATH", LoadObjFromInput);

            GameObject modeRow = CreateHorizontalRow(panel.transform, 42f);
            CreateButton(modeRow.transform, "RESET", ResetModel);
            CreateButton(modeRow.transform, "TAPER", ApplyTaperFromUi);
            CreateButton(modeRow.transform, "TWIST", ApplyTwistFromUi);
            CreateButton(modeRow.transform, "BEND", ApplyBendFromUi);

            taperSlider = CreateSlider(panel.transform, "TAPER", -0.85f, 1.4f, 0.65f, out taperValue);
            twistSlider = CreateSlider(panel.transform, "TWIST", -360f, 360f, 180f, out twistValue);
            bendSlider = CreateSlider(panel.transform, "BEND", -170f, 170f, 80f, out bendValue);
            latticeSlider = CreateSlider(panel.transform, "FFD N", 2f, 5f, 3f, out latticeValue, true);

            taperSlider.onValueChanged.AddListener(value =>
            {
                taperValue.text = value.ToString("0.00");
                if (Deformer != null && Deformer.Mode == DeformationMode.Taper) ApplyTaper(value);
            });
            twistSlider.onValueChanged.AddListener(value =>
            {
                twistValue.text = value.ToString("0") + " deg";
                if (Deformer != null && Deformer.Mode == DeformationMode.Twist) ApplyTwist(value);
            });
            bendSlider.onValueChanged.AddListener(value =>
            {
                bendValue.text = value.ToString("0") + " deg";
                if (Deformer != null && Deformer.Mode == DeformationMode.Bend) ApplyBend(value);
            });
            latticeSlider.onValueChanged.AddListener(value => latticeValue.text = Mathf.RoundToInt(value) + "x" + Mathf.RoundToInt(value) + "x" + Mathf.RoundToInt(value));
            taperValue.text = taperSlider.value.ToString("0.00");
            twistValue.text = twistSlider.value.ToString("0") + " deg";
            bendValue.text = bendSlider.value.ToString("0") + " deg";
            latticeValue.text = "3x3x3";

            GameObject ffdRow = CreateHorizontalRow(panel.transform, 42f);
            CreateButton(ffdRow.transform, "ENTER FFD", EnterFfd);
            CreateButton(ffdRow.transform, "REBUILD", RebuildFfd);
            CreateButton(ffdRow.transform, "AUTO DEMO", PlayAutoDemo);

            statusText = CreateLabel(panel.transform, "Loading...", 14f, FontStyles.Normal, textColor, 76f);
            statusText.alignment = TextAlignmentOptions.TopLeft;
            CreateLabel(
                panel.transform,
                "Right-drag: orbit  |  Wheel: zoom\nFFD mode: left-drag orange lattice points. Every deformation starts from the original vertices.",
                12.5f,
                FontStyles.Normal,
                new Color(0.66f, 0.77f, 0.84f),
                62f).alignment = TextAlignmentOptions.TopLeft;
        }

        public void LoadSampleObj()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "DeformationSample.obj");
            LoadObj(path, "Sample OBJ");
            if (pathInput != null) pathInput.text = path;
        }

        public void LoadObjFromInput()
        {
            LoadObj(pathInput == null ? string.Empty : pathInput.text.Trim(), "External OBJ");
        }

        public void LoadObj(string path, string sourceLabel)
        {
            try
            {
                Mesh mesh = RuntimeObjLoader.LoadFromFile(path);
                Deformer.SetMesh(mesh);
                Destroy(mesh);
                FitModelToView();
                LatticeView.Rebuild();
                LatticeView.SetVisible(false);
                SetStatus($"Loaded: {sourceLabel}\nVertices: {Deformer.Mesh.vertexCount}   Triangles: {Deformer.Mesh.triangles.Length / 3}\nMode: ORIGINAL");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SetStatus("Load failed: " + exception.Message);
            }
        }

        public void ResetModel()
        {
            if (!Ready()) return;
            Deformer.ResetMesh();
            Deformer.BuildLattice(Mathf.RoundToInt(latticeSlider.value));
            LatticeView.SetVisible(false);
            SetModeStatus("ORIGINAL");
        }

        public void ApplyTaper(float value)
        {
            if (!Ready()) return;
            Deformer.ApplyTaper(value);
            LatticeView.SetVisible(false);
            SetModeStatus($"TAPER  amount={value:0.00}");
        }

        public void ApplyTwist(float value)
        {
            if (!Ready()) return;
            Deformer.ApplyTwist(value);
            LatticeView.SetVisible(false);
            SetModeStatus($"TWIST  angle={value:0} deg");
        }

        public void ApplyBend(float value)
        {
            if (!Ready()) return;
            Deformer.ApplyBend(value);
            LatticeView.SetVisible(false);
            SetModeStatus($"BEND  angle={value:0} deg");
        }

        public void EnterFfd()
        {
            if (!Ready()) return;
            Deformer.EnterFreeFormMode();
            LatticeView.SetVisible(true);
            int n = Deformer.LatticeResolution;
            SetModeStatus($"FFD {n}x{n}x{n} - drag orange points");
        }

        public void RebuildFfd()
        {
            if (!Ready()) return;
            int n = Mathf.RoundToInt(latticeSlider.value);
            Deformer.BuildLattice(n);
            Deformer.EnterFreeFormMode();
            LatticeView.SetVisible(true);
            SetModeStatus($"FFD {n}x{n}x{n} lattice rebuilt");
        }

        public void SetFfdPoint(int index, Vector3 position)
        {
            if (!Ready()) return;
            Deformer.SetLatticePoint(index, position);
        }

        public void PlayAutoDemo()
        {
            if (autoDemoRoutine != null) StopCoroutine(autoDemoRoutine);
            autoDemoRoutine = StartCoroutine(AutoDemo());
        }

        private IEnumerator AutoDemo()
        {
            ResetModel();
            yield return new WaitForSeconds(1.2f);
            yield return AnimateValue(0f, 0.9f, 1.8f, ApplyTaper, taperSlider);
            yield return new WaitForSeconds(0.8f);
            yield return AnimateValue(0f, 250f, 2f, ApplyTwist, twistSlider);
            yield return new WaitForSeconds(0.8f);
            yield return AnimateValue(0f, 105f, 2f, ApplyBend, bendSlider);
            yield return new WaitForSeconds(0.8f);
            RebuildFfd();
            yield return new WaitForSeconds(0.6f);

            int n = Deformer.LatticeResolution;
            int index = DeformationMath.Index(n - 1, n - 1, n - 1, n);
            Vector3 start = Deformer.LatticePoints[index];
            Vector3 end = start + new Vector3(Deformer.OriginalBounds.size.x * 0.55f, Deformer.OriginalBounds.size.y * 0.25f, 0f);
            float elapsed = 0f;
            while (elapsed < 2.2f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 2.2f));
                SetFfdPoint(index, Vector3.Lerp(start, end, t));
                LatticeView.SyncFromDeformer();
                LatticeView.SetVisible(true);
                yield return null;
            }
            SetModeStatus("FFD: top lattice point moved");
            autoDemoRoutine = null;
        }

        private IEnumerator AnimateValue(float from, float to, float duration, Action<float> apply, Slider slider)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float value = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration)));
                slider.SetValueWithoutNotify(value);
                apply(value);
                yield return null;
            }
        }

        private void ApplyTaperFromUi() => ApplyTaper(taperSlider.value);
        private void ApplyTwistFromUi() => ApplyTwist(twistSlider.value);
        private void ApplyBendFromUi() => ApplyBend(bendSlider.value);

        private void FitModelToView()
        {
            Bounds bounds = Deformer.OriginalBounds;
            float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float scale = largest > 0.0001f ? 3.4f / largest : 1f;
            modelTransform.localScale = Vector3.one * scale;
            Vector3 targetCenter = new Vector3(1.25f, 1.75f, 0f);
            modelTransform.position = targetCenter - bounds.center * scale;
            orbitCamera.Target = targetCenter;
            orbitCamera.Distance = 6.3f;
        }

        private bool Ready()
        {
            if (Deformer != null && Deformer.Mesh != null) return true;
            SetStatus("Load an OBJ model first.");
            return false;
        }

        private void SetModeStatus(string mode)
        {
            SetStatus($"Mesh: {Deformer.Mesh.name}\nVertices: {Deformer.Mesh.vertexCount}   Triangles: {Deformer.Mesh.triangles.Length / 3}\nMode: {mode}");
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        private GameObject CreateHorizontalRow(Transform parent, float height)
        {
            GameObject row = CreateUiObject("Row", parent);
            LayoutElement element = row.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return row;
        }

        private Button CreateButton(Transform parent, string label, Action callback)
        {
            GameObject buttonObject = CreateUiObject(label + " Button", parent);
            UnityEngine.UI.Image image = buttonObject.AddComponent<UnityEngine.UI.Image>();
            image.color = buttonColor;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            colors.highlightedColor = new Color(0.08f, 0.52f, 0.68f);
            colors.pressedColor = accentColor;
            button.colors = colors;
            button.onClick.AddListener(() => callback());
            TMP_Text text = CreateLabel(buttonObject.transform, label, 13f, FontStyles.Bold, textColor, 0f);
            Stretch(text.rectTransform, 4f);
            text.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private Slider CreateSlider(
            Transform parent,
            string label,
            float minimum,
            float maximum,
            float initial,
            out TMP_Text valueLabel,
            bool wholeNumbers = false)
        {
            GameObject row = CreateHorizontalRow(parent, 34f);
            TMP_Text caption = CreateLabel(row.transform, label, 13f, FontStyles.Normal, textColor, 0f);
            caption.GetComponent<LayoutElement>().preferredWidth = 82f;

            GameObject sliderObject = CreateUiObject(label + " Slider", row.transform);
            sliderObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = minimum;
            slider.maxValue = maximum;
            slider.value = initial;
            slider.wholeNumbers = wholeNumbers;

            GameObject background = CreateUiObject("Background", sliderObject.transform);
            UnityEngine.UI.Image backgroundImage = background.AddComponent<UnityEngine.UI.Image>();
            backgroundImage.color = new Color(0.12f, 0.18f, 0.23f);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.35f);
            backgroundRect.anchorMax = new Vector2(1f, 0.65f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject fillArea = CreateUiObject("Fill Area", sliderObject.transform);
            Stretch(fillArea.GetComponent<RectTransform>(), 7f);
            GameObject fill = CreateUiObject("Fill", fillArea.transform);
            UnityEngine.UI.Image fillImage = fill.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = accentColor;
            Stretch(fill.GetComponent<RectTransform>(), 0f);
            slider.fillRect = fill.GetComponent<RectTransform>();

            GameObject handleArea = CreateUiObject("Handle Slide Area", sliderObject.transform);
            Stretch(handleArea.GetComponent<RectTransform>(), 8f);
            GameObject handle = CreateUiObject("Handle", handleArea.transform);
            UnityEngine.UI.Image handleImage = handle.AddComponent<UnityEngine.UI.Image>();
            handleImage.color = Color.white;
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(14f, 22f);
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            valueLabel = CreateLabel(row.transform, string.Empty, 12.5f, FontStyles.Normal, accentColor, 0f);
            valueLabel.alignment = TextAlignmentOptions.MidlineRight;
            valueLabel.GetComponent<LayoutElement>().preferredWidth = 66f;
            return slider;
        }

        private TMP_InputField CreateInputField(Transform parent, string placeholderText)
        {
            GameObject root = CreateUiObject("OBJ Path Input", parent);
            root.AddComponent<LayoutElement>().preferredHeight = 39f;
            UnityEngine.UI.Image background = root.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.075f, 0.105f, 0.14f);
            TMP_InputField input = root.AddComponent<TMP_InputField>();

            GameObject textArea = CreateUiObject("Text Area", root.transform);
            Stretch(textArea.GetComponent<RectTransform>(), 10f);
            textArea.AddComponent<RectMask2D>();
            TMP_Text placeholder = CreateLabel(textArea.transform, placeholderText, 12f, FontStyles.Italic, new Color(0.45f, 0.56f, 0.63f), 0f);
            Stretch(placeholder.rectTransform, 0f);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;
            TMP_Text text = CreateLabel(textArea.transform, string.Empty, 12f, FontStyles.Normal, textColor, 0f);
            Stretch(text.rectTransform, 0f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            input.textViewport = textArea.GetComponent<RectTransform>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            return input;
        }

        private TMP_Text CreateLabel(
            Transform parent,
            string value,
            float fontSize,
            FontStyles style,
            Color color,
            float height)
        {
            GameObject textObject = CreateUiObject("Label", parent);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            LayoutElement element = textObject.AddComponent<LayoutElement>();
            if (height > 0f) element.preferredHeight = height;
            return text;
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            GameObject result = new GameObject(name, typeof(RectTransform));
            result.transform.SetParent(parent, false);
            return result;
        }

        private static void Stretch(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }
    }
}
