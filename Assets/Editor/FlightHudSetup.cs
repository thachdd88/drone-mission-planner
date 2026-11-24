using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Adds telemetry and the flight HUD to the open scene.
/// Menu: Tools > Drone > Add Flight HUD. Also called by DroneSceneSetup.Build().
/// Safe to run more than once: it reuses what is already there.
/// </summary>
public static class FlightHudSetup
{
    const string HudDir = "Assets/UI/HUD";
    const string UxmlPath = HudDir + "/FlightHud.uxml";
    const string ThemePath = HudDir + "/HudTheme.tss";
    const string PanelSettingsPath = HudDir + "/HudPanelSettings.asset";

    [MenuItem("Tools/Drone/Add Flight HUD")]
    public static void AddToOpenScene()
    {
        var drone = GameObject.Find("PlayerDrone");
        if (drone == null)
        {
            Debug.LogError("[FlightHudSetup] No 'PlayerDrone' in the open scene. Open DroneScene first.");
            return;
        }
        Setup(drone);
        EditorSceneManager.MarkSceneDirty(drone.scene);
        EditorSceneManager.SaveScene(drone.scene);
        Debug.Log("[FlightHudSetup] Flight HUD added to " + drone.scene.path);
    }

    public static void Setup(GameObject drone)
    {
        var telemetry = drone.GetComponent<DroneTelemetry>();
        if (telemetry == null) telemetry = Undo.AddComponent<DroneTelemetry>(drone);

        var hudGO = GameObject.Find("FlightHUD");
        if (hudGO == null)
        {
            hudGO = new GameObject("FlightHUD");
            Undo.RegisterCreatedObjectUndo(hudGO, "Create Flight HUD");
        }

        var doc = hudGO.GetComponent<UIDocument>();
        if (doc == null) doc = hudGO.AddComponent<UIDocument>();
        doc.panelSettings = GetOrCreatePanelSettings();
        doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
        if (doc.visualTreeAsset == null) Debug.LogError("[FlightHudSetup] Missing " + UxmlPath);

        var hud = hudGO.GetComponent<FlightHud>();
        if (hud == null) hud = hudGO.AddComponent<FlightHud>();
        hud.telemetry = telemetry;

        EditorUtility.SetDirty(hudGO);
        EditorUtility.SetDirty(drone);
    }

    static PanelSettings GetOrCreatePanelSettings()
    {
        var ps = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
        if (ps != null) return ps;

        ps = ScriptableObject.CreateInstance<PanelSettings>();
        ps.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
        if (ps.themeStyleSheet == null) Debug.LogError("[FlightHudSetup] Missing " + ThemePath);
        ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        ps.referenceResolution = new Vector2Int(1920, 1080);
        ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        ps.match = 0.5f;
        AssetDatabase.CreateAsset(ps, PanelSettingsPath);
        AssetDatabase.SaveAssets();
        return ps;
    }
}
