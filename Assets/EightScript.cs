using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class EightScript : MonoBehaviour
{

    static int _highestID = 0;
    static int _moduleIdCounter = 1;
    static bool _interrupted = false;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBossModule Boss;
    public KMSelectable ButtonSelectable;
    public TextMesh EightText;
    public TextMesh LegacyModeText;
    public GameObject ButtonCap;
    public Light Light;
    public MeshRenderer Pin;
    public static string[] IgnoredModules = null;

    private KMAudio.KMAudioRef[] Sounds = new KMAudio.KMAudioRef[4];
    private int SolveCache, TimesPressed;
    private int Minimum = 1;
    private bool Autosolve, Passed, Pressing, Solved;
    private bool Interrupted = true;
    private Settings _Settings;

    class Settings
    {
        public bool LegacyMode = false;
    }

    void GetSettings()
    {
        var SettingsConfig = new ModConfig<Settings>("8");
        _Settings = SettingsConfig.Settings; // This reads the settings from the file, or creates a new file if it does not exist
        SettingsConfig.Settings = _Settings; // This writes any updates or fixes if there's an issue with the file
    }

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        GetSettings();
        _interrupted = false;
        Pressing = true;
        Light.intensity = 0;
        if (IgnoredModules == null)
            IgnoredModules = GetComponent<KMBossModule>().GetIgnoredModules("8", new string[]{
                "14",
                "8",
                "Forget Enigma",
                "Forget Everything",
                "Forget It Not",
                "Forget Me Later",
                "Forget Me Not",
                "Forget Perspective",
                "Forget Them All",
                "Forget This",
                "Forget Us Not",
                "Organization",
                "Purgatory",
                "Simon's Stages",
                "Souvenir",
                "Tallordered Keys",
                "The Time Keeper",
                "Timing is Everything",
                "The Troll",
                "Turn The Key",
                "Übermodule",
                "Ültimate Custom Night",
                "The Very Annoying Button"
            });
        ButtonSelectable.OnInteract += ButtonPress;
        EightText.text = "";
        Module.OnActivate += delegate
        {
            Debug.LogFormat("[8 #{0}] The button must be pushed a minimum of {1} time{2} in order to soothe Stanley.", _moduleID, Minimum, Minimum == 1 ? "" : "s");
            if (_moduleID == _highestID)
                Sounds[0] = Audio.HandlePlaySoundAtTransformWithRef("activate", transform, false);
            StartCoroutine(WaitForSoundLength(41.801f));
            Pressing = false;
            Light.intensity = 2.5f;
            if (_Settings.LegacyMode)
                StartCoroutine(AnimLegacyMode());
        };
        Bomb.OnBombExploded += delegate { try { Sounds[0].StopSound(); } catch { } try { Sounds[1].StopSound(); } catch { } try { Sounds[2].StopSound(); } catch { } };
    }

    // Use this for initialization
    void Start()
    {
        if (_moduleID > _highestID)
            _highestID = _moduleID;
        float Scalar = transform.lossyScale.x;
        Light.range *= Scalar;
        StartCoroutine(SolveCheck());
        if (!_Settings.LegacyMode)
        {
            Minimum = 8;
            LegacyModeText.text = "";
        }
        else
            Debug.LogFormat("[8 #{0}] Legacy Mode is enabled!", _moduleID);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha8) && _moduleID == _highestID)
        {
            try
            {
                Sounds[3].StopSound();
            }
            catch { }
            Sounds[3] = Audio.HandlePlaySoundAtTransformWithRef("8", transform, false);
        }
    }

    private bool ButtonPress()
    {
        if (!Pressing)
        {
            StartCoroutine(AnimateButton(0f, -0.05f));
            if (Sounds[1] != null)
                Sounds[1].StopSound();
            Sounds[1] = Audio.HandlePlaySoundAtTransformWithRef("8", ButtonCap.transform, false);
            if (!Interrupted)
                _interrupted = true;
            TimesPressed++;
            if (TimesPressed >= Minimum && !Passed)
            {
                Passed = true;
                Pin.material.color = new Color(0, 1, 0);
                if (Bomb.GetSolvableModuleNames().Where(x => !IgnoredModules.Contains(x)).Count() == Bomb.GetSolvedModuleNames().Where(x => !IgnoredModules.Contains(x)).Count())
                {
                    Module.HandlePass();
                    Solved = true;
                    if (!Autosolve)
                        Debug.LogFormat("[8 #{0}] You pushed the button {1} time{2}, which successfully soothed Stanley. Module solved!", _moduleID, TimesPressed, TimesPressed == 1 ? "" : "s");
                    else
                        Debug.LogFormat("[8 #{0}] Autosolved.", _moduleID);
                }
            }
        }
        return false;
    }

    private IEnumerator AnimateButton(float a, float b)
    {
        if (a != 0)
        {
            EightText.text = "";
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, transform);
        }
        else
        {
            EightText.text = "8";
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);
        }
        Pressing = true;
        var duration = 0.3f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            ButtonCap.transform.localPosition = new Vector3(0f, Easing.OutCirc(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
        ButtonCap.transform.localPosition = new Vector3(0f, b, 0f);
        if (a == 0)
        {
            duration = 0.3f;
            while (duration > 0f)
            {
                yield return null;
                duration -= Time.deltaTime;
            }
            StartCoroutine(AnimateButton(-0.05f, 0f));
        }
        else
        {
            duration = 0.2f;
            while (duration > 0f)
            {
                yield return null;
                duration -= Time.deltaTime;
            }
            Pressing = false;
        }
    }

    private IEnumerator SolveCheck()
    {
        while (true)
        {
            yield return null;
            if (SolveCache != Bomb.GetSolvedModuleNames().Where(x => !IgnoredModules.Contains(x)).Count() && !Autosolve)
            {
                SolveCache = Bomb.GetSolvedModuleNames().Where(x => !IgnoredModules.Contains(x)).Count();
                if (_Settings.LegacyMode)
                    Minimum = SolveCache + 1;
                else
                    Minimum = 8;
                Pin.material.color = new Color();
                if (!Passed)
                {
                    try
                    {
                        Sounds[1].StopSound();
                    }
                    catch { }
                    if (_moduleID == _highestID)
                        Module.HandleStrike();
                    StartCoroutine(Strike());
                    _interrupted = true;
                    if (_moduleID == _highestID)
                        Audio.PlaySoundAtTransform("strike", ButtonCap.transform);
                    Debug.LogFormat("[8 #{0}] You pushed the button {1} time{2}, which was not enough to soothe Stanley. The button must now be pushed a minimum of {3} time{4} in order to soothe him again.", _moduleID, TimesPressed, TimesPressed == 1 ? "" : "s", Minimum, Minimum == 1 ? "" : "s");
                }
                else
                    Debug.LogFormat("[8 #{0}] You pushed the button {1} time{2}, which successfully soothed Stanley. The button must now be pushed {3} time{4} in order to soothe him again.", _moduleID, TimesPressed, TimesPressed == 1 ? "" : "s", Minimum, Minimum == 1 ? "" : "s");
                Passed = false;
                TimesPressed = 0;
            }
            if (Bomb.GetSolvableModuleNames().Count() == Bomb.GetSolvedModuleNames().Count() && _moduleID == _highestID)
            {
                try
                {
                    Sounds[0].StopSound();
                }
                catch { }
                try
                {
                    Sounds[2].StopSound();
                }
                catch { }
                Sounds[0] = Audio.HandlePlaySoundAtTransformWithRef("solve", transform, false);
                break;
            }
        }
    }

    private IEnumerator WaitForSoundLength(float length)
    {
        Interrupted = false;
        float duration = length;
        while (duration > 0 && !Interrupted)
        {
            yield return null;
            duration -= Time.deltaTime;
            if (_interrupted && _moduleID == _highestID && !Interrupted)
            {
                try
                {
                    Sounds[0].StopSound();
                }
                catch { }
                Sounds[2] = Audio.HandlePlaySoundAtTransformWithRef("interrupt", transform, false);
            }
            if (_interrupted && !Interrupted)
            {
                Interrupted = true;
                break;
            }
        }
        Interrupted = true;
    }

    private IEnumerator Strike()
    {
        float timer = 0.175f;
        for (int i = 0; i < 2; i++)
        {
            Pin.material.color = new Color(1, 0, 0);
            while (timer > 0)
            {
                yield return null;
                timer -= Time.deltaTime;
            }
            timer = 0.175f;
            Pin.material.color = new Color();
            while (timer > 0)
            {
                yield return null;
                timer -= Time.deltaTime;
            }
            timer = 0.175f;
        }
    }

    private IEnumerator AnimLegacyMode(float delay = 0.25f, float duration = 1f)
    {
        float timer = 0;
        while (timer < delay)
        {
            yield return null;
            timer += Time.deltaTime;
        }
        timer = 0;
        while (timer < duration)
        {
            yield return null;
            timer += Time.deltaTime;
            LegacyModeText.transform.localPosition = new Vector3(Easing.OutExpo(timer, -0.06f, -0.015f, duration), LegacyModeText.transform.localPosition.y, LegacyModeText.transform.localPosition.z);
        }
        LegacyModeText.transform.localPosition = new Vector3(-0.015f, LegacyModeText.transform.localPosition.y, LegacyModeText.transform.localPosition.z);
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} press 30' to press the button 30 times.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        string[] CommandArray = command.Split(' ');
        if (CommandArray.Length != 2 || CommandArray[0] != "press")
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }
        bool bad = false;
        try
        {
            int.Parse(CommandArray[1]);
        }
        catch (FormatException e)
        {
            bad = true;
        }
        if (bad)
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }
        for (int i = 0; i < int.Parse(CommandArray[1]); i++)
        {
            ButtonSelectable.OnInteract();
            while (Pressing)
                yield return "trycancel The 8 button has stopped being pressed, due to a request to cancel.";
        }

    }
    IEnumerator TwitchHandleForcedSolve()
    {
        Autosolve = true;
        Minimum = Bomb.GetSolvableModuleNames().Where(x => !IgnoredModules.Contains(x)).Count() + 1;
        Debug.LogFormat("[8 #{0}] Autosolving...", _moduleID);
        Pin.material.color = new Color();
        while (!(Bomb.GetSolvableModuleNames().Where(x => !IgnoredModules.Contains(x)).Count() == Bomb.GetSolvedModuleNames().Where(x => !IgnoredModules.Contains(x)).Count()))
            yield return true;
        while (!Solved)
        {
            yield return true;
            ButtonSelectable.OnInteract();
            while (Pressing)
                yield return true;
        }
    }
}
