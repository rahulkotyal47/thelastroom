using UnityEngine;

// The only object the scene needs. It builds the room, player, entity, audio and UI at startup.
public class GameBootstrap : MonoBehaviour
{
    // Keeps shader variants (emission, fog) in player builds. Filled by the editor builder.
    public Material[] variantKeepers;

    public static void ApplyAtmosphere()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.035f, 0.035f, 0.05f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.01f, 0.012f, 0.016f);
        RenderSettings.fogDensity = 0.035f;
        RenderSettings.skybox = null;
    }

    void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        GameSettings.Apply();
        ApplyAtmosphere();

        gameObject.AddComponent<AudioManager>();
        var gm = gameObject.AddComponent<GameManager>();
        var world = RoomBuilder.Build();

        // Player
        var playerGo = new GameObject("Player");
        playerGo.transform.position = world.playerSpawn;
        var cc = playerGo.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.32f; cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.25f; cc.skinWidth = 0.03f; cc.minMoveDistance = 0f;
        var pc = playerGo.AddComponent<PlayerController>();

        var camGo = new GameObject("PlayerCamera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(playerGo.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 72f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 60f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        camGo.AddComponent<AudioListener>();
        pc.cameraRoot = camGo.transform;
        pc.Teleport(world.playerSpawn, 0f);

        var flGo = new GameObject("Flashlight");
        flGo.transform.SetParent(camGo.transform, false);
        flGo.transform.localPosition = new Vector3(0.1f, -0.1f, 0.1f);
        var spot = flGo.AddComponent<Light>();
        spot.type = LightType.Spot; spot.spotAngle = 60f; spot.innerSpotAngle = 28f;
        spot.range = 14f; spot.intensity = 3f; spot.color = new Color(1f, 0.95f, 0.8f);
        spot.shadows = LightShadows.Soft;
        var flash = flGo.AddComponent<FlashlightController>();
        flash.Init(spot);

        var inter = camGo.AddComponent<InteractionSystem>();
        inter.Init(cam);

        var brain = gameObject.AddComponent<HorrorEntity>();
        brain.Init(world.entity, playerGo.transform, cam, world.ceilingLight, flash, world.spotFar, world.doorApproach);

        var events = gameObject.AddComponent<HorrorEventManager>();
        events.Init(playerGo.transform, cam, brain, world.ceilingLight, pc, world.spotFar, world.spotWardrobe);

        var ui = gameObject.AddComponent<UIManager>();
        ui.Init(gm, inter, pc);

        gm.Init(pc, brain, world.door, ui, events, flash, world.ceilingLight);

        if (world.keys.Count != GameManager.TotalKeys || world.door == null)
            Debug.LogError("THE LAST ROOM: validation failed (keys=" + world.keys.Count + ")");
        Physics.SyncTransforms();
    }
}
