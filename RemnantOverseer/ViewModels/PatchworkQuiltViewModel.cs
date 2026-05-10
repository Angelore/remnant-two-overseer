using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using RemnantOverseer.Models.Messages;
using RemnantOverseer.Services;
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
        IsActive = true;
    }

    private void InitPatchworkQuilt(List<string> questCompletedLog)
    {
        QuiltPatches =
        [
            new QuiltPatch()
            {
                ResourceKey = "Patchwork_Quest_SideD_ThreeMenMorris",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_ThreeMensMorris.png",
                QuestId = "Quest_SideD_ThreeMenMorris"
            },
            new QuiltPatch()
            {
                ResourceKey = "Patchwork_Quest_Boss_NightWeaver",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_Nightweaver.png",
                QuestId = "Quest_Boss_NightWeaver"
            },
            new QuiltPatch()
            {
                ResourceKey = "Patchwork_Quest_SideD_CharnelHouse",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_BoneHarvester.png",
                QuestId = "Quest_SideD_CharnelHouse"
            },
            new QuiltPatch()
            {
                ResourceKey = "Patchwork_Quest_Miniboss_BloatKing",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_BloatKing.png",
                QuestId = "Quest_Miniboss_BloatKing"
            },
            new QuiltPatch()
            {
                ResourceKey = "Patchwork_Quest_Miniboss_FaeArchon",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_Archon.png",
                QuestId = "Quest_Miniboss_FaeArchon"
            },
            new QuiltPatch()
            {
                ResourceKey = "Patchwork_Quest_SideD_FaeCouncil",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_Council.png",
                QuestId = "Quest_SideD_FaeCouncil"
            },
            new QuiltPatch()
            {
                ResourceKey = "Patchwork_Quest_SideD_TownTurnToDust",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_TownTurnedDust.png",
                QuestId = "Quest_SideD_TownTurnToDust"
            },
            new QuiltPatch()
            {
                ResourceKey = "Patchwork_Quest_Miniboss_DranGrenadier",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_Grenadier.png",
                QuestId = "Quest_Miniboss_DranGrenadier"
            },
            new QuiltPatch()
            {
                ResourceKey = "Patchwork_Quest_Boss_Faelin",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_FaelinFaerin.png",
                QuestId = "Quest_Boss_Faelin"
            },
            new QuiltPatch()
            {
                ResourceKey = "Patchwork_Quest_SideD_Ravenous",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_Ravenous.png",
                QuestId = "Quest_SideD_Ravenous"
            },
            new QuiltPatch()
            {
                ResourceKey = "Patchwork_Quest_Miniboss_RedPrince",
                ImagePath = "avares://RemnantOverseer/Assets/Images/Quilt/T_RedPrince.png",
                QuestId = "Quest_Miniboss_RedPrince"
            },
            new QuiltPatch()
            {
                ResourceKey = "Patchwork_Quest_SideD_CrimsonHarvest",
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

    protected override void OnActivated()
    {
        Messenger.Register<PatchworkQuiltViewModel, CultureChangedMessage>(this, (r, m) => {
            foreach (var patch in r.QuiltPatches)
            {
                patch.RefreshLocalizedProperties();
            }
        });
    }
}

internal class QuiltPatch : ObservableObject
{
    public required string ResourceKey { get; set; }
    public string Name => LocalizationService.Get(ResourceKey);
    public bool IsCompleted { get; set; }
    public required string ImagePath { get; set; }
    public required string QuestId { get; set; }

    public Bitmap Image => new(AssetLoader.Open(new Uri(ImagePath)));

    public void RefreshLocalizedProperties()
    {
        OnPropertyChanged(nameof(Name));
    }
}
