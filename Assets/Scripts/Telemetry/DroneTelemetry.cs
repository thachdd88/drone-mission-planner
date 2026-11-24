using UnityEngine;

/// <summary>One snapshot of the drone's state. All UI reads this instead of the controller.</summary>
public struct TelemetryData
{
    // Position
    public Vector3 worldPosition;
    public GeoCoordinate gps;
    public float altitudeAgl;        // metres above whatever is directly below
    public float altitudeRelative;   // metres above the home point

    // Motion
    public float groundSpeed;        // horizontal speed, m/s
    public float verticalSpeed;      // m/s, positive = climbing
    public float heading;            // degrees, 0 = north, clockwise
    public float pitch;              // degrees, positive = nose up
    public float roll;               // degrees, positive = right side down

    // Navigation
    public float distanceToHome;     // horizontal distance, metres
    public float bearingToHome;      // degrees, 0 = north
    public float distanceTravelled;  // metres since start
    public float flightTime;         // seconds since start

    // Power
    public float batteryPercent;     // 0..100
    public float batteryVoltage;     // volts
}

/// <summary>
/// Computes the drone's flight data every physics step and exposes it as <see cref="Data"/>.
/// Put it on the same GameObject as <see cref="DroneController"/>.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class DroneTelemetry : MonoBehaviour
{
    [Header("GPS")]
    public GeoReference geoReference = new GeoReference();

    [Header("Simulated battery")]
    [Tooltip("Minutes of flight on a full battery while hovering.")]
    public float hoverEnduranceMinutes = 20f;

    [Tooltip("Extra drain at full horizontal speed, as a fraction of hover drain (0.5 = 50% more).")]
    public float speedDrainFactor = 0.5f;

    [Tooltip("Extra drain while climbing at full climb speed.")]
    public float climbDrainFactor = 0.8f;

    [Tooltip("Battery voltage at 100% and 0% (default: 4S LiPo).")]
    public float fullVoltage = 16.8f;
    public float emptyVoltage = 14.0f;

    [Header("Altitude sensor")]
    [Tooltip("Max range of the downward altitude sensor (metres).")]
    public float aglRange = 100f;

    public TelemetryData Data => data;
    public Vector3 HomePosition => home;

    TelemetryData data;
    Rigidbody rb;
    DroneController controller;
    Vector3 home;
    Vector3 lastPosition;
    float battery = 100f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<DroneController>();
    }

    void Start()
    {
        home = transform.position;
        lastPosition = home;
        Sample(0f);
    }

    void FixedUpdate() => Sample(Time.fixedDeltaTime);

    void Sample(float dt)
    {
        Vector3 pos = transform.position;
        Vector3 vel = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(vel.x, 0f, vel.z);

        data.worldPosition = pos;
        data.gps = geoReference.ToGeo(pos);
        data.altitudeRelative = pos.y - home.y;
        data.altitudeAgl = MeasureAgl(pos);

        data.groundSpeed = horizontalVel.magnitude;
        data.verticalSpeed = vel.y;
        data.heading = CompassAngle(transform.forward);

        // The physics body never tilts; the visual model does. Report the model's attitude.
        if (controller != null && controller.model != null)
        {
            Vector3 e = controller.model.localEulerAngles;
            data.pitch = -Mathf.DeltaAngle(0f, e.x);
            data.roll = -Mathf.DeltaAngle(0f, e.z);
        }

        Vector3 toHome = home - pos;
        toHome.y = 0f;
        data.distanceToHome = toHome.magnitude;
        data.bearingToHome = toHome.sqrMagnitude > 0.0001f ? CompassAngle(toHome) : 0f;

        data.distanceTravelled += Vector3.Distance(pos, lastPosition);
        lastPosition = pos;
        data.flightTime += dt;

        DrainBattery(dt, horizontalVel.magnitude, vel.y);
        data.batteryPercent = battery;
        data.batteryVoltage = Mathf.Lerp(emptyVoltage, fullVoltage, battery / 100f);
    }

    float MeasureAgl(Vector3 pos)
    {
        // Rays that start inside a collider don't hit it, so the drone's own collider is ignored.
        if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, aglRange, ~0, QueryTriggerInteraction.Ignore))
            return hit.distance;
        return pos.y; // nothing below within range: fall back to height above world Y = 0
    }

    void DrainBattery(float dt, float hSpeed, float vSpeed)
    {
        if (dt <= 0f || hoverEnduranceMinutes <= 0f) return;

        float maxSpeed = controller != null ? Mathf.Max(0.01f, controller.speed) : 1f;
        float maxClimb = controller != null ? Mathf.Max(0.01f, controller.climbSpeed) : 1f;

        float load = 1f
            + speedDrainFactor * Mathf.Clamp01(hSpeed / maxSpeed)
            + climbDrainFactor * Mathf.Clamp01(vSpeed / maxClimb);

        float hoverDrainPerSecond = 100f / (hoverEnduranceMinutes * 60f);
        battery = Mathf.Max(0f, battery - hoverDrainPerSecond * load * dt);
    }

    /// <summary>Degrees clockwise from north (+Z) for a world direction.</summary>
    static float CompassAngle(Vector3 dir)
    {
        float a = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        return a < 0f ? a + 360f : a;
    }

    /// <summary>Puts the battery back to full (e.g. after landing at home).</summary>
    public void ResetBattery() => battery = 100f;
}
