// Save this file as Assets/Editor/BuildVerticalSlicePackage.cs
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BuildVerticalSlicePackage
{
    private const string packageName = "VerticalSlice_ErrorHandler.unitypackage";
    private static readonly string[] exportPaths = new[]
    {
        "Assets/Scripts/ErrorHandler",
        "Assets/Prefabs",
        "Assets/Scenes",
        "Assets/ScriptableObjects/Bugs",
        "Assets/Audio"
    };

    [MenuItem("Tools/Error Handler/Build Vertical Slice Package")]
    public static void BuildPackage()
    {
        // Ensure scripts folder exists (we expect your C# scripts already in Assets/Scripts/ErrorHandler)
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/ScriptableObjects/Bugs");
        Directory.CreateDirectory("Assets/Audio");

        // Create sample BugDefinition assets
        CreateBugDefinition("Gravity Inversion", "Inverts gravity for a short duration on target/global.", 20f, 0.4f, "ForceAnchor", BugCategory.Physics);
        CreateBugDefinition("Missing Texture", "Target becomes invisible until patched.", 18f, 0.2f, "SuperGlue", BugCategory.Rendering);
        CreateBugDefinition("AI Heart-Eyes", "NPC AI falls in love with the player instead of attacking.", 22f, 0.5f, "ScriptOverride", BugCategory.AI);

        // Create placeholder VFX prefabs
        CreateVFXPrefab("Assets/Prefabs/VFX/SpawnFX.prefab", Color.cyan);
        CreateVFXPrefab("Assets/Prefabs/VFX/ResolveFX.prefab", Color.green);
        CreateVFXPrefab("Assets/Prefabs/VFX/ExpireFX.prefab", Color.red);

        // Create placeholder audio stingers
        CreatePlaceholderAudio("Assets/Audio/stinger_initial.wav");
        CreatePlaceholderAudio("Assets/Audio/stinger_repeat.wav");
        CreatePlaceholderAudio("Assets/Audio/spawn.wav");
        CreatePlaceholderAudio("Assets/Audio/resolve.wav");
        CreatePlaceholderAudio("Assets/Audio/expire.wav");

        // Create minimal UI prefabs (bug list item and log entry)
        // Will try to use TextMeshPro if present; otherwise fall back to legacy Text.
        UICreator.CreateBugListItemPrefab();
        UICreator.CreateLogEntryPrefab();

        // Create demo scene and Managers object
        CreateDemoScene();

        // Save assets
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Export package
        var exportList = GetExistingExportPaths();
        if (exportList.Length == 0)
        {
            Debug.LogError("[BuildVerticalSlicePackage] Nothing found to export. Make sure there are assets in the expected folders.");
            return;
        }

        var exportPath = Path.Combine(Directory.GetCurrentDirectory(), packageName);
        AssetDatabase.ExportPackage(exportList, exportPath, ExportPackageOptions.Interactive | ExportPackageOptions.Recurse);
        Debug.Log($"[BuildVerticalSlicePackage] Exported package to: {exportPath}");
    }

    static string[] GetExistingExportPaths()
    {
        var list = new System.Collections.Generic.List<string>();
        foreach (var p in exportPaths)
            if (Directory.Exists(p) || AssetDatabase.GetAssetPathsFromAssetBundle(p).Length > 0)
                list.Add(p);
        // Also include scripts if present
        if (Directory.Exists("Assets/Scripts/ErrorHandler")) list.Add("Assets/Scripts/ErrorHandler");
        return list.ToArray();
    }

    static void CreateBugDefinition(string name, string desc, float duration, float difficulty, string preferredTool, BugCategory category)
    {
        var asset = ScriptableObject.CreateInstance<BugDefinition>();
        asset.bugName = name;
        asset.description = desc;
        asset.baseDuration = duration;
        asset.difficulty = difficulty;
        asset.spawnWeight = 1f;
        if (System.Enum.TryParse(preferredTool, out PreferredTool pt)) asset.preferredTool = pt;
        asset.category = category;
        var safeName = name.Replace(" ", "_").Replace(":", "").Replace("/", "_");
        var path = $"Assets/ScriptableObjects/Bugs/{safeName}.asset";
        AssetDatabase.CreateAsset(asset, path);
        Debug.Log($"Created BugDefinition asset at {path}");
    }

    static void CreateVFXPrefab(string path, Color color)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var go = new GameObject(Path.GetFileNameWithoutExtension(path));
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = color;
        main.duration = 0.8f;
        main.loop = false;
        ps.playOnAwake = false;
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"Created VFX prefab: {path}");
    }

    static void CreatePlaceholderAudio(string path)
    {
        var sampleRate = 44100;
        var samples = sampleRate / 10; // 0.1s of audio (silence)
        var clip = AudioClip.Create(Path.GetFileNameWithoutExtension(path), samples, 1, sampleRate, false);
        AssetDatabase.CreateAsset(clip, path);
        Debug.Log($"Created placeholder audio: {path}");
    }

    static void CreateDemoScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        scene.name = "VerticalSlice";

        // Managers object
        var managers = new GameObject("Managers");
        var bugManager = managers.AddComponent<BugManager>();
        bugManager.initialDelay = 1.5f;
        bugManager.spawnIntervalMin = 6f;
        bugManager.spawnIntervalMax = 12f;
        bugManager.maxActiveBugs = 4;

        // Attach handlers (these components require your scripts to exist)
        managers.AddComponent<GravityInversionHandler>();
        managers.AddComponent<MissingTextureHandler>();
        managers.AddComponent<AILogicFailureHandler>();

        // UI Manager (will need references set manually or via prefab)
        managers.AddComponent<UIManager_uGUI_TMP>();
        managers.AddComponent<VFXSpawner>();
        var audioMgr = managers.AddComponent<AudioCueManager>();
        var src = managers.gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;

        // Create a sample vulnerable cube target
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Cube_Target";
        cube.transform.position = Vector3.up * 1f;
        cube.AddComponent<Rigidbody>();
        cube.tag = "Vulnerable";

        // Anchor cube
        var anchor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        anchor.name = "Cube_Anchor";
        anchor.transform.position = new Vector3(1.2f, 1f, 0f);
        var rb = anchor.AddComponent<Rigidbody>();
        rb.isKinematic = true;

        // NPC
        var npcGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npcGO.name = "NPC_Test";
        npcGO.transform.position = new Vector3(-1.5f, 1f, 0f);
        npcGO.AddComponent<NPCBehavior>();
        npcGO.tag = "Vulnerable";

        // Camera / ToolUser
        var camGO = new GameObject("PlayerCamera");
        var cam = camGO.AddComponent<Camera>();
        cam.transform.position = new Vector3(0f, 2f, -6f);
        cam.transform.LookAt(Vector3.zero);
        camGO.tag = "Player";
        var toolUser = camGO.AddComponent<ToolUser>();
        toolUser.playerCamera = cam;
        // create a simple SuperGlue tool and assign it (so player can test)
        var toolHolder = new GameObject("SuperGlue_Tool");
        var glue = toolHolder.AddComponent<SuperGlueTool>();
        glue.toolName = "SuperGlue";
        toolUser.equippedTool = glue;
        toolHolder.transform.SetParent(camGO.transform, false);

        // Save scene
        var scenePath = "Assets/Scenes/VerticalSlice.unity";
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"Created demo scene at {scenePath}");
    }

    // Small helper to create basic UI prefabs. We try to use TMP if present.
    static class UICreator
    {
        public static void CreateBugListItemPrefab()
        {
            Directory.CreateDirectory("Assets/Prefabs/UI");
            var root = new GameObject("BugListItem_TMP");
            var rect = root.AddComponent<RectTransform>();
            var hl = root.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hl.childControlHeight = true;
            hl.childControlWidth = false;
            hl.spacing = 6f;

            // Progress background + fill
            var progressBack = CreateUIObject("ProgressBack", root.transform);
            var imgBack = progressBack.AddComponent<UnityEngine.UI.Image>();
            imgBack.color = new Color(0f,0f,0f,0.25f);
            var fillObj = CreateUIObject("Fill", progressBack.transform);
            var imgFill = fillObj.AddComponent<UnityEngine.UI.Image>();
            imgFill.type = UnityEngine.UI.Image.Type.Filled;
            imgFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            imgFill.fillOrigin = (int)UnityEngine.UI.Image.OriginHorizontal.Left;
            imgFill.fillAmount = 0f;
            imgFill.color = Color.green;

            // Name text (try TMP)
            bool hasTMP = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro") != null;
            GameObject nameGO, targetGO, timerGO;
            if (hasTMP)
            {
                var t1 = CreateTextMeshPro("NameTMP", root.transform, "Bug Name");
                nameGO = t1;
                var t2 = CreateTextMeshPro("TargetTMP", root.transform, "Target");
                targetGO = t2;
                var t3 = CreateTextMeshPro("TimerTMP", root.transform, "0.0s");
                timerGO = t3;
            }
            else
            {
                nameGO = CreateLegacyText("NameText", root.transform, "Bug Name");
                targetGO = CreateLegacyText("TargetText", root.transform, "Target");
                timerGO = CreateLegacyText("TimerText", root.transform, "0.0s");
            }

            // Buttons
            var assignBtn = CreateButton("Assign", root.transform, "Assign");
            var resolveBtn = CreateButton("Resolve", root.transform, "Resolve");

            // Add BugListItem_TMP component and wire references (some fields won't be serialized until prefab)
            var item = root.AddComponent<BugListItem_TMP>();
            // We will assign references after prefab creation via means below.

            var prefabPath = "Assets/Prefabs/UI/BugListItem_TMP.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            // After prefab saved, open it and assign references by name
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (go != null)
            {
                var inst = PrefabUtility.LoadPrefabContents(prefabPath);
                var comp = inst.GetComponent<BugListItem_TMP>();
                comp.progressBar = inst.transform.Find("ProgressBack/Fill").GetComponent<UnityEngine.UI.Image>();

                // find TMP or legacy texts
                var nt = inst.transform.Find("NameTMP") ?? inst.transform.Find("NameText");
                var targ = inst.transform.Find("TargetTMP") ?? inst.transform.Find("TargetText");
                var tim = inst.transform.Find("TimerTMP") ?? inst.transform.Find("TimerText");

                if (nt != null)
                {
                    var tmp = nt.GetComponent<TMPro.TextMeshProUGUI>();
                    if (tmp != null) comp.nameText = tmp;
                    else comp.nameText = nt.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                }
                if (targ != null)
                {
                    var tmp = targ.GetComponent<TMPro.TextMeshProUGUI>();
                    if (tmp != null) comp.targetText = tmp;
                }
                if (tim != null)
                {
                    var tmp = tim.GetComponent<TMPro.TextMeshProUGUI>();
                    if (tmp != null) comp.timerText = tmp;
                }

                // buttons
                var ab = inst.transform.Find("Assign");
                if (ab != null) comp.assignButton = ab.GetComponent<UnityEngine.UI.Button>();
                var rb = inst.transform.Find("Resolve");
                if (rb != null) comp.resolveButton = rb.GetComponent<UnityEngine.UI.Button>();

                // Add LowTimeWarningFX
                var fx = inst.AddComponent<LowTimeWarningFX>();
                var border = CreateUIObject("Border", inst.transform);
                var borderImg = border.AddComponent<UnityEngine.UI.Image>();
                borderImg.color = new Color(1f,1f,1f,0.12f);
                border.transform.SetAsFirstSibling();
                fx.borderImage = borderImg;
                fx.initialStinger = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/stinger_initial.wav");
                fx.repeatingStinger = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/stinger_repeat.wav");

                PrefabUtility.SaveAsPrefabAsset(inst, prefabPath);
                PrefabUtility.UnloadPrefabContents(inst);
                Debug.Log($"Created UI prefab {prefabPath}");
            }
        }

        public static void CreateLogEntryPrefab()
        {
            Directory.CreateDirectory("Assets/Prefabs/UI");
            var go = new GameObject("LogEntry_TMP");
            var rect = go.AddComponent<RectTransform>();
            bool hasTMP = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro") != null;
            if (hasTMP)
            {
                var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
                tmp.text = "00:00:00 - Log entry";
                tmp.fontSize = 18;
            }
            else
            {
                var t = go.AddComponent<UnityEngine.UI.Text>();
                t.text = "00:00:00 - Log entry";
                t.fontSize = 18;
            }

            var prefabPath = "Assets/Prefabs/UI/LogEntry_TMP.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"Created log entry prefab {prefabPath}");
        }

        static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            return go;
        }

        static GameObject CreateLegacyText(string name, Transform parent, string text)
        {
            var go = CreateUIObject(name, parent);
            var t = go.AddComponent<UnityEngine.UI.Text>();
            t.text = text;
            t.color = Color.white;
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return go;
        }

        static GameObject CreateTextMeshPro(string name, Transform parent, string text)
        {
            var go = CreateUIObject(name, parent);
            var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            return go;
        }

        static GameObject CreateButton(string name, Transform parent, string label)
        {
            var go = CreateUIObject(name, parent);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.2f,0.2f,0.2f,0.9f);
            var btn = go.AddComponent<UnityEngine.UI.Button>();
            var txtGO = CreateUIObject("Label", go.transform);
            var hasTMP = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro") != null;
            if (hasTMP)
            {
                var tmp = txtGO.AddComponent<TMPro.TextMeshProUGUI>();
                tmp.text = label;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
            }
            else
            {
                var t = txtGO.AddComponent<UnityEngine.UI.Text>();
                t.text = label;
                t.alignment = TextAnchor.MiddleCenter;
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return go;
        }
    }
}
#endif
