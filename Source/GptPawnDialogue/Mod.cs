using HarmonyLib;
using System;
using System.Threading;
using UnityEngine;
using Verse;

namespace GptPawnDialogue
{
    public sealed class Mod : Verse.Mod
    {
        public const string Id = "GptPawnDialogue";
        public const string Name = "GPT Pawn Dialogue";
        public const string Version = "1.0";

        public static CancellationTokenSource onQuit = new();

        public static Mod Instance = null;

        public Mod(ModContentPack content) : base(content)
        {
            Instance = this;

            new Harmony(Id).PatchAll();

            Application.wantsToQuit += () =>
            {
                onQuit.Cancel();
                return true;
            };

            Logger.Message("Initialized");
        }

        public static bool Running => onQuit.IsCancellationRequested == false;
    }
}
