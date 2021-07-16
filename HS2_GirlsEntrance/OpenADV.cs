﻿using System;
using System.Collections.Generic;
using Manager;
using Actor;
using ADV;
using AIChara;
using Illusion.Anime;
using UniRx;

namespace HS2_GirlsEntrance
{
    //Open an adv file in lobby
    class OpenADV
    {
        public async void OpenADVScene(string _bundle, string _asset, Heroine _heroine, LobbySceneManager scene, Action _onEnd = null)
        {
            //Indicates that the animation is playing
            HS2_GirlsEntrance.isPlaying = true;

            this.ConciergeChaCtrl = Singleton<Character>.Instance.GetChara(-1);
            this.ConciergeHeroine = new Heroine(this.ConciergeChaCtrl.chaFile, false)
            {
                fixCharaID = -1
            };
            this.ConciergeHeroine.SetRoot(this.ConciergeChaCtrl.gameObject);

            //await Setup.LoadAsync(base.transform);
            await Setup.LoadAsync(scene.transform);

            this.isNowADV.Value = true;
            Game instance = Singleton<Game>.Instance;
            this.packData = new OpenADV.PackData();
            this.packData.SetCommandData(new ICommandData[]
            {
                instance.saveData
            });
            this.packData.SetParam(new IParams[]
            {
                this.ConciergeHeroine,
                _heroine
            });
            this.packData.personal = _heroine.personality;
            this.packData.isParent = false;
            this.openData.bundle = _bundle;
            this.openData.asset = _asset;
            this.packData.onComplete = delegate ()
            {
                this.isNowADV.Value = false;
                this.isADVShow = true;
                Action onEnd = _onEnd;
                if (onEnd != null)
                {
                    onEnd();
                }
                Controller.Table.Get(this.ConciergeChaCtrl).itemHandler.DisableItems();

                scene.StartFade(false);
                scene.SetCharaAnimationAndPosition();
                HS2_GirlsEntrance.isPlaying = false;
            };

            Setup.Open(this.openData, this.packData, true, true, true, false);
        }
        
        private class PackData : CharaPackData
        {
            public int personal { get; set; }
            public override List<Program.Transfer> Create()
            {
                List<Program.Transfer> list = base.Create();
                list.Add(Program.Transfer.VAR(new string[]
                {
                    "int",
                    "personal",
                    this.personal.ToString()
                }));
                return list;
            }
        }

        private OpenADV.PackData packData { get; set; }
        private OpenData openData = new OpenData();
        private BoolReactiveProperty isNowADV = new BoolReactiveProperty(false);
        private bool isADVShow;
        public Heroine ConciergeHeroine { get; private set; }
        public ChaControl ConciergeChaCtrl { get; private set; }
        private Controller ConciergeCtrl;

        private ValueDictionary<int, int, List<TitleCharaStateInfo.Param>> dicCharaState = new ValueDictionary<int, int, List<TitleCharaStateInfo.Param>>();
    }
}
