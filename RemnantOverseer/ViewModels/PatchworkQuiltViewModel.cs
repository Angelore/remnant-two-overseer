using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;

namespace RemnantOverseer.ViewModels;
internal class PatchworkQuiltViewModel: ViewModelBase
{
    public QuiltPatch[] QuiltPatches { get; private set; } = [];

    public PatchworkQuiltViewModel(List<string> questCompletedLog)
    {
        if (Design.IsDesignMode)
        {
            questCompletedLog = new List<string>()
            {
                "Quest_SideD_ThreeMenMorris",
                "Quest_Boss_NightWeaver",
                //"Quest_SideD_CharnelHouse",
                "Quest_Miniboss_BloatKing",
                "Quest_Miniboss_FaeArchon",
                "Quest_SideD_FaeCouncil",
                "Quest_SideD_TownTurnToDust",
                //"Quest_Miniboss_DranGrenadier",
                "Quest_Boss_Faerlin",
                "Quest_SideD_Ravenous",
                "Quest_Miniboss_RedPrince",
                "Quest_SideD_CrimsonHarvest"
            };
        }
        InitPatchworkQuilt(questCompletedLog);
    }

    private void InitPatchworkQuilt(List<string> questCompletedLog)
    {
        QuiltPatches =
        [
            new QuiltPatch()
            {
                Name = "Postulant's Parlor",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_ThreeMensMorris.png",
                QuestId = "Quest_SideD_ThreeMenMorris"
            },
            new QuiltPatch()
            {
                Name = "The Nightweaver",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_Nightweaver.png",
                QuestId = "Quest_Boss_NightWeaver"
            },
            new QuiltPatch()
            {
                Name = "Harvester's Reach",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_BoneHarvester.png",
                QuestId = "Quest_SideD_CharnelHouse"
            },
            new QuiltPatch()
            {
                Name = "Bloat King",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_BloatKing.png",
                QuestId = "Quest_Miniboss_BloatKing"
            },
            new QuiltPatch()
            {
                Name = "Magister Dullain",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_Archon.png",
                QuestId = "Quest_Miniboss_FaeArchon"
            },
            new QuiltPatch()
            {
                Name = "Council Tribunal",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_Council.png",
                QuestId = "Quest_SideD_FaeCouncil"
            },
            new QuiltPatch()
            {
                Name = "Tiller's Rest",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_TownTurnedDust.png",
                QuestId = "Quest_SideD_TownTurnToDust"
            },
            new QuiltPatch()
            {
                Name = "Gwendil: The Unburnt",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_Grenadier.png",
                QuestId = "Quest_Miniboss_DranGrenadier"
            },
            new QuiltPatch()
            {
                Name = "Imposter King",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_FaelinFaerin.png",
                QuestId = "Quest_Boss_Faelin"
            },
            new QuiltPatch()
            {
                Name = "The Great Hall",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_Ravenous.png",
                QuestId = "Quest_SideD_Ravenous"
            },
            new QuiltPatch()
            {
                Name = "The Red Prince",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_RedPrince.png",
                QuestId = "Quest_Miniboss_RedPrince"
            },
            new QuiltPatch()
            {
                Name = "Butcher's Quarter",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_Burning.png",
                QuestId = "Quest_SideD_CrimsonHarvest"
            },
        ];

        foreach (var patch in QuiltPatches)
        {
            patch.IsCompleted = questCompletedLog.Contains(patch.QuestId);
        }
        // Special case for Faelin/Faer[l]in
        if (!QuiltPatches[8].IsCompleted)
            QuiltPatches[8].IsCompleted = questCompletedLog.Contains("Quest_Boss_Faerlin");
    }
}

internal class QuiltPatch
{
    public required string Name { get; set; }
    public bool IsCompleted { get; set; }
    public required string ImagePath { get; set; }
    public required string QuestId { get; set; }

    public Bitmap Image => new(AssetLoader.Open(new Uri(ImagePath)));
}
