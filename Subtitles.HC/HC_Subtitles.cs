using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using ILLGames.Extensions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IllusionPlugins
{
    public partial class Subtitles : BasePlugin
    {
        static Dictionary<string, string> HCVoiceMap = new() {
            {"hc_na_000","もしもし？入てもいかな？"},
            // 入室後
            {"hc_na_001","呼ばれたけど、どうかしたの？"},
            {"hc_na_002","何？何が用事？"},
            {"hc_na_003","ワタシに何が聞きたいの？"},
            // 汎用クリック
            {"hc_na_038","わかったよ、任せて～"},
            {"hc_na_039","ちょっと待っててね"},
            // 汎用クリック（怒り）
            {"hc_na_040","はい、どうぞ！"},
            {"hc_na_041","これでいいのね？"},
            // 怒り
            {"hc_na_042","ダメじゃん～！次やったら許さないからね！"},
            // 戻る
            {"hc_na_043","それじゃ、またね！"},
            {"hc_na_044","じゃぁ、また何があったら呼んでいいからね～"},
            {"hc_na_045","それじゃ、楽しいん時間を過ごしてね～"},
            {"hc_na_046","また来てね～"},
            {"hc_na_047","いってらっしゃい～"},
            {"hc_na_048","じゃあ～まだね～"},
            // 戻る（怒り）
            {"hc_na_049","ワタシ、もう帰るけど、この次こんな事したら本当に怒るからね！"},
            {"hc_na_050","ワタシが本気で怒る前に、早く帰って方がいいよ……"},
            // ??
            {"hc_na_051","どうかした？いきなり何が困ったことがあるの？"},
            {"hc_na_052","それじゃ、ササと簡単な説明するね～"},
            {"hc_na_053","まずは、へルプについてだけど"},
            {"hc_na_054","分からないことどか、困ったことがあったら、説明書が出すから、ここから確認してね"},
            {"hc_na_055","実績解除について知りたいの？"},
            {"hc_na_056","リブートだけが認められた追加機能に使うのに、制限解除が必要なんだけどね"},
            {"hc_na_057","普通だたら使えないけど、ワタシに何がくれるなら、解除してあげでもいいよ？"},
            {"hc_na_058","そうだな～だたら、ジンジャークッキーが欲しいな～"},
            {"hc_na_059","ワタシ、ジンジャークッキーが好きだから、もしもらえたなら、アナタでも制限解除してあげるよ！"},
            {"hc_na_060","他に用があるなら言ってね～"},
            // 実績
            {"hc_na_061","今のアナタの実績が確認できるよ"},
            // ヘルプ
            {"hc_na_062","操作方法とかの確認ね～任せて～何でも教えてあげる"},
            // エッチ
            {"hc_na_063","いいよ～そうなにワタシエッチしたいなら、付き合ってあげる"},
            {"hc_na_064","ワタシとエッチするのが我慢出来ないだ……わかるよ～ワタシの魅了にハマっちゃったよね～"},
            // 実績（怒り）
            {"hc_na_065","別に～好きにみっていいよ"},
            // ヘルプ（怒り）
            {"hc_na_066","何が困ってるの？しょうがないな～教えてあげてもいいよ"},
            // エッチ（怒り）
            {"hc_na_067","何？エッチのことをしたいんだ、ワタシはその気無いだけとな"},
            {"hc_na_068","ワタシとエッチしたいの？今？もっとちゃんとお願い欲しいですけど"},
            {"hc_na_069","エッチしたいの？今はそゆん気分じゃ無いけどな"},
            {"hc_na_080","はいはい！わかったよ！"},
            {"hc_na_081","いいよ、ちょっと待ってね！"},
        };

        static H.HScene HSceneInstance;
        static HC.Scene.HomeScene HomeSceneInstance;

        static class Hooks
        {
            static string getSubtitle(string audioFile)
            {
                if (HSceneInstance != null)
                {
                    foreach (var list in HSceneInstance.CtrlVoice._dicdiclstVoiceList)
                    {
                        if (list == null) continue;
                        foreach (var mode in list.Values)
                            foreach (var sheet in mode.Values)
                                foreach (var kind in sheet.DicdicVoiceList)
                                    foreach (var voiceID in kind.Values)
                                    {
                                        if (voiceID.NameFile == audioFile)
                                        {
                                            return voiceID.Word;
                                        }
                                    }
                    }
                }
                else if (HomeSceneInstance != null)
                {
                    return HCVoiceMap.GetValueSafe(audioFile);
                }
                return null;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Manager.Voice), nameof(Manager.Voice.PlayStandby))]
            private static void PlayVoice(AudioSource audioSource, Manager.Voice.Loader loader, Il2CppSystem.Action<AudioSource> afterAction)
            {
                if (loader.Asset.IsNullOrEmpty())
                    return;
                string audioFile = loader.Asset;
                var subtitle = getSubtitle(audioFile);
                Log.LogInfo($"Play voice({audioFile}): {subtitle}");
                if (!subtitle.IsNullOrEmpty())
                {
                    SubtitlesCanvas.DisplaySubtitle(audioSource, subtitle);
                }


            }

            [HarmonyPostfix, HarmonyPatch(typeof(H.HScene), nameof(H.HScene.Start))]
            private static void HSceneStart(H.HScene __instance)
            {
                HSceneInstance = __instance;
                MakeCanvas(SceneManager.GetActiveScene());
            }

            [HarmonyPostfix, HarmonyPatch(typeof(HC.Scene.HomeScene), nameof(HC.Scene.HomeScene.Start))]
            private static void HomeSceneSceneStart(HC.Scene.HomeScene __instance)
            {
                HomeSceneInstance = __instance;
                MakeCanvas(SceneManager.GetActiveScene());
            }

            // //// Voice in the beginning - ILCCPP CANT HANDLE THIS METHOD! MANY SUBTITLES MISSING BECAUSE OF IT!!!!!!
            // [HarmonyPostfix, HarmonyPatch(typeof(H.HVoiceCtrl), nameof(H.HVoiceCtrl.GetPlayListNum), 
            //     new Type[] {typeof(List<H.HVoiceCtrl.VoicePtnInfo>), typeof(Dictionary<int, Dictionary<int, H.HVoiceCtrl.VoiceList>>), typeof(List<int>), typeof(int), typeof(Character.Human)}, 
            //     new ArgumentType[] {ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal})]
            // private static void GetDicIncexesBegining(List<H.HVoiceCtrl.PlayVoiceinfo> __result)
            // {
            //     Log.LogDebug($"Hooked {__result}");
            // }

        }

    }
}
