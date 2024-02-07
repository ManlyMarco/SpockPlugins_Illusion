// System

using System;

// BepInEx
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
#if HC
using TMPro;
#endif

// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// This file is not game specific. Its responsible to manage the Subtitle Canvas and text.
namespace IllusionPlugins;

[BepInProcess(Constants.MainGameProcessName)]
[BepInPlugin(GUID, PluginName, Version)]
public partial class Subtitles : BasePlugin
{
    // Plugin consts
    public const string GUID = "SpockBauru.Subtitles";
    public const string PluginName = "Subtitles";
    public const string Version = "0.1";
    public const string PluginNameInternal = Constants.Prefix + "_Subtitles";

    // BepInEx Config
    private static ConfigEntry<bool> EnableConfig;

    // Plugin variables
    private static GameObject canvasObject;
    private static new BepInEx.Logging.ManualLogSource Log;

    public override void Load()
    {
        Log = base.Log;

        EnableConfig = Config.Bind("General",
                                   "Enable Subtitles",
                                   true,
                                   "Reload the game to Enable/Disable");

        if (EnableConfig.Value)
            Harmony.CreateAndPatchAll(typeof(Hooks), GUID);

        // IL2CPP don't automatically inherits MonoBehaviour, so needs to add a component separatelly
        ClassInjector.RegisterTypeInIl2Cpp<SubtitlesCanvas>();
    }

    /// <summary>
    /// Create the subtitle canvas in the desired scene
    /// </summary>
    /// <param name="scene"></param>
    public static void MakeCanvas(Scene scene)
    {
        if (canvasObject != null) 
            Object.Destroy(canvasObject);

        // Creating Canvas object
        canvasObject = new GameObject("SubtitleCanvas");
        SceneManager.MoveGameObjectToScene(canvasObject, scene);
        canvasObject.AddComponent<SubtitlesCanvas>();
    }

    public class SubtitlesCanvas : MonoBehaviour
    {
        // Constructor needed to use Start, Update, etc...
        public SubtitlesCanvas(IntPtr handle) : base(handle) { }

        private static GameObject subtitleObject;
#if RG
        private static Text subtitle;
#elif HC
        private static TextMeshProUGUI subtitle;
#endif

        private static float time = 0;
        private static float clipLenght = 0;

        private void Start()
        {
            // Setting canvas attributes
            var canvasScaler = canvasObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            canvasObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

            // Setting subtitle object
            subtitleObject = new GameObject("SubtitleText");
            subtitleObject.transform.SetParent(canvasObject.transform);

#if RG
            var fontSize = (int)(Screen.height / 25.0f);

            var subtitleRect = subtitleObject.AddComponent<RectTransform>();
            subtitleRect.pivot = new Vector2(0, -1);
            subtitleRect.sizeDelta = new Vector2(Screen.width * 0.990f, fontSize + fontSize * 0.05f);

            subtitle = subtitleObject.AddComponent<Text>();
            subtitle.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            subtitle.fontSize = fontSize;
            subtitle.fontStyle = FontStyle.Bold;
            subtitle.alignment = TextAnchor.LowerCenter;
            subtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitle.verticalOverflow = VerticalWrapMode.Overflow;
            subtitle.color = Color.white;
            subtitle.text = "";

            var outline = subtitleObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(2.0f, -2.0f);
#elif HC
            var fontSize = (int)(Screen.height / 30.0f);

            var subtitleRect = subtitleObject.AddComponent<RectTransform>();
            subtitleRect.pivot = new Vector2(0, -1);
            subtitleRect.sizeDelta = new Vector2(Screen.width * 0.990f, fontSize + fontSize * 0.05f);

            static T getResource<T>(string name) where T : Object
            {
                var objs = Resources.FindObjectsOfTypeAll(Il2CppType.Of<T>());
                for (var i = objs.Length - 1; i >= 0; --i)
                {
                    var obj = objs[i];
                    if (obj.name == name)
                    {
                        var ret = obj.TryCast<T>();
                        return ret;
                    }
                }

                return null;
            }

            subtitle = subtitleObject.AddComponent<TextMeshProUGUI>();
            subtitle.font = getResource<TMP_FontAsset>("tmp_hc_default");
            subtitle.fontSharedMaterial = getResource<Material>("tmp_hc_default t_outline_bold");

            subtitle.fontSize = fontSize;
            subtitle.alignment = TextAlignmentOptions.Bottom;
            subtitle.overflowMode = TextOverflowModes.Overflow;
            subtitle.enableWordWrapping = true;
            subtitle.color = Color.white;
            subtitle.text = "";
#endif
        }

        /// <summary>
        /// Display the subtitle text while the voiceFile is active
        /// </summary>
        /// <param name="voiceFile"></param>
        /// <param name="text"></param>
        public static void DisplaySubtitle(AudioSource voiceFile, string text)
        {
            subtitle.text = text;
            clipLenght = voiceFile.clip.length;
            time = 0;
        }

        // Using Update because coroutines, onDestroy and onDisable are not working as intended
        private void Update()
        {
            if (time < clipLenght) subtitleObject.active = true;
            else subtitleObject.active = false;
            time += Time.deltaTime;
        }
    }
}
