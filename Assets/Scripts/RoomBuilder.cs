using System.Collections.Generic;
using UnityEngine;

public class WorldRefs
{
    public Transform root;
    public Vector3 playerSpawn;
    public ExitDoor door;
    public FlickeringLight ceilingLight;
    public GameObject entity;
    public Vector3 spotFar, spotWardrobe, doorApproach;
    public List<KeyPickup> keys = new List<KeyPickup>();
}

// Builds the whole room from primitives. Room interior: X -4..4, Z -5..5, height 3. Floor top is y = 0.
public static class RoomBuilder
{
    static Transform root;

    static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }
    static Color C(float r, float g, float b) { return new Color(r, g, b, 1f); }

    static Transform Group(string name, Vector3 pos, float yaw, Transform parent)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent != null ? parent : root, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return g.transform;
    }

    static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider = true, Vector3 euler = default(Vector3))
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.transform.localRotation = Quaternion.Euler(euler);
        g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider)
        {
            var c = g.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }
        return g;
    }

    static GameObject Box(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool collider = true, Vector3 euler = default(Vector3))
    {
        return Prim(PrimitiveType.Cube, name, parent, pos, scale, mat, collider, euler);
    }

    static Light MakeLight(string name, Transform parent, Vector3 pos, Color col, float intensity, float range, LightShadows shadows)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        var l = g.AddComponent<Light>();
        l.type = LightType.Point; l.color = col; l.intensity = intensity; l.range = range; l.shadows = shadows;
        return l;
    }

    static void Inspect(GameObject g, string text, bool spook = false)
    {
        var i = g.AddComponent<InspectObject>();
        i.text = text; i.spook = spook;
    }

    static KeyPickup CreateKey(Transform parent, string name, Vector3 pos, float yaw, Material gold)
    {
        var k = Group(name, pos, yaw, parent);
        Prim(PrimitiveType.Sphere, "Bow", k, V(0f, 0.006f, -0.06f), V(0.05f, 0.012f, 0.05f), gold, false);
        Box("Shaft", k, V(0f, 0.006f, 0.005f), V(0.012f, 0.012f, 0.12f), gold, false);
        Box("Tooth1", k, V(0.014f, 0.006f, 0.055f), V(0.025f, 0.01f, 0.012f), gold, false);
        Box("Tooth2", k, V(0.014f, 0.006f, 0.035f), V(0.025f, 0.01f, 0.012f), gold, false);
        var col = k.gameObject.AddComponent<BoxCollider>();
        col.isTrigger = true; col.center = V(0f, 0.03f, 0f); col.size = V(0.28f, 0.12f, 0.28f);
        return k.gameObject.AddComponent<KeyPickup>();
    }

    public static WorldRefs Build()
    {
        var w = new WorldRefs();
        root = new GameObject("World").transform;
        w.root = root;

        // ---------- Materials ----------
        var floorMat = Mats.Make(Color.white, 0.3f, null, Mats.Noise(256, C(0.09f, 0.06f, 0.04f), C(0.24f, 0.16f, 0.10f), 7f, 5, 1), new Vector2(7f, 4f));
        var wallMat = Mats.Make(Color.white, 0.05f, null, Mats.Noise(256, C(0.10f, 0.11f, 0.10f), C(0.22f, 0.23f, 0.20f), 5f, 0, 2), new Vector2(3f, 1f));
        var ceilMat = Mats.Make(C(0.08f, 0.08f, 0.08f), 0f);
        var trimMat = Mats.Make(C(0.05f, 0.04f, 0.035f), 0.1f);
        var woodMat = Mats.Make(Color.white, 0.2f, null, Mats.Noise(128, C(0.14f, 0.09f, 0.06f), C(0.28f, 0.18f, 0.11f), 4f, 0, 3));
        var darkWood = Mats.Make(C(0.08f, 0.055f, 0.04f), 0.2f);
        var fabricMat = Mats.Make(C(0.35f, 0.33f, 0.30f), 0f);
        var sheetMat = Mats.Make(C(0.55f, 0.52f, 0.48f), 0f);
        var blanketMat = Mats.Make(C(0.22f, 0.10f, 0.10f), 0f);
        var metalMat = Mats.Make(C(0.2f, 0.2f, 0.22f), 0.55f);
        var crateMat = Mats.Make(C(0.25f, 0.18f, 0.10f), 0.1f);
        var rugMat = Mats.Make(C(0.25f, 0.04f, 0.04f), 0f);
        var paperMat = Mats.Make(C(0.8f, 0.76f, 0.65f), 0f);
        var doorMat = Mats.Make(Color.white, 0.2f, null, Mats.Noise(128, C(0.09f, 0.055f, 0.03f), C(0.2f, 0.12f, 0.07f), 4f, 0, 4));
        var glassMat = Mats.Make(C(0.05f, 0.07f, 0.09f), 0.9f);
        var bulbEmission = C(2f, 1.4f, 0.8f);
        var bulbMat = Mats.Make(C(1f, 0.85f, 0.6f), 0.1f, bulbEmission);
        var windowMat = Mats.Make(C(0.03f, 0.05f, 0.1f), 0.1f, C(0.05f, 0.09f, 0.2f));
        var lampMat = Mats.Make(C(0.5f, 0.05f, 0.05f), 0.1f, C(2f, 0.1f, 0.1f));
        var glowMat = Mats.Make(Color.white, 0f, C(1.5f, 1.5f, 1.4f));
        var keyMat = Mats.Make(C(0.8f, 0.65f, 0.15f), 0.7f, C(0.45f, 0.34f, 0.05f));
        var bookA = Mats.Make(C(0.3f, 0.07f, 0.07f), 0f);
        var bookB = Mats.Make(C(0.07f, 0.12f, 0.2f), 0f);

        // ---------- Structure ----------
        var S = Group("Structure", Vector3.zero, 0f, null);
        Box("Floor", S, V(0f, -0.15f, 0f), V(8.8f, 0.3f, 10.8f), floorMat);
        Box("Ceiling", S, V(0f, 3.15f, 0f), V(8.8f, 0.3f, 10.8f), ceilMat);
        Box("WallWest", S, V(-4.15f, 1.5f, 0f), V(0.3f, 3.3f, 10.6f), wallMat);
        Box("WallEast", S, V(4.15f, 1.5f, 0f), V(0.3f, 3.3f, 10.6f), wallMat);
        Box("WallSouth", S, V(0f, 1.5f, -5.15f), V(8f, 3.3f, 0.3f), wallMat);
        Box("WallNorthLeft", S, V(-1.3f, 1.5f, 5.15f), V(5.4f, 3.3f, 0.3f), wallMat);
        Box("WallNorthRight", S, V(3.3f, 1.5f, 5.15f), V(1.4f, 3.3f, 0.3f), wallMat);
        Box("DoorLintel", S, V(2f, 2.675f, 5.15f), V(1.2f, 0.95f, 0.3f), wallMat);

        // Trim and beams (visual depth)
        Box("TrimWest", S, V(-3.97f, 0.09f, 0f), V(0.06f, 0.18f, 10f), trimMat, false);
        Box("TrimEast", S, V(3.97f, 0.09f, 0f), V(0.06f, 0.18f, 10f), trimMat, false);
        Box("TrimSouth", S, V(0f, 0.09f, -4.97f), V(8f, 0.18f, 0.06f), trimMat, false);
        Box("TrimNorthL", S, V(-1.3f, 0.09f, 4.97f), V(5.4f, 0.18f, 0.06f), trimMat, false);
        Box("TrimNorthR", S, V(3.3f, 0.09f, 4.97f), V(1.4f, 0.18f, 0.06f), trimMat, false);
        Box("BeamA", S, V(0f, 2.88f, -2.5f), V(8f, 0.24f, 0.28f), darkWood, false);
        Box("BeamB", S, V(0f, 2.88f, 2.5f), V(8f, 0.24f, 0.28f), darkWood, false);

        // ---------- Exit door ----------
        var doorPivot = Group("ExitDoor", V(1.4f, 0f, 5.15f), 0f, S);
        Box("DoorPanel", doorPivot, V(0.6f, 1.1f, 0f), V(1.2f, 2.2f, 0.12f), doorMat);
        Prim(PrimitiveType.Sphere, "HandleIn", doorPivot, V(1.05f, 1.05f, -0.09f), V(0.08f, 0.08f, 0.08f), metalMat, false);
        Prim(PrimitiveType.Sphere, "HandleOut", doorPivot, V(1.05f, 1.05f, 0.09f), V(0.08f, 0.08f, 0.08f), metalMat, false);
        var door = doorPivot.gameObject.AddComponent<ExitDoor>();
        var lamp = Prim(PrimitiveType.Sphere, "DoorLamp", S, V(2f, 2.45f, 4.95f), V(0.14f, 0.14f, 0.14f), lampMat, false);
        var lampLight = MakeLight("DoorLampLight", S, V(2f, 2.35f, 4.7f), C(1f, 0.1f, 0.1f), 0.7f, 3.5f, LightShadows.None);
        var outsideLight = MakeLight("OutsideLight", S, V(2f, 1.8f, 6.2f), C(1f, 0.97f, 0.9f), 3f, 9f, LightShadows.None);
        outsideLight.enabled = false;
        Box("OutsideGlow", S, V(2f, 1.5f, 7.4f), V(7f, 3.6f, 0.1f), glowMat, false);
        door.Setup(lamp.GetComponent<Renderer>(), lampLight, outsideLight);
        w.door = door;

        // ---------- Bed (west wall) ----------
        var F = Group("Furniture", Vector3.zero, 0f, null);
        var bed = Box("Bed", F, V(-3.1f, 0.25f, 2.8f), V(1.1f, 0.35f, 2.1f), darkWood);
        Inspect(bed, "The sheets are still warm. Someone was here a moment ago.");
        Box("Mattress", F, V(-3.1f, 0.5f, 2.8f), V(1f, 0.18f, 2f), fabricMat);
        Box("Pillow", F, V(-3.1f, 0.65f, 3.55f), V(0.7f, 0.12f, 0.4f), sheetMat, false);
        Box("Blanket", F, V(-3.1f, 0.62f, 2.4f), V(0.95f, 0.06f, 1.1f), blanketMat, false, V(0f, 6f, 0f));
        Box("Headboard", F, V(-3.1f, 0.8f, 3.88f), V(1.1f, 0.9f, 0.1f), darkWood);

        // Bedside table + lamp (key 1)
        Box("BedsideTable", F, V(-3.2f, 0.3f, 0.95f), V(0.5f, 0.6f, 0.5f), woodMat);
        Prim(PrimitiveType.Cylinder, "LampBase", F, V(-3.3f, 0.65f, 0.85f), V(0.08f, 0.05f, 0.08f), metalMat, false);
        Prim(PrimitiveType.Cylinder, "LampShade", F, V(-3.3f, 0.78f, 0.85f), V(0.2f, 0.08f, 0.2f), fabricMat, false);

        // Table, chair, items
        var table = Group("Table", V(1.2f, 0f, 0.5f), 0f, F);
        Box("TableTop", table, V(0f, 0.75f, 0f), V(1.6f, 0.06f, 0.9f), woodMat);
        foreach (float lx in new[] { -0.7f, 0.7f })
            foreach (float lz in new[] { -0.38f, 0.38f })
                Box("TableLeg", table, V(lx, 0.36f, lz), V(0.07f, 0.72f, 0.07f), darkWood);
        Box("BookA", table, V(-0.4f, 0.805f, 0.1f), V(0.3f, 0.05f, 0.22f), bookA, false, V(0f, 20f, 0f));
        Box("BookB", table, V(-0.38f, 0.855f, 0.1f), V(0.26f, 0.05f, 0.2f), bookB, false, V(0f, -10f, 0f));
        Prim(PrimitiveType.Cylinder, "Cup", table, V(0.55f, 0.82f, -0.2f), V(0.08f, 0.04f, 0.08f), metalMat, false);
        var note = Box("Note", table, V(0.25f, 0.785f, 0.1f), V(0.28f, 0.006f, 0.2f), paperMat, true, V(0f, 12f, 0f));
        Inspect(note, "Scratched into the paper: IT ONLY MOVES WHEN YOU LOOK AWAY.");

        var chair = Group("Chair", V(1.15f, 0f, 1.45f), 15f, F);
        Box("Seat", chair, V(0f, 0.45f, 0f), V(0.45f, 0.05f, 0.45f), woodMat);
        Box("Back", chair, V(0f, 0.8f, 0.2f), V(0.45f, 0.7f, 0.05f), woodMat);
        foreach (float lx in new[] { -0.19f, 0.19f })
            foreach (float lz in new[] { -0.19f, 0.19f })
                Box("ChairLeg", chair, V(lx, 0.22f, lz), V(0.05f, 0.44f, 0.05f), darkWood);

        // Wardrobe (east wall, hollow, key 2 on the inner shelf)
        var wr = Group("Wardrobe", Vector3.zero, 0f, F);
        Box("WardrobeBack", wr, V(3.92f, 1.1f, -2.2f), V(0.06f, 2.2f, 1.4f), darkWood);
        Box("WardrobeSideS", wr, V(3.55f, 1.1f, -2.87f), V(0.8f, 2.2f, 0.06f), woodMat);
        Box("WardrobeSideN", wr, V(3.55f, 1.1f, -1.53f), V(0.8f, 2.2f, 0.06f), woodMat);
        Box("WardrobeTop", wr, V(3.55f, 2.17f, -2.2f), V(0.8f, 0.06f, 1.4f), woodMat);
        Box("WardrobePlinth", wr, V(3.55f, 0.1f, -2.2f), V(0.8f, 0.2f, 1.4f), woodMat);
        Box("WardrobeShelf", wr, V(3.55f, 1.2f, -2.2f), V(0.74f, 0.04f, 1.34f), woodMat);
        var dl = Group("WardrobeDoorL", V(3.15f, 1.1f, -2.87f), -70f, wr);
        Box("Panel", dl, V(0f, 0f, 0.34f), V(0.04f, 2.1f, 0.68f), woodMat);
        var dr = Group("WardrobeDoorR", V(3.15f, 1.1f, -1.53f), 25f, wr);
        Box("Panel", dr, V(0f, 0f, -0.34f), V(0.04f, 2.1f, 0.68f), woodMat);

        // Shelving unit (west wall, key 3 on the top board behind a jar)
        var sh = Group("Shelves", Vector3.zero, 0f, F);
        Box("UprightA", sh, V(-3.7f, 1f, -3.28f), V(0.4f, 2f, 0.05f), darkWood);
        Box("UprightB", sh, V(-3.7f, 1f, -1.72f), V(0.4f, 2f, 0.05f), darkWood);
        Box("BoardLow", sh, V(-3.7f, 0.4f, -2.5f), V(0.4f, 0.04f, 1.6f), woodMat);
        Box("BoardMid", sh, V(-3.7f, 0.95f, -2.5f), V(0.4f, 0.04f, 1.6f), woodMat);
        Box("BoardTop", sh, V(-3.7f, 1.5f, -2.5f), V(0.4f, 0.04f, 1.6f), woodMat);
        Prim(PrimitiveType.Cylinder, "Jar", sh, V(-3.62f, 1.62f, -2.15f), V(0.1f, 0.1f, 0.1f), glassMat, false);
        Box("BookC", sh, V(-3.7f, 1.04f, -2.9f), V(0.25f, 0.14f, 0.1f), bookA, false);
        Box("BookD", sh, V(-3.7f, 1.04f, -2.78f), V(0.25f, 0.12f, 0.08f), bookB, false);
        Box("Tin", sh, V(-3.7f, 0.5f, -2.2f), V(0.2f, 0.12f, 0.2f), metalMat, false);

        // ---------- Props ----------
        var P = Group("Props", Vector3.zero, 0f, null);
        var crate1 = Box("Crate1", P, V(3.3f, 0.25f, -4.45f), V(0.5f, 0.5f, 0.5f), crateMat, true, V(0f, 10f, 0f));
        Inspect(crate1, "Empty. Deep scratch marks line the inside.");
        Box("Crate2", P, V(2.7f, 0.25f, -4.5f), V(0.5f, 0.5f, 0.5f), crateMat, true, V(0f, -8f, 0f));
        Box("Crate3", P, V(3.25f, 0.72f, -4.45f), V(0.42f, 0.42f, 0.42f), crateMat, true, V(0f, 35f, 0f));
        Prim(PrimitiveType.Cylinder, "Barrel", P, V(-3.4f, 0.45f, -4.3f), V(0.6f, 0.45f, 0.6f), woodMat);
        Box("Rug", P, V(0.6f, 0.01f, -0.2f), V(3f, 0.02f, 2.2f), rugMat, false, V(0f, 8f, 0f));
        Prim(PrimitiveType.Cylinder, "Bottle", P, V(-1.2f, 0.06f, 1.8f), V(0.12f, 0.1f, 0.12f), glassMat, false, V(0f, 0f, 90f));
        Vector3[] papers = { V(-1.5f, 0.012f, -2.2f), V(0.2f, 0.012f, -3.1f), V(-0.6f, 0.012f, 3.0f), V(2.2f, 0.012f, 2.6f), V(-2.0f, 0.012f, 0.1f), V(1.0f, 0.012f, -1.9f) };
        for (int i = 0; i < papers.Length; i++)
            Box("Paper" + i, P, papers[i], V(0.25f, 0.004f, 0.18f), paperMat, false, V(0f, 37f * i + 10f, 0f));

        var frame = Box("PaintingFrame", P, V(-2f, 1.7f, 4.97f), V(0.8f, 0.55f, 0.04f), darkWood, true, V(0f, 0f, 3f));
        Box("PaintingCanvas", P, V(-2f, 1.7f, 4.95f), V(0.7f, 0.45f, 0.02f), Mats.Make(C(0.07f, 0.1f, 0.07f), 0f), false, V(0f, 0f, 3f));
        Inspect(frame, "A portrait. The face has been scratched out, but the eyes are still there.");
        var mirror = Box("Mirror", P, V(3.97f, 1.6f, 3.2f), V(0.04f, 1.1f, 0.7f), glassMat);
        Inspect(mirror, "Your reflection blinks a moment too late.", true);

        Box("WindowPane", P, V(3.99f, 1.7f, 1.2f), V(0.04f, 1f, 0.7f), windowMat, false);
        Box("Board1", P, V(3.94f, 1.45f, 1.2f), V(0.06f, 0.1f, 0.95f), darkWood, false, V(3f, 0f, 0f));
        Box("Board2", P, V(3.94f, 1.7f, 1.2f), V(0.06f, 0.1f, 0.95f), darkWood, false, V(-4f, 0f, 0f));
        Box("Board3", P, V(3.94f, 1.95f, 1.2f), V(0.06f, 0.1f, 0.95f), darkWood, false, V(2f, 0f, 0f));

        // ---------- Lights ----------
        var L = Group("Lights", Vector3.zero, 0f, null);
        var ceil = MakeLight("CeilingLight", L, V(0f, 2.45f, -0.5f), C(1f, 0.72f, 0.45f), 1.6f, 9f, LightShadows.Soft);
        var bulb = Prim(PrimitiveType.Sphere, "Bulb", L, V(0f, 2.45f, -0.5f), V(0.16f, 0.16f, 0.16f), bulbMat, false);
        Prim(PrimitiveType.Cylinder, "Cord", L, V(0f, 2.8f, -0.5f), V(0.012f, 0.25f, 0.012f), trimMat, false);
        var fl = ceil.gameObject.AddComponent<FlickeringLight>();
        fl.Init(ceil, bulb.GetComponent<Renderer>(), bulbEmission, true);
        w.ceilingLight = fl;
        MakeLight("WindowLight", L, V(3.3f, 1.7f, 1.2f), C(0.45f, 0.6f, 1f), 0.55f, 5f, LightShadows.None);

        // ---------- Keys ----------
       var K = Group("Keys", Vector3.zero, 0f, null);

// Candidate spots: position sits on a surface, plus a yaw angle.
var spots = new List<Vector4>
{
    new Vector4(-3.2f, 0.60f,  0.95f,  25f),  // bedside table
    new Vector4( 3.5f, 1.22f, -2.30f,  80f),  // inside wardrobe
    new Vector4(-3.72f,1.52f, -2.45f, -30f),  // top shelf behind jar
    new Vector4(-3.7f, 0.97f, -2.00f,  15f),  // middle shelf
    new Vector4(-3.7f, 0.42f, -3.00f, -50f),  // low shelf
    new Vector4( 1.0f, 0.78f,  0.80f,  40f),  // table top
    new Vector4( 1.15f,0.475f, 1.45f,  10f),  // chair seat
    new Vector4( 2.7f, 0.50f, -4.50f, -20f),  // crate top
    new Vector4(-3.4f, 0.90f, -4.30f,  60f),  // barrel top
    new Vector4(-3.1f, 0.71f,  3.55f,   0f),  // pillow
};

// Fisher-Yates shuffle, then take the first three.
for (int i = spots.Count - 1; i > 0; i--)
{
    int j = Random.Range(0, i + 1);
    var tmp = spots[i]; spots[i] = spots[j]; spots[j] = tmp;
}
for (int i = 0; i < GameManager.TotalKeys; i++)
{
    var s = spots[i];
    w.keys.Add(CreateKey(K, "Key" + (i + 1), new Vector3(s.x, s.y, s.z), s.w, keyMat));
}

        // ---------- Entity visual ----------
        w.entity = BuildEntity();

        w.playerSpawn = V(0f, 0.1f, -4f);
        w.spotFar = V(-0.5f, 0f, 4.2f);
        w.spotWardrobe = V(2.4f, 0f, -2f);
        w.doorApproach = V(2f, 0f, 4f);
        return w;
    }

    static GameObject BuildEntity()
    {
        var e = new GameObject("Entity");
        e.transform.SetParent(root, false);
        var body = Mats.Make(C(0.015f, 0.015f, 0.02f), 0f, C(0.02f, 0.025f, 0.045f));
        var eye = Mats.Make(Color.black, 0f, C(2f, 2f, 1.7f));
        var t = e.transform;
        Prim(PrimitiveType.Capsule, "Torso", t, V(0f, 1.15f, 0f), V(0.42f, 0.55f, 0.26f), body, false);
        Prim(PrimitiveType.Capsule, "Legs", t, V(0f, 0.32f, 0f), V(0.3f, 0.32f, 0.22f), body, false);
        Prim(PrimitiveType.Sphere, "Head", t, V(0f, 1.95f, 0f), V(0.26f, 0.34f, 0.26f), body, false);
        Prim(PrimitiveType.Capsule, "ArmL", t, V(-0.34f, 1.15f, 0f), V(0.1f, 0.55f, 0.1f), body, false, V(0f, 0f, -8f));
        Prim(PrimitiveType.Capsule, "ArmR", t, V(0.34f, 1.15f, 0f), V(0.1f, 0.55f, 0.1f), body, false, V(0f, 0f, 8f));
        Prim(PrimitiveType.Sphere, "EyeL", t, V(-0.07f, 1.98f, 0.12f), V(0.045f, 0.045f, 0.045f), eye, false);
        Prim(PrimitiveType.Sphere, "EyeR", t, V(0.07f, 1.98f, 0.12f), V(0.045f, 0.045f, 0.045f), eye, false);
        var lg = new GameObject("EntityLight");
        lg.transform.SetParent(t, false);
        lg.transform.localPosition = V(0f, 1.4f, 0.9f);
        var l = lg.AddComponent<Light>();
        l.type = LightType.Point; l.color = C(0.5f, 0.6f, 1f); l.intensity = 0.8f; l.range = 3.5f; l.shadows = LightShadows.None;
        foreach (var r in e.GetComponentsInChildren<Renderer>())
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return e;
    }
}
