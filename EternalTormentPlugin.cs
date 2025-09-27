using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using GlobalSettings;

namespace EternalTorment;

[BepInAutoPlugin("io.github.eternaltorment","Eternal Torment","1.2.0")]
public partial class EternalTormentPlugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    public static ConfigEntry<bool> rosaryPreservation;
    public static ConfigEntry<bool> shardsRecycling;
    public static int Money = 0;
    private void Awake()
    {
        Logger = base.Logger;
        rosaryPreservation = Config.Bind(Name,"Rosary Preservation",false,"Keep part of your rosaries on death. % based on your max health.");
        shardsRecycling = Config.Bind(Name, "Shards Recycling", false, "Gain shards when breaking your cocoon. 5 times your max health.");
        Harmony harmony = new(Id);
        harmony.PatchAll();
    }
}

[HarmonyPatch(typeof(PlayerData), nameof(PlayerData.TakeHealth))]
class TakeHealth_Patch
{
    static void Prefix(ref int amount)
    {
        amount = 999;
        EternalTormentPlugin.Logger.LogInfo($"Torment is eternal");
    }
}

[HarmonyPatch(typeof(HeroController), nameof(HeroController.CocoonBroken), new Type[] {typeof(bool),typeof(bool)})]
class CocoonBroken_Patch
{
    static void Prefix(HeroController __instance)
    {
        int heroCorpseMoneyPool = __instance.playerData.HeroCorpseMoneyPool;
        EternalTormentPlugin.Logger.LogInfo($"Cocoon has {heroCorpseMoneyPool} rosaries.");
        if (EternalTormentPlugin.shardsRecycling.Value)
        {
            int shards = __instance.playerData.maxHealth * 5;
            CurrencyManager.AddCurrency(shards, CurrencyType.Shard, true);
            EternalTormentPlugin.Logger.LogInfo($"Cocoon has {shards} shards.");
        }
    }
}

[HarmonyPatch(typeof(HeroController), nameof(HeroController.Die))]
class Die_Patch
{
    static void Prefix(HeroController __instance)
    {
        EternalTormentPlugin.Money = __instance.playerData.geo;
        EternalTormentPlugin.Logger.LogInfo($"Money is {EternalTormentPlugin.Money}.");
    }
}

[HarmonyPatch(typeof(HeroController), nameof(HeroController.Respawn))]
class Respawn_Patch
{
    static void Prefix(HeroController __instance)
    {
        bool wasDead = __instance.cState.dead;
        if (wasDead && EternalTormentPlugin.rosaryPreservation.Value)
        {
            int money = EternalTormentPlugin.Money;
            int modifier = __instance.playerData.maxHealth;
            bool isEquipped = Gameplay.DeadPurseTool.IsEquipped;
            if (isEquipped) modifier = 10;
            int keep = Mathf.RoundToInt((float)money * (float)modifier / 10f);
            money -= keep;
            __instance.playerData.geo = keep;
            __instance.playerData.HeroCorpseMoneyPool = Mathf.RoundToInt((float)money);
            EternalTormentPlugin.Logger.LogInfo($"Kept {keep} rosaries.");
        }
    }
}