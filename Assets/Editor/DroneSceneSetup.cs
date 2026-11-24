using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// One-time setup: builds Assets/Scenes/DroneScene.unity with a floor, some obstacles,
/// a light and the drone player. Run from menu: Tools > Drone > Build Drone Scene.
/// </summary>
public static class DroneSceneSetup
{
    const string MatDir = "Assets/Materials/Drone";
    const string ScenePath = "Assets/Scenes/DroneScene.unity";

    static Shader shader;

    [MenuItem("Tools/Drone/Build Drone Scene")]
    public static void Build()
    {
        shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "Drone");
        EnsureFolder("Assets", "Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Light
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.65f, 0.75f, 0.9f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.5f, 0.55f);
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.25f, 0.25f);

        // Environment
        var env = new GameObject("Environment").transform;
        var floorMat = Mat("Floor", new Color(0.35f, 0.42f, 0.38f), 0f, 0.2f);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(env);
        floor.transform.localScale = new Vector3(3f, 1f, 3f); // 30 x 30 units
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        Color[] pillarColors = {
            new Color(0.9f, 0.3f, 0.25f), new Color(0.25f, 0.6f, 0.95f),
            new Color(0.95f, 0.8f, 0.2f), new Color(0.4f, 0.85f, 0.45f)
        };
        var rand = new System.Random(7);
        for (int i = 0; i < 12; i++)
        {
            var p = GameObject.CreatePrimitive(i % 3 == 0 ? PrimitiveType.Cylinder : PrimitiveType.Cube);
            p.name = "Obstacle_" + i;
            p.transform.SetParent(env);
            float h = 1f + (float)rand.NextDouble() * 4f;
            float angle = i * 30f * Mathf.Deg2Rad;
            float radius = 5f + (float)rand.NextDouble() * 7f;
            p.transform.position = new Vector3(Mathf.Cos(angle) * radius, h * 0.5f, Mathf.Sin(angle) * radius);
            // Unity's cylinder is 2 units tall, so halve its Y scale.
            p.transform.localScale = i % 3 == 0 ? new Vector3(1f, h * 0.5f, 1f) : new Vector3(1f, h, 1f);
            p.GetComponent<Renderer>().sharedMaterial = Mat("Obstacle_" + (i % 4), pillarColors[i % 4], 0.1f, 0.5f);
        }

        // Drone
        var drone = BuildDrone(new Vector3(0f, 1f, 0f));

        // Camera following the drone
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.nearClipPlane = 0.03f;
        camGO.AddComponent<AudioListener>();
        camGO.transform.SetParent(drone.transform, false);
        camGO.transform.localPosition = new Vector3(0f, 0.25f, -0.7f);
        camGO.transform.localEulerAngles = new Vector3(15f, 0f, 0f);

        // Telemetry + flight HUD
        FlightHudSetup.Setup(drone);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[DroneSceneSetup] Built " + ScenePath);
    }

    static GameObject BuildDrone(Vector3 pos)
    {
        var body = Mat("Drone_Body", new Color(0.12f, 0.13f, 0.15f), 0.6f, 0.7f);
        var accent = Mat("Drone_Accent", new Color(1f, 0.45f, 0.05f), 0.2f, 0.8f);
        var metal = Mat("Drone_Metal", new Color(0.55f, 0.57f, 0.6f), 0.9f, 0.6f);
        var blade = Mat("Drone_Blade", new Color(0.05f, 0.05f, 0.05f), 0f, 0.4f);
        var lens = Mat("Drone_Lens", new Color(0.02f, 0.05f, 0.12f), 0.3f, 0.95f);
        var ledG = Mat("Drone_LED_Green", Color.green, 0f, 0.5f, new Color(0f, 3f, 0.5f));
        var ledR = Mat("Drone_LED_Red", Color.red, 0f, 0.5f, new Color(3f, 0f, 0f));

        var root = new GameObject("PlayerDrone");
        root.tag = "Player";
        root.transform.position = pos;
        var rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        var col = root.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.01f, 0f);
        col.size = new Vector3(0.32f, 0.08f, 0.32f);

        var model = new GameObject("Model").transform;
        model.SetParent(root.transform, false);

        Part(PrimitiveType.Cube, "Body", model, Vector3.zero, new Vector3(0.09f, 0.035f, 0.14f), Vector3.zero, body);
        Part(PrimitiveType.Sphere, "Canopy", model, new Vector3(0, 0.018f, 0.01f), new Vector3(0.075f, 0.035f, 0.11f), Vector3.zero, accent);
        Part(PrimitiveType.Cube, "Battery", model, new Vector3(0, 0.002f, -0.075f), new Vector3(0.06f, 0.025f, 0.03f), Vector3.zero, accent);
        Part(PrimitiveType.Sphere, "CameraLens", model, new Vector3(0, -0.012f, 0.072f), new Vector3(0.032f, 0.032f, 0.02f), Vector3.zero, lens);
        Part(PrimitiveType.Cube, "Arm_A", model, Vector3.zero, new Vector3(0.30f, 0.012f, 0.018f), new Vector3(0, 45, 0), body);
        Part(PrimitiveType.Cube, "Arm_B", model, Vector3.zero, new Vector3(0.30f, 0.012f, 0.018f), new Vector3(0, -45, 0), body);

        foreach (float x in new[] { -0.035f, 0.035f })
        {
            Part(PrimitiveType.Cube, "SkidLeg_F", model, new Vector3(x, -0.03f, 0.035f), new Vector3(0.006f, 0.03f, 0.006f), Vector3.zero, metal);
            Part(PrimitiveType.Cube, "SkidLeg_B", model, new Vector3(x, -0.03f, -0.035f), new Vector3(0.006f, 0.03f, 0.006f), Vector3.zero, metal);
            Part(PrimitiveType.Cylinder, "Skid", model, new Vector3(x, -0.045f, 0), new Vector3(0.007f, 0.06f, 0.007f), new Vector3(90, 0, 0), metal);
        }

        float d = 0.106f;
        Vector3[] corners = { new Vector3(-d, 0, d), new Vector3(d, 0, d), new Vector3(d, 0, -d), new Vector3(-d, 0, -d) };
        var rotors = new List<Transform>();
        for (int i = 0; i < 4; i++)
        {
            var c = corners[i];
            Part(PrimitiveType.Cylinder, "Motor_" + i, model, c + new Vector3(0, 0.012f, 0), new Vector3(0.028f, 0.012f, 0.028f), Vector3.zero, metal);
            var rotor = new GameObject("Rotor_" + i).transform;
            rotor.SetParent(model, false);
            rotor.localPosition = c + new Vector3(0, 0.028f, 0);
            Part(PrimitiveType.Cube, "Blade1", rotor, Vector3.zero, new Vector3(0.12f, 0.003f, 0.014f), new Vector3(0, 0, 4), blade);
            Part(PrimitiveType.Cube, "Blade2", rotor, Vector3.zero, new Vector3(0.12f, 0.003f, 0.014f), new Vector3(0, 90, -4), blade);
            Part(PrimitiveType.Cylinder, "Hub", rotor, Vector3.zero, new Vector3(0.012f, 0.006f, 0.012f), Vector3.zero, accent);
            rotors.Add(rotor);
            Part(PrimitiveType.Sphere, "LED_" + i, model, c + new Vector3(0, -0.004f, 0), Vector3.one * 0.012f, Vector3.zero, c.z > 0 ? ledG : ledR);
        }

        var ctrl = root.AddComponent<DroneController>();
        ctrl.model = model;
        ctrl.rotors = rotors.ToArray();
        return root;
    }

    static GameObject Part(PrimitiveType t, string name, Transform parent, Vector3 pos, Vector3 scale, Vector3 euler, Material m)
    {
        var go = GameObject.CreatePrimitive(t);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.transform.localEulerAngles = euler;
        go.GetComponent<Renderer>().sharedMaterial = m;
        return go;
    }

    static Material Mat(string name, Color c, float metal, float smooth, Color? emit = null)
    {
        string path = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", c);
        m.color = c;
        m.SetFloat("_Metallic", metal);
        m.SetFloat("_Smoothness", smooth);
        if (emit.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emit.Value);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }
}
