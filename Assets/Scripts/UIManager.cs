using UnityEngine;

// All UI is drawn with IMGUI so it works with any input handling setting and needs no EventSystem.
public class UIManager : MonoBehaviour
{
    public bool ShowingHowTo;

    GameManager gm;
    InteractionSystem inter;
    PlayerController player;
    string msg;
    float msgTimer, fade, fadeTarget, fadeSpeed, flash, flashDecay;
    Color fadeColor = Color.black, flashColor = Color.white;
    GUIStyle title, sub, body, list, hud, prompt, small, btn;
    bool stylesReady;

    static readonly Color Red = new Color(0.75f, 0.05f, 0.05f);
    static readonly Color Grey = new Color(0.8f, 0.8f, 0.78f);

    public void Init(GameManager g, InteractionSystem i, PlayerController p) { gm = g; inter = i; player = p; }
    public void ShowMessage(string t, float s) { msg = t; msgTimer = s; }
    public void SetFadeColor(Color c) { fadeColor = c; }
    public void SetFade(float a) { fade = a; fadeTarget = a; }
    public void FadeTo(float a, float dur) { fadeTarget = a; fadeSpeed = dur <= 0.001f ? 1000f : Mathf.Abs(a - fade) / dur; }
    public void Flash(Color c, float a, float dur) { flashColor = c; flash = a; flashDecay = a / Mathf.Max(0.01f, dur); }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        fade = Mathf.MoveTowards(fade, fadeTarget, fadeSpeed * dt);
        flash = Mathf.MoveTowards(flash, 0f, flashDecay * dt);
        if (msgTimer > 0f) msgTimer -= dt;
    }

    static Texture2D Tex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c); t.Apply();
        t.hideFlags = HideFlags.HideAndDontSave;
        return t;
    }

    void EnsureStyles()
    {
        if (stylesReady) return;
        stylesReady = true;
        title = new GUIStyle(GUI.skin.label) { fontSize = 84, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        sub = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter };
        body = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.UpperCenter, wordWrap = true };
        list = new GUIStyle(GUI.skin.label) { fontSize = 30, alignment = TextAnchor.UpperLeft };
        hud = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft };
        prompt = new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter };
        small = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleRight };
        btn = new GUIStyle(GUI.skin.button) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
        var n = Tex(new Color(0.08f, 0.08f, 0.09f, 0.95f));
        var h = Tex(new Color(0.35f, 0.04f, 0.04f, 0.95f));
        var a = Tex(new Color(0.2f, 0.02f, 0.02f, 0.95f));
        btn.normal.background = n; btn.hover.background = h; btn.active.background = a; btn.focused.background = n;
        btn.normal.textColor = Grey; btn.hover.textColor = Color.white; btn.active.textColor = Color.white; btn.focused.textColor = Grey;
    }

    void Fill(Rect r, Color c)
    {
        var o = GUI.color; GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = o;
    }

    void Text(Rect r, string s, GUIStyle st, Color c)
    {
        var old = st.normal.textColor;
        st.normal.textColor = Color.black;
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), s, st);
        st.normal.textColor = c;
        GUI.Label(r, s, st);
        st.normal.textColor = old;
    }

    bool Button(float cx, float y, string label)
    {
        bool c = GUI.Button(new Rect(cx - 170f, y, 340f, 54f), label, btn);
        if (c) AudioManager.Sfx("click", 0.5f);
        return c;
    }

    void Settings(float cx, float y)
    {
        Text(new Rect(cx - 330f, y, 300f, 30f), "Mouse sensitivity", small, Grey);
        float s = GUI.HorizontalSlider(new Rect(cx + 10f, y + 9f, 260f, 20f), GameSettings.Sensitivity, 0.5f, 6f);
        if (!Mathf.Approximately(s, GameSettings.Sensitivity)) GameSettings.Sensitivity = s;
        Text(new Rect(cx - 330f, y + 42f, 300f, 30f), "Master volume", small, Grey);
        float v = GUI.HorizontalSlider(new Rect(cx + 10f, y + 51f, 260f, 20f), GameSettings.Volume, 0f, 1f);
        if (!Mathf.Approximately(v, GameSettings.Volume)) GameSettings.Volume = v;
    }

    void OnGUI()
    {
        if (gm == null) return;
        EnsureStyles();
        float scale = Screen.height / 720f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float W = Screen.width / scale;
        const float H = 720f;
        float cx = W * 0.5f;
        var full = new Rect(0, 0, W, H);

        switch (gm.State)
        {
            case GameState.Menu:
                Fill(full, new Color(0f, 0f, 0f, 0.92f));
                if (ShowingHowTo) DrawHowTo(cx);
                else DrawMenu(cx);
                break;
            case GameState.Playing:
            case GameState.Escaping:
                DrawHud(W, H);
                break;
            case GameState.Paused:
                Fill(full, new Color(0f, 0f, 0f, 0.72f));
                Text(new Rect(0, 90, W, 100), "PAUSED", title, Grey);
                if (Button(cx, 230, "RESUME")) gm.Resume();
                if (Button(cx, 296, "RESTART")) gm.Restart();
                if (Button(cx, 362, "MAIN MENU")) gm.ToMenu();
                if (Button(cx, 428, "QUIT")) gm.Quit();
                Settings(cx, 520);
                break;
            case GameState.GameOver:
                Fill(full, new Color(0.02f, 0f, 0f, 1f));
                Text(new Rect(0, 150, W, 120), "IT FOUND YOU", title, Red);
                if (Button(cx, 340, "TRY AGAIN")) gm.Restart();
                if (Button(cx, 410, "MAIN MENU")) gm.ToMenu();
                if (Button(cx, 480, "QUIT")) gm.Quit();
                break;
            case GameState.Victory:
                Fill(full, new Color(0.03f, 0.04f, 0.05f, 1f));
                Text(new Rect(0, 120, W, 120), "YOU ESCAPED", title, new Color(0.85f, 0.9f, 0.85f));
                Text(new Rect(cx - 400f, 245, 800f, 100), "You made it outside. But the door behind you is still open, and the dark inside is looking back.", body, Grey);
                if (Button(cx, 380, "PLAY AGAIN")) gm.Restart();
                if (Button(cx, 450, "MAIN MENU")) gm.ToMenu();
                if (Button(cx, 520, "QUIT")) gm.Quit();
                break;
        }

        if (fade > 0.001f) Fill(full, new Color(fadeColor.r, fadeColor.g, fadeColor.b, fade));
        if (flash > 0.001f) Fill(full, new Color(flashColor.r, flashColor.g, flashColor.b, flash));
    }

    void DrawMenu(float cx)
    {
        float W = cx * 2f;
        Text(new Rect(0, 80, W, 110), "THE LAST ROOM", title, Red);
        Text(new Rect(0, 195, W, 40), "Some doors should never be opened.", sub, Grey);
        if (Button(cx, 290, "PLAY")) gm.StartGame();
        if (Button(cx, 358, "HOW TO PLAY")) ShowingHowTo = true;
        if (Button(cx, 426, "QUIT")) gm.Quit();
        Settings(cx, 540);
    }

    void DrawHowTo(float cx)
    {
        float W = cx * 2f;
        Text(new Rect(0, 40, W, 90), "HOW TO PLAY", title, Red);
        Text(new Rect(cx - 260f, 150, 520, 260), "WASD - Move\nMouse - Look around\nLeft Shift - Sprint\nF - Toggle flashlight\nE - Interact\nEsc - Pause", list, Color.white);
        Text(new Rect(cx - 400f, 430, 800, 90), "Find all three keys and escape before the entity catches you.", body, Grey);
        Text(new Rect(cx - 400f, 500, 800, 50), "Hint: it is bolder when you look away.", sub, new Color(0.6f, 0.15f, 0.15f));
        if (Button(cx, 590, "BACK")) ShowingHowTo = false;
    }

    void DrawHud(float W, float H)
    {
        if (gm.State == GameState.Playing)
        {
            Text(new Rect(24, 18, 300, 40), "KEYS: " + gm.Keys + "/" + GameManager.TotalKeys, hud, Grey);

            if (player != null && player.Stamina01 < 0.995f)
            {
                Fill(new Rect(24, H - 34, 164, 10), new Color(0f, 0f, 0f, 0.5f));
                Fill(new Rect(26, H - 32, 160f * player.Stamina01, 6), new Color(0.75f, 0.75f, 0.7f, 0.7f));
            }

            string p = inter != null ? inter.CurrentPrompt : null;
            float size = p != null ? 7f : 4f;
            Fill(new Rect(W * 0.5f - size * 0.5f, H * 0.5f - size * 0.5f, size, size), new Color(1f, 1f, 1f, p != null ? 0.9f : 0.5f));
            if (p != null) Text(new Rect(0, H * 0.5f + 40f, W, 40), p, prompt, Color.white);
        }

        if (msgTimer > 0f && !string.IsNullOrEmpty(msg))
        {
            float a = Mathf.Clamp01(msgTimer / 0.8f);
            Text(new Rect(W * 0.5f - 420f, H - 190f, 840f, 120f), msg, body, new Color(0.92f, 0.92f, 0.86f, a));
        }
    }
}
