using UnityEngine;

public static class GameSettings
{
    const string SensKey = "tlr_sens";
    const string VolKey = "tlr_vol";

    public static float Sensitivity
    {
        get { return PlayerPrefs.GetFloat(SensKey, 2f); }
        set { PlayerPrefs.SetFloat(SensKey, value); }
    }

    public static float Volume
    {
        get { return PlayerPrefs.GetFloat(VolKey, 0.8f); }
        set { PlayerPrefs.SetFloat(VolKey, value); AudioListener.volume = value; }
    }

    public static void Apply() { AudioListener.volume = Volume; }
}
