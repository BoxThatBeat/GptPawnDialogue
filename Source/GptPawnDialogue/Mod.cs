﻿using System;
using Verse;
using HarmonyLib;

namespace GptPawnDialogue
{
    public sealed class Mod : Verse.Mod
    {
        public const string Id = "GptPawnDialogue";
        public const string Name = "GPT Pawn Dialogue";
        public const string Version = "1.0";

        public static Mod Instance = null;

        public Mod(ModContentPack content) : base(content)
        {
            Instance = this;

            new Harmony(Id).PatchAll();

            Log("Initialized");
        }

        public static void Log(string message) => Verse.Log.Message(PrefixMessage(message));
        public static void Warning(string message) => Verse.Log.Warning(PrefixMessage(message));
        public static void Error(string message) => Verse.Log.Error(PrefixMessage(message));
        private static string PrefixMessage(string message) => $"[{Name} v{Version}] {message}";
    }
}
