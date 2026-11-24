using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Basic flight HUD built with UI Toolkit (layout in Assets/UI/HUD/FlightHud.uxml).
/// It only reads <see cref="DroneTelemetry"/>; it never touches the controller.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class FlightHud : MonoBehaviour
{
    public DroneTelemetry telemetry;

    [Tooltip("How many times per second the readouts refresh.")]
    public float refreshRate = 10f;

    [Tooltip("Battery % at which the HUD warns, and at which it turns critical.")]
    public float lowBattery = 30f;
    public float criticalBattery = 15f;

    [Header("Scaling")]
    [Tooltip("Screen height the HUD is designed for. At this height the HUD is drawn at 1:1.")]
    public float referenceHeight = 1080f;

    [Tooltip("The HUD never shrinks below this, so text stays readable in small windows.")]
    public float minScale = 0.8f;

    [Tooltip("The HUD never grows beyond this on very large screens.")]
    public float maxScale = 2f;

    static readonly string[] Compass = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

    Label lat, lon, gpsAlt, heading, battPercent, battVoltage, flightTime;
    Label altAgl, altRel, vSpeed, gSpeed, homeDist, homeBearing, travelled, warning;
    VisualElement battFill;
    float nextRefresh;
    PanelSettings runtimePanel;
    int lastScreenHeight;

    void OnEnable()
    {
        var doc = GetComponent<UIDocument>();

        // Scale with a runtime copy of the panel settings so the asset on disk is never modified.
        if (runtimePanel == null && doc.panelSettings != null)
        {
            runtimePanel = Instantiate(doc.panelSettings);
            runtimePanel.name = doc.panelSettings.name + " (Runtime)";
            runtimePanel.scaleMode = PanelScaleMode.ConstantPixelSize;
            doc.panelSettings = runtimePanel;
        }
        lastScreenHeight = 0;

        var root = doc.rootVisualElement;
        lat = root.Q<Label>("gps-lat");
        lon = root.Q<Label>("gps-lon");
        gpsAlt = root.Q<Label>("gps-alt");
        heading = root.Q<Label>("heading");
        battPercent = root.Q<Label>("batt-percent");
        battVoltage = root.Q<Label>("batt-voltage");
        battFill = root.Q<VisualElement>("batt-fill");
        flightTime = root.Q<Label>("flight-time");
        altAgl = root.Q<Label>("alt-agl");
        altRel = root.Q<Label>("alt-rel");
        vSpeed = root.Q<Label>("vspeed");
        gSpeed = root.Q<Label>("gspeed");
        homeDist = root.Q<Label>("home-dist");
        homeBearing = root.Q<Label>("home-bearing");
        travelled = root.Q<Label>("travelled");
        warning = root.Q<Label>("warning");

        if (telemetry == null) telemetry = FindAnyObjectByType<DroneTelemetry>();
        nextRefresh = 0f;
    }

    void OnDestroy()
    {
        if (runtimePanel != null) Destroy(runtimePanel);
    }

    void UpdateScale()
    {
        if (runtimePanel == null || Screen.height == lastScreenHeight) return;
        lastScreenHeight = Screen.height;
        runtimePanel.scale = Mathf.Clamp(Screen.height / Mathf.Max(1f, referenceHeight), minScale, maxScale);
    }

    void Update()
    {
        UpdateScale();
        if (telemetry == null || lat == null) return;
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 1f / Mathf.Max(1f, refreshRate);

        TelemetryData d = telemetry.Data;

        lat.text = $"LAT  {d.gps.latitude:F6}°";
        lon.text = $"LON  {d.gps.longitude:F6}°";
        gpsAlt.text = $"MSL  {d.gps.altitudeMsl:F1} m";

        heading.text = $"{Mathf.RoundToInt(d.heading) % 360:000}° {CompassPoint(d.heading)}";

        altAgl.text = $"AGL  {d.altitudeAgl:F1} m";
        altRel.text = $"REL  {d.altitudeRelative:+0.0;-0.0;0.0} m";
        vSpeed.text = $"V/S  {d.verticalSpeed:+0.0;-0.0;0.0} m/s";

        gSpeed.text = $"GS   {d.groundSpeed:F1} m/s";
        homeDist.text = $"HOME {d.distanceToHome:F1} m";
        homeBearing.text = $"BRG  {Mathf.RoundToInt(d.bearingToHome) % 360:000}° {CompassPoint(d.bearingToHome)}";
        travelled.text = $"DIST {d.distanceTravelled:F0} m";

        int t = Mathf.FloorToInt(d.flightTime);
        flightTime.text = $"{t / 60:00}:{t % 60:00}";

        UpdateBattery(d.batteryPercent, d.batteryVoltage);
    }

    void UpdateBattery(float percent, float voltage)
    {
        battPercent.text = $"{percent:F0}%";
        battVoltage.text = $"{voltage:F1} V";
        battFill.style.width = Length.Percent(Mathf.Clamp(percent, 0f, 100f));

        bool critical = percent <= criticalBattery;
        bool low = !critical && percent <= lowBattery;
        battFill.EnableInClassList("batt-fill--low", low);
        battFill.EnableInClassList("batt-fill--critical", critical);

        if (critical) ShowWarning(percent <= 0f ? "BATTERY EMPTY" : "CRITICAL BATTERY - LAND NOW");
        else if (low) ShowWarning("LOW BATTERY");
        else warning.style.display = DisplayStyle.None;
    }

    void ShowWarning(string text)
    {
        warning.text = text;
        warning.style.display = DisplayStyle.Flex;
    }

    static string CompassPoint(float degrees)
    {
        int i = Mathf.RoundToInt(Mathf.Repeat(degrees, 360f) / 45f) % 8;
        return Compass[i];
    }
}
