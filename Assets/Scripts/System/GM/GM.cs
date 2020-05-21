﻿using UnityEngine;
using System;
using System.IO;
using UnityEngine.UI;
using System.Collections;
using System.Runtime.InteropServices;
using NetworkSync;

/// <summary>
/// Main Game Mananger class.  Because it is a singleton, when referencing in another
/// script, you need to call it's instance.  Like so: GM.Instance.data etc.
/// </summary>
public class GM : Singleton<GM> {

    public string versionNumber;

    public Runner runner;
    public DataManager data;
    public OptionsManager options;
    public Animator state;
    public Logger logger;
    public NetworkSync.GameSync sync;
    public WinnitronNetwork network;
    public LogOutputHandler logOutput;
    public VideoManager video;
    public StateMachineHelper stateMachineHelper;
    public Jukebox jukebox;

    public Text infoHeader;
    public Text info;
    public Text infoAction;

    //SINGLETON STUFF

    protected GM () { }

    //END SINGLETON STUFF


    void Start() {
        logger.Info("##### VERSION " + versionNumber + " #####");
        SetWindowTitle();
        WriteProcessInfo();
    }

    /// <summary>
    /// Reinitializes all the data in the GM.Instance.
    /// </summary>
    public void Init()
    {
        logger.Debug("GM initializing!");

        Cursor.visible = false;

        //Do Windows window management shizzle
        ResetScreen();

        Instance.StartCoroutine("Initialize");
    }


    IEnumerator Initialize()
    {
        Environment.SetEnvironmentVariable("WINNITRON_LAUNCHER_VERSION", versionNumber, EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable("WINNITRON_IDENTIFIER", ArcadeID(), EnvironmentVariableTarget.User);

        // Let's initialize stuff in order
        // Options are already loaded via Start()
        InfoText("STARTING UP", "Loading Runner...");
        runner.Init();
        InfoText("STARTING UP", "Checking Sync...");
        sync.Init();

        while (!sync.isFinished)
            yield return null;

        InfoText("STARTING UP", "Compiling Games...");
        data.Init();

        InfoText("STARTING UP", "Done!");
        state.SetTrigger("NextState");
    }


    void OnApplicationQuit()
    {
        Environment.SetEnvironmentVariable("WINNITRON_LAUNCHER_VERSION", null, EnvironmentVariableTarget.User);
        Environment.SetEnvironmentVariable("WINNITRON_IDENTIFIER", null, EnvironmentVariableTarget.User);
    }

    /// <summary>
    /// Causes an Oops screen to appear.  This function calls the real Oops in StateManager.cs
    /// </summary>
    /// <param name="text">Text to show on the Oops screen</param>
    /// <param name="isCritical">Critical will force quit the launcher</param>
    public void Oops(string text, bool isCritical)
    {
        GM.Instance.state.SetTrigger("Oops");
        stateMachineHelper.oopsIsCritical = isCritical;
        InfoText("OOPS!", text);
    }

    /// <summary>
    /// Overloaded method that just takes text and assumes non-critical.
    /// </summary>
    /// <param name="text">Text to show.</param>
    public void Oops(string text)
    {
        Oops(text, false);
    }

    /// <summary>
    /// Displays info text.
    /// </summary>
    /// <param name="text"></param>
    public void InfoText(string header, string text)
    {
        infoHeader.text = header;
        info.text = text;
    }

    /// <summary>
    /// Gets text from the apporpriate language file.
    /// </summary>
    /// <param name="category"></param>
    /// <param name="type"></param>
    /// <returns>A plain text string.</returns>
    public string Text(string category, string type)
    {
        return options.GetText (category, type);
    }

    /// <summary>
    /// Restarts the launcher
    /// </summary>
    public void Restart()
    {
        state.SetTrigger("Init");
        ResetScreen();
    }

    private void WriteProcessInfo() {
        string info = System.Diagnostics.Process.GetCurrentProcess().Id +
                      "\n" +
                      Path.Combine(Path.GetFullPath("."), "WINNITRON.bat") +
                      "\n" +
                      GetWindowHandle();

        GM.Instance.logger.Debug("writing pid and exe path to " + PidFile());
        File.WriteAllText(PidFile(), info);
    }


    private string ArcadeID() {
        // TODO: If we have an API key, that means we could fetch all our machine data
        // (name, slug, etc) from the Network.

        string key = options.GetSyncSettings()["apiKey"];
        if (key == "YOUR API KEY HERE")
            key = "";

        return key.Trim();
    }

    //*
    #if UNITY_STANDALONE_WIN

    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    private static extern bool SetWindowPos(IntPtr hwnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
    [DllImport("user32.dll", EntryPoint = "FindWindow")]
    public static extern IntPtr FindWindow(System.String className, System.String windowName);
    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    public static extern long SetWindowLong(long hwnd, long nIndex, long dwNewLong);
    [DllImport("user32.dll", EntryPoint = "SetWindowText")]
    public static extern bool SetWindowText(System.IntPtr hwnd, System.String lpString);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern System.IntPtr GetActiveWindow();

    public static void ResetScreen() {
        SetWindowLong(FindWindow(null, Application.productName).ToInt32(), -16L, 0x00800000L);
        SetWindowPos(FindWindow(null, Application.productName), 0, 0, 0, Screen.currentResolution.width, Screen.currentResolution.height, 0x0020);
        Screen.fullScreen = true;
    }

    public static string PidFile() {
        string drive = Environment.GetEnvironmentVariable("HOMEDRIVE");
        string path = Environment.GetEnvironmentVariable("HOMEPATH");

        return Path.Combine(drive + path, "winnitron.pid");
    }

    public static System.IntPtr GetWindowHandle() {
      return GetActiveWindow();
    }

    private void SetWindowTitle() {
        System.IntPtr window = GetWindowHandle();
        SetWindowText(window, "WinnitronLauncher!");
    }

    #else

    public static void ResetScreen() {
    }

    public static string PidFile() {
        string path = Environment.GetEnvironmentVariable("HOME");
        return Path.Combine(path, "winnitron.pid");
    }

    public static System.IntPtr GetWindowHandle() {
      return System.IntPtr.Zero;
    }

    private void SetWindowTitle() {
        // NOOP
    }

    #endif
    //*/

}