using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Menu, Playing, Paused, Dying, GameOver, Escaping, Victory }

public class GameManager : MonoBehaviour
{
    public const int TotalKeys = 3;
    public const string IntroText = "You don't remember how you got here. The door is locked. Find the keys. Whatever you do, don't let it find you.";

    public static GameManager Instance { get; private set; }
    public static bool SkipMenuOnLoad;

    public GameState State { get; private set; }
    public int Keys { get; private set; }
    public float PlayTime { get; private set; }
    public PlayerController Player { get { return player; } }
    public UIManager UI { get { return ui; } }
    public HorrorEventManager Events { get { return events; } }

    PlayerController player;
    HorrorEntity entity;
    ExitDoor door;
    UIManager ui;
    HorrorEventManager events;
    FlashlightController flashlight;
    FlickeringLight ceilingLight;
    bool caught, escaped;

    void Awake() { Instance = this; State = GameState.Menu; }
    void OnDestroy() { if (Instance == this) Instance = null; }

    public void Init(PlayerController p, HorrorEntity e, ExitDoor d, UIManager u, HorrorEventManager ev, FlashlightController f, FlickeringLight l)
    {
        player = p; entity = e; door = d; ui = u; events = ev; flashlight = f; ceilingLight = l;
    }

    void Start()
    {
        if (SkipMenuOnLoad) { SkipMenuOnLoad = false; StartGame(); }
        else { State = GameState.Menu; SetCursor(false); }
    }

    void Update()
    {
        if (State == GameState.Playing)
        {
            PlayTime += Time.deltaTime;
            if (GameInput.PausePressed) Pause();
        }
        else if (State == GameState.Paused)
        {
            if (GameInput.PausePressed) Resume();
        }
        else if (State == GameState.Menu && ui != null && ui.ShowingHowTo && GameInput.PausePressed)
        {
            ui.ShowingHowTo = false;
        }
    }

    public void ShowMessage(string text, float seconds) { if (ui != null) ui.ShowMessage(text, seconds); }

    void SetCursor(bool gameplay)
    {
        Cursor.lockState = gameplay ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !gameplay;
    }

    public void StartGame()
    {
        if (State == GameState.Playing) return;
        Keys = 0; PlayTime = 0f; caught = false; escaped = false;
        State = GameState.Playing;
        SetCursor(true);
        ui.SetFadeColor(Color.black);
        ui.SetFade(1f);
        ui.FadeTo(0f, 1.6f);
        ui.ShowMessage(IntroText, 8f);
    }

    public void Pause()
    {
        if (State != GameState.Playing) return;
        State = GameState.Paused;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        SetCursor(false);
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        State = GameState.Playing;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SetCursor(true);
    }

    public bool CollectKey()
    {
        if (State != GameState.Playing || Keys >= TotalKeys) return false;
        Keys++;
        AudioManager.Sfx("pickup", 0.8f);
        if (Keys >= TotalKeys)
        {
            ShowMessage("All keys collected. The exit is unlocked.", 6f);
            entity.BeginFinalAppearance();
        }
        else if (Keys == 1) ShowMessage("Key 1 of 3 found. The room feels colder.", 4f);
        else ShowMessage("Key 2 of 3 found. Something is watching you.", 4f);
        events.OnKeyCollected(Keys);
        return true;
    }

    public void PlayerCaught()
    {
        if (caught || State != GameState.Playing) return;
        caught = true;
        StartCoroutine(CatchRoutine());
    }

    IEnumerator CatchRoutine()
    {
        State = GameState.Dying;
        player.Freeze();
        entity.PrepareJumpScare();
        player.StartCoroutine(player.LookAtRoutine(entity.HeadPosition, 0.18f));
        AudioManager.Sfx("scare", 0.85f);
        player.Shake(0.12f, 1.1f);
        ui.Flash(Color.white, 0.9f, 0.18f);
        flashlight.Flicker(1.2f);
        ceilingLight.Burst(1.2f);
        yield return new WaitForSeconds(1.35f);
        ui.SetFadeColor(Color.black);
        ui.FadeTo(1f, 0.45f);
        yield return new WaitForSeconds(0.6f);
        entity.HideNow();
        State = GameState.GameOver;
        SetCursor(false);
        ui.FadeTo(0f, 0.6f);
    }

    public void Escape()
    {
        if (State != GameState.Playing || escaped) return;
        escaped = true;
        StartCoroutine(EscapeRoutine());
    }

    IEnumerator EscapeRoutine()
    {
        State = GameState.Escaping;
        player.Freeze();
        entity.HideNow();
        door.Open();
        yield return new WaitForSeconds(1.2f);
        ui.SetFadeColor(new Color(1f, 1f, 0.95f));
        ui.FadeTo(1f, 1.4f);
        yield return new WaitForSeconds(1.5f);
        AudioManager.Sfx("victory", 0.8f);
        State = GameState.Victory;
        SetCursor(false);
        ui.FadeTo(0f, 1.2f);
    }

    void Reload()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Restart() { SkipMenuOnLoad = true; Reload(); }
    public void ToMenu() { SkipMenuOnLoad = false; Reload(); }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
