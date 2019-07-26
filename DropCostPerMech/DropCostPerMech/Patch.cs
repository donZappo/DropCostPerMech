using BattleTech;
using BattleTech.Framework;
using BattleTech.Save;
using BattleTech.Save.SaveGameStructure;
using BattleTech.UI;
using Harmony;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DropCostPerMech
{
    //[HarmonyPatch(typeof(GameInstanceSave), MethodType.Constructor)]
    //[HarmonyPatch(new Type[] { typeof(GameInstance), typeof(SaveReason) })]
    //public static class GameInstanceSave_Constructor_Patch {
    //    static void Postfix(GameInstanceSave __instance)
    //    {
    //        if (UnityGameInstance.BattleTechGame.Simulation.Constants.Story.MaximumDebt == 42)
    //            Helper.SaveState(__instance.InstanceGUID, __instance.SaveTime);
    //    }
    //}


    //[HarmonyPatch(typeof(GameInstance), "Load")]
    //public static class GameInstance_Load_Patch {
    //    static void Prefix(GameInstanceSave save)
    //    {
    //        if (UnityGameInstance.BattleTechGame.Simulation.Constants.Story.MaximumDebt == 42)
    //            Helper.LoadState(save.InstanceGUID, save.SaveTime);
    //    }
    //}
    
    [HarmonyPatch(typeof(AAR_ContractObjectivesWidget), "FillInObjectives")]
    public static class AAR_ContractObjectivesWidget_FillInObjectives {

        static void Postfix(AAR_ContractObjectivesWidget __instance)
        {
            if (UnityGameInstance.BattleTechGame.Simulation != null && UnityGameInstance.BattleTechGame.Simulation.Constants.Story.MaximumDebt == 42)
            {
                var consumedMilestones = Traverse.Create(UnityGameInstance.BattleTechGame.Simulation)
                .Field("ConsumedMilestones").GetValue<List<string>>();
                var consumed = consumedMilestones.Contains("milestone_111_talk_rentToOwn");

                if (consumed || UnityGameInstance.BattleTechGame.Simulation.IsCareerMode())
                {
                    try
                    {
                        Settings settings = Helper.LoadSettings();

                        string missionObjectiveResultString = $"DROP COSTS DEDUCTED: ¢{Fields.FormattedDropCost}";
                        if (settings.CostByTons && !settings.NewAlgorithm)
                        {
                            missionObjectiveResultString += $"{Environment.NewLine}AFTER {settings.freeTonnageAmount} TON CREDIT WORTH ¢{string.Format("{0:n0}", settings.freeTonnageAmount * settings.cbillsPerTon)}";
                        }
                        MissionObjectiveResult missionObjectiveResult = new MissionObjectiveResult(missionObjectiveResultString, "7facf07a-626d-4a3b-a1ec-b29a35ff1ac0", false, true, ObjectiveStatus.Succeeded, false);
                        ReflectionHelper.InvokePrivateMethode(__instance, "AddObjective", new object[] { missionObjectiveResult });
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(Contract), "CompleteContract")]
    public static class Contract_CompleteContract {

        static void Postfix(Contract __instance)
        {
            if (UnityGameInstance.BattleTechGame.Simulation != null && UnityGameInstance.BattleTechGame.Simulation.Constants.Story.MaximumDebt == 42)
            {
                var consumedMilestones = Traverse.Create(UnityGameInstance.BattleTechGame.Simulation)
                .Field("ConsumedMilestones").GetValue<List<string>>();
                var consumed = consumedMilestones.Contains("milestone_111_talk_rentToOwn");
                Settings settings = Helper.LoadSettings();

                if (consumed || UnityGameInstance.BattleTechGame.Simulation.IsCareerMode())
                {
                    try
                    {
                        int newMoneyResults = Mathf.FloorToInt(__instance.MoneyResults - Fields.DropCost);
                        ReflectionHelper.InvokePrivateMethode(__instance, "set_MoneyResults", new object[] { newMoneyResults });
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(LanceHeaderWidget), "RefreshLanceInfo")]
    public static class LanceHeaderWidget_RefreshLanceInfo {    
        static void Postfix(LanceHeaderWidget __instance, List<MechDef> mechs)
        {
            if (UnityGameInstance.BattleTechGame.Simulation != null && UnityGameInstance.BattleTechGame.Simulation.Constants.Story.MaximumDebt == 42)
            {
                try
                {
                    Settings settings = Helper.LoadSettings();
                    string freeTonnageText = "";

                    LanceConfiguratorPanel LC = (LanceConfiguratorPanel)ReflectionHelper.GetPrivateField(__instance, "LC");
                    if (LC.IsSimGame)
                    {
                        int lanceTonnage = 0;
                        float dropCost = 0f;
                        if (settings.CostByTons)
                        {
                            foreach (MechDef def in mechs)
                            {
                                dropCost += (def.Chassis.Tonnage * settings.cbillsPerTon);
                                lanceTonnage += (int)def.Chassis.Tonnage;
                            }
                        }
                        else
                        {
                            foreach (MechDef def in mechs)
                            {
                                dropCost += (Helper.CalculateCBillValue(def) * settings.percentageOfMechCost);
                                lanceTonnage += (int)def.Chassis.Tonnage;
                            }
                        }
                        if (settings.CostByTons && settings.someFreeTonnage && !settings.NewAlgorithm)
                        {
                            freeTonnageText = $"({settings.freeTonnageAmount} TONS FREE)";
                            dropCost = Math.Max(0f, (lanceTonnage - settings.freeTonnageAmount) * settings.cbillsPerTon);
                        }
                        else if (settings.CostByTons && settings.someFreeTonnage && settings.NewAlgorithm)
                        {
                            freeTonnageText = $"({settings.freeTonnageAmount} TONS FREE)";
                            if (lanceTonnage >= settings.freeTonnageAmount)
                            {
                                dropCost = (lanceTonnage - settings.freeTonnageAmount) * (lanceTonnage - settings.freeTonnageAmount) * settings.cbillsPerTon / settings.freeTonnageAmount;
                            }
                            else
                            {
                                dropCost = 0;
                            }
                        }

                        string formattedDropCost = string.Format("{0:n0}", dropCost);
                        Fields.DropCost = dropCost;
                        Fields.LanceTonnage = lanceTonnage;
                        Fields.FormattedDropCost = formattedDropCost;
                        Fields.FreeTonnageText = freeTonnageText;

                        TextMeshProUGUI simLanceTonnageText = (TextMeshProUGUI)ReflectionHelper.GetPrivateField(__instance, "simLanceTonnageText");
                        // longer strings interfere with messages about incorrect lance configurations
                        simLanceTonnageText.text = $"DROP COST: \n¢{Fields.FormattedDropCost}\nWEIGHT:\n{Fields.LanceTonnage} TONS";
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError(e);
                }
            }
        }
    }
}