using CommunityToolkit.Mvvm.Messaging;
using lib.remnant2.analyzer;
using lib.remnant2.analyzer.Enums;
using lib.remnant2.analyzer.Model;
using lib.remnant2.analyzer.Model.Mechanics;
using lib.remnant2.analyzer.Model.Prism;
using lib.remnant2.analyzer.SaveLocation;
using lib.remnant2.saves.Model.Memory;
using RemnantOverseer.Models.Messages;
using RemnantOverseer.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace RemnantOverseer.Services;
public class SaveDataService
{
    private readonly SettingsService _settingsService;
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
    private readonly object _loadFailureNotificationLock = new();
    Subject<DateTime> _fileUpdateSubject = new Subject<DateTime>();
    private Dataset? _dataset;
    private int _lastCharacterCount = 0;
    private string? _lastLoadFailureNotification;
    private string? FilePath { get { return _settingsService.Get().SaveFilePath; } }
    private static readonly FileSystemWatcher FileWatcher = new();

    public SaveDataService(SettingsService settingsService)
    {
        _settingsService = settingsService;

        FileWatcher.Changed += OnSaveFileChanged;
        FileWatcher.Created += OnSaveFileChanged;
        FileWatcher.Deleted += OnSaveFileChanged;

        // File watcher often raises multiple events for one file update
        _fileUpdateSubject.Throttle(TimeSpan.FromSeconds(1)).Subscribe(async events => await OnSaveFileChangedDebounced());
        WeakReferenceMessenger.Default.Register<SaveFilePathChangedMessage>(this, async (r, m) => await SaveFilePathChangedMessageHandler(m));
    }

    public async Task<Dataset?> GetSaveData()
    {
        if (FilePath == null)
        {
            try
            {
                _settingsService.Get().SaveFilePath = SaveUtils.GetSaveFolder();
                await _settingsService.Sync();
                WeakReferenceMessenger.Default.Send(new NotificationInfoMessage(NotificationStrings.DefaultLocationFound));
                Log.Instance.Information(NotificationStrings.DefaultLocationFound);
            }
            catch
            {
                var message = NotificationStrings.DefaultLocationNotFound;
                if (ShouldShowLoadFailureNotification(message))
                {
                    WeakReferenceMessenger.Default.Send(new NotificationWarningMessage(message));
                }
                Log.Instance.Warning(message);
                return null;
            }
        }

        var dataset = await LoadSaveData(false);

        if (dataset == null)
        {
            WeakReferenceMessenger.Default.Send(new DatasetIsNullMessage());
        }
        else
        {
            _lastCharacterCount = dataset.Characters.Count;
            WeakReferenceMessenger.Default.Send(new DatasetParsedMessage());
        }

        return dataset;
    }

    public void Reset()
    {
        _dataset = null;
        ResetLoadFailureNotification();
    }

    public bool StartWatching()
    {
        if (FilePath is null) return false;

        if (Directory.Exists(FilePath))
        {
            var file = Path.GetFileName(SaveUtils.GetSavePath(FilePath, "profile"));
            if (file is null)
            {
                WeakReferenceMessenger.Default.Send(new NotificationErrorMessage(NotificationStrings.FileWatcherFileNotFound));
                Log.Instance.Error(NotificationStrings.FileWatcherFileNotFound);
                return false;
            }
            FileWatcher.Filter = file;
            FileWatcher.Path = FilePath;
            FileWatcher.EnableRaisingEvents = true;
            Log.Instance.Information($"Started watching at {FilePath}");
            return true;
        }
        else
        {
            FileWatcher.EnableRaisingEvents = false;
            WeakReferenceMessenger.Default.Send(new NotificationErrorMessage(NotificationStrings.FileWatcherFolderNotFound));
            Log.Instance.Error(NotificationStrings.FileWatcherFolderNotFound);
            return false;
        }
    }
    public void PauseWatching()
    {
        FileWatcher.EnableRaisingEvents = false;
        Log.Instance.Information($"Stopped watching at {FilePath}");
    }
    public void ResumeWatching()
    {
        if (FileWatcher.Path == null) return;
        FileWatcher.EnableRaisingEvents = true;
        Log.Instance.Information($"Resumed watching at {FilePath}");
    }

    private void OnSaveFileChanged(object sender, FileSystemEventArgs e)
    {
        _fileUpdateSubject.OnNext(DateTime.UtcNow);
    }

    private async Task OnSaveFileChangedDebounced()
    {
        var dataset = await LoadSaveData(true);
        if (dataset == null) return;

        // If the number of character changed, we can't rely on previous index anymore. There is no way to uniquely id  characters, so we will just reset
        var countChanged = dataset.Characters.Count != _lastCharacterCount;
        _lastCharacterCount = dataset.Characters.Count;
        WeakReferenceMessenger.Default.Send(new SaveFileChangedMessage(countChanged));
    }

    private async Task SaveFilePathChangedMessageHandler(SaveFilePathChangedMessage message)
    {
        PauseWatching();
        ResetLoadFailureNotification();
        var dataset = await LoadSaveData(true, message.Value);
        StartWatching();
        if (dataset == null) return;

        _lastCharacterCount = dataset.Characters.Count;
        WeakReferenceMessenger.Default.Send(new SaveFileChangedMessage(true));
    }

    private async Task<Dataset?> LoadSaveData(bool forceRefresh, string? filePath = null)
    {
        // TODO: Add timeout?
        await _semaphore.WaitAsync();
        try
        {
            if (!forceRefresh && _dataset != null)
            {
                return _dataset;
            }

            var dataset = await Task.Run(() => Analyzer.Analyze(filePath ?? FilePath));
            _dataset = dataset;
            ResetLoadFailureNotification();
            return dataset;
        }
        catch (Exception ex)
        {
            var message = $"{NotificationStrings.SaveFileParsingError}{Environment.NewLine}{ex.Message}";
            if (ShouldShowLoadFailureNotification(message))
            {
                WeakReferenceMessenger.Default.Send(new NotificationErrorMessage(message));
            }
            Log.Instance.Error(message);
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private bool ShouldShowLoadFailureNotification(string message)
    {
        lock (_loadFailureNotificationLock)
        {
            if (_lastLoadFailureNotification == message)
            {
                return false;
            }

            _lastLoadFailureNotification = message;
            return true;
        }
    }

    private void ResetLoadFailureNotification()
    {
        lock (_loadFailureNotificationLock)
        {
            _lastLoadFailureNotification = null;
        }
    }

    #region For debug only
    internal async Task ExportSave(string? exportPath)
    {
        if (exportPath is null) throw new ArgumentNullException("File path not set");

        await Task.Run(() => Exporter.Export(exportPath, FilePath, true, true, true));
    }

    internal void ParseSave()
    {
        if (FilePath is null) throw new ArgumentNullException("File path not set");

        var saves = lib.remnant2.analyzer.Analyzer.GetProfileStrings(FilePath);
        Debug.WriteLine(saves);

        var data = lib.remnant2.analyzer.Analyzer.Analyze(FilePath);
        Debug.WriteLine(data);
    }

    internal void ReportPlayerInfo()
    {
        if (_dataset is null)
        {
            WeakReferenceMessenger.Default.Send(new NotificationWarningMessage(NotificationStrings.PlayerInfoNotAvailable));
            Log.Instance.Warning(NotificationStrings.PlayerInfoNotAvailable);
            return;
        }

        var logger = Log.Instance.ForContext<SaveDataService>();
        logger.Information($"Active character save: save_{_dataset.ActiveCharacterIndex}.sav");

        FileHeader fhp = _dataset.ProfileSaveFile!.FileHeader;
        logger.Information($"Profile save file version: {fhp.Version}, game build: {fhp.BuildNumber}");

        // Account Awards ------------------------------------------------------------
        logger.Information("BEGIN Account Awards");
        foreach (string award in _dataset.AccountAwards)
        {
            LootItem? lootItem = ItemDb.GetItemByIdOrDefault(award);
            if (lootItem == null)
            {
                logger.Warning($"  UnknownMarker account award: {award}");
            }
            else
            {
                logger.Information($"  Account award: {lootItem.Name}");
            }
        }
        foreach (LootItem m in ItemDb.Db.Where(x => x["Type"] == "award" && !_dataset.AccountAwards.Exists(y => y == x["Id"])).Select(x => new LootItem { Properties = x }))
        {
            logger.Information($"  Missing {Utils.Capitalize(m.Type)}: {m.Name}");
        }
        logger.Information("END Account Awards");

        for (int index = 0; index < _dataset.Characters.Count; index++)
        {
            // Character ------------------------------------------------------------
            Character character = _dataset.Characters[index];
            int acquired = character.Profile.AcquiredItems;
            int missing = character.Profile.MissingItems.Count;
            int total = acquired + missing;

            logger.Information($"Character {index + 1} (save_{character.Index}), Acquired Items: {acquired}, Missing Items: {missing}, Total: {total}");
            FileHeader fh = character.WorldSaveFile!.FileHeader;
            logger.Information($"World save file version: {fh.Version}, game build: {fh.BuildNumber}");
            logger.Information($"Is Hardcore: {character.Profile.IsHardcore}");
            logger.Information($"Trait Rank: {character.Profile.TraitRank}");
            logger.Information($"Last Saved Trait Points: {character.Profile.LastSavedTraitPoints}");
            logger.Information($"Power Level: {character.Profile.PowerLevel}");
            logger.Information($"Item Level: {character.Profile.ItemLevel}");
            logger.Information($"Gender: {character.Profile.Gender}");
            logger.Information($"Relic Charges: {character.Profile.RelicCharges}");
            // Equipment ------------------------------------------------------------
            logger.Information($"BEGIN Equipment, Character {index + 1} (save_{character.Index})");
            List<InventoryItem> equipped = character.Profile.Inventory.Where(x => x.IsEquipped).ToList();
            IOrderedEnumerable<InventoryItem> equipment1 = equipped.Where(x => !x.IsTrait).OrderBy(x => x.EquippedSlot);
            IOrderedEnumerable<InventoryItem> traits1 = equipped.Where(x => x.IsTrait).OrderBy(x => x.EquippedSlot);

            foreach (InventoryItem r in equipment1)
            {
                if (Enum.IsDefined(typeof(EquipmentSlot), r.EquippedSlot!))
                {
                    string level = r.Level is > 0 ? $" +{r.Level}" : "";
                    LootItem? item = ItemDb.GetItemByProfileId(r.ProfileId);
                    logger.Information(item == null
                        ? $"!!{r.ProfileId} not found in the database!"
                        : $"  {Utils.FormatCamelAsWords(r.EquippedSlot.ToString())}: {item.Name}{level}");

                    foreach (InventoryItem m in character.Profile.Inventory.Where(x => x.EquippedModItemId == r.Id))
                    {
                        if (m.LootItem == null) continue;
                        if (m.LootItem.Type == "fragment")
                        {
                            // The relic's FRAGMENTS-panel fragments (EquippedModItemId points at the equipped
                            // relic — a holdover link, not prism segments). Show them by their prism-slot name,
                            // the in-game side-bar vocabulary (e.g. "Armor" not the item label "Base Armor").
                            // The per-level % value will be added in a later pass.
                            LootItem slotDef = ItemDb.GetPrismSegmentByFragmentId(m.LootItem.Id) ?? m.LootItem;
                            string slotType = Utils.FormatCamelAsWords(r.EquippedSlot.ToString());
                            string prefix = string.IsNullOrEmpty(slotType) ? "" : $"{slotType} ";
                            logger.Information($"  {prefix}Fragment: {slotDef.Name} (lvl {m.Level ?? 1})");
                        }
                        else
                        {
                            logger.Information($"  {Utils.FormatEquipmentSlot(r.EquippedSlot.ToString(), m.LootItem.Type, m.Level ?? 1, m.LootItem.Name)}");
                        }
                    }
                }
            }

            foreach (var r in traits1.Select(x => new { ItemDb.GetItemByProfileId(x.ProfileId)!.Name, Item = x }).OrderBy(x => x.Name))
            {
                logger.Information($"  Trait: {r.Name}, Level {r.Item.Level}");
            }
            logger.Information($"END Equipment, Character {index + 1} (save_{character.Index}),");

            // Loadouts ------------------------------------------------------------
            logger.Information($"BEGIN Loadouts, Character {index + 1} (save_{character.Index})");
            if (character.Profile.Loadouts == null)
            {
                logger.Information("This character has no loadouts");
            }
            else
            {
                for (int i = 0; i < character.Profile.Loadouts.Count; i++)
                {
                    List<LoadoutRecord> loadoutRecords = character.Profile.Loadouts[i];
                    if (loadoutRecords.Count == 0)
                    {
                        logger.Information($"Loadout {i + 1}: empty");
                    }
                    else
                    {
                        logger.Information($"Loadout {i + 1}:");
                        IOrderedEnumerable<LoadoutRecord> equipment = loadoutRecords.Where(x => x.Type == LoadoutRecordType.Equipment).OrderBy(x => x.Slot);
                        IOrderedEnumerable<LoadoutRecord> traits = loadoutRecords.Where(x => x.Type == LoadoutRecordType.Trait).OrderBy(x => x.Slot);
                        List<LoadoutRecord> other = loadoutRecords.Where(x => x.Type != LoadoutRecordType.Equipment && x.Type != LoadoutRecordType.Trait).ToList();

                        foreach (LoadoutRecord r in equipment)
                        {
                            LoadoutSlot slot = (LoadoutSlot)r.Slot;
                            logger.Information($"  {Utils.FormatEquipmentSlot(slot.ToString(), r.ItemType, r.Level, r.Name)}");
                        }

                        foreach (LoadoutRecord r in traits)
                        {
                            switch (r.Slot)
                            {
                                case 0:
                                case 1:
                                    continue; // These are archetypes we already display them in the equipment section, they are the same
                                case 2:
                                    logger.Information($"  Trait: {r.Name}, Level {r.Level}");
                                    break;
                                default:
                                    logger.Warning($"  !!!Unknown Slot {r.Name}, {r.Type}, {r.Slot}, {r.Level}");
                                    break;
                            }
                        }

                        if (other.Count > 0)
                        {
                            foreach (LoadoutRecord r in other)
                            {
                                logger.Warning($"  !!!Unknown Type {r.Name}, {r.Type}, {r.Slot}, {r.Level}");
                            }
                        }
                    }
                }
            }
            logger.Information($"END Loadouts, Character {index + 1} (save_{character.Index})");

            // Inventory ------------------------------------------------------------
            logger.Information($"BEGIN Inventory, Character {index + 1} (save_{character.Index})");

            List<InventoryItem> debug = character.Profile.Inventory.Where(x => x.ProfileId == "/Game/Items/Common/Item_DragonHeartUpgrade.Item_DragonHeartUpgrade_C").ToList();
            List<IGrouping<string?, InventoryItem>> itemTypes = [.. character.Profile.Inventory
                .GroupBy(x => x.LootItem?.Type)
                .OrderBy(x=> x.Key)];

            foreach (IGrouping<string?, InventoryItem> type in itemTypes)
            {
                string? typeKey = type.Key;
                if (typeKey == null)
                {
                    foreach (InventoryItem item in type)
                    {
                        if (!Utils.IsKnownInventoryItem(Utils.GetNameFromProfileId(item.ProfileId)))
                        {
                            logger.Warning($"  Inventory item not found in database: {item.ProfileId}");
                        }
                    }
                }
                else
                {
                    if (typeKey == "armorspecial") continue;
                    logger.Information("  " + Utils.Capitalize(typeKey) + ":");

                    bool hasOne = false;
                    foreach (InventoryItem item in type.OrderBy(x => x.LootItem!.Name))
                    {
                        if (item.Quantity is 0) continue;
                        hasOne = true;

                        string name = item.LootItem!.Name;
                        string quantity = item.Quantity.HasValue ? $" x{item.Quantity.Value}" : "";
                        string level = item.Level.HasValue ? $" +{item.Level.Value}" : "";
                        string favorited = item.Favorited ? ", favorite" : "";
                        string @new = item.New ? ", new" : "";
                        string slotted = item.EquippedModItemId is >= 0 ? ", slotted" : "";
                        if (item.LootItem!.Type == "fragment")
                        {
                            name = Utils.FormatRelicFragmentLevel(item.LootItem!.Name, item.Level ?? 1);
                            // In-game-style value, only for fragments actually slotted into the relic
                            // (a loose inventory fragment provides no bonus, so showing its value is misleading).
                            string fragValue = item.EquippedModItemId is >= 0
                                && item.LootItem!.As<FragmentLootItem>()?.ValueAt(item.Level ?? 1) is { } fv ? $" {RenderFragmentValue(fv)}" : "";
                            level = item.Level.HasValue ? $" (lvl {item.Level.Value}){fragValue}" : "";
                        }
                        if (item.LootItem!.Type == "archetype" || item.LootItem!.Type == "trait")
                        {
                            level = item.Level.HasValue ? $", Level {item.Level.Value}" : "";
                        }
                        logger.Information("    " + name + quantity + level + favorited + @new + slotted);
                        if (item.Id != null)
                        {
                            foreach (InventoryItem slottedItem in character.Profile.Inventory.Where(x => x.EquippedModItemId == item.Id))
                            {
                                LootItem? li = slottedItem.LootItem;
                                if (li == null)
                                {
                                    logger.Warning($"!!!!!!Equipped item with profileId: '{slottedItem.ProfileId}' not found");
                                }
                                // Post-revamp, relic fragments aren't relic contents — they're a character-level
                                // FRAGMENTS panel (and, separately, prism segments). A slotted fragment's
                                // EquippedModItemId still points at the equipped relic (a holdover from the old
                                // slot-into-relic system), so listing it here would wrongly nest it under the
                                // relic. Skip fragments — they're rendered separately (the FRAGMENTS panel).
                                else if (li.Type != "fragment")
                                {
                                    logger.Information($"      {Utils.FormatEquipmentSlot(string.Empty, li.Type, slottedItem.Level ?? 1, li.Name)}");
                                }
                            }
                        }
                    }
                    if (!hasOne)
                    {
                        logger.Information("    None");
                    }

                }
            }

            logger.Information($"END Inventory, Character {index + 1} (save_{character.Index})");

            // Equipment------------------------------------------------------------
            logger.Information($"BEGIN Quick slots, Character {index + 1} (save_{character.Index})");
            foreach (InventoryItem item in character.Profile.QuickSlots)
            {
                logger.Information($"  {item.LootItem?.Name}");
            }
            logger.Information($"END Quick slots, Character {index + 1} (save_{character.Index})");

            // Prisms ------------------------------------------------------------
            logger.Information($"BEGIN Prisms, Character {index + 1} (save_{character.Index})");

            // The relic's slotted fragments (the in-game "FRAGMENTS" panel), duplicated here in the
            // prism-segment format. Character-level, not tied to a specific prism (0-3 of them). The value
            // is the fragment at its own level (ValueAt), as it is equipped, not leveled in a prism.
            // Inventory order == the relic's slot order, matching the in-game FRAGMENTS panel; don't re-sort.
            List<InventoryItem> slottedFragments = character.Profile.Inventory
                .Where(x => x.LootItem?.Type == "fragment" && x.EquippedModItemId is >= 0)
                .ToList();
            if (slottedFragments.Count > 0)
            {
                logger.Information("  Slotted fragments:");
                foreach (InventoryItem f in slottedFragments)
                {
                    LootItem? slotDef = ItemDb.GetPrismSegmentByFragmentId(f.LootItem!.Id);
                    string segName = slotDef?.Name ?? f.LootItem!.Name;
                    string color = slotDef?.As<PrismSlotLootItem>() is { } ps ? $" ({ps.Color})" : "";
                    string val = f.LootItem!.As<FragmentLootItem>()?.ValueAt(f.Level ?? 1) is { } fv ? $" -> {RenderFragmentValue(fv)}" : "";
                    logger.Information($"    {segName} +{f.Level ?? 1}{color}{val}");
                }
            }

            List<PrismData> prisms = character.Profile.Prisms;
            if (prisms.Count == 0)
            {
                logger.Information("  None");
            }
            foreach (PrismData prism in prisms)
            {
                string prismName = prism.Item.LootItem?.Name ?? prism.Item.ProfileId;
                logger.Information($"  {prismName} +{prism.DisplayLevel} (raw level {prism.Level}, fed: {(prism.HasBeenFed ? "yes" : "no")})");

                // Progress of the ring shown around the level in-game (XP toward the next level).
                // PendingExperience can bank past the threshold without levelling up, so the
                // ring (and this %) cap at 100%; the raw banked XP is shown by pendingXp.
                int pendingXp = (int)prism.PendingExperience;
                if (prism.ExperienceRequiredForNextLevel is { } requiredXp)
                {
                    logger.Information($"    Level progress: {pendingXp} / {requiredXp} XP ({prism.LevelProgress:P0})");
                }
                else
                {
                    logger.Information($"    Level progress: maxed ({pendingXp} XP pending)");
                }

                // Fragments and fusions leveled up in the prism (the "PRISM" block in-game)
                if (prism.Slots.Count == 0)
                {
                    logger.Information("    Prism: empty");
                }
                else
                {
                    logger.Information("    Prism:");
                    foreach (PrismSlot segment in prism.Slots)
                    {
                        LootItem? def = segment.LootItem;
                        string name = def?.Name ?? segment.RowName;
                        FusionLootItem? fusion = def?.As<FusionLootItem>();
                        PrismSlotLootItem? single = def?.As<PrismSlotLootItem>();

                        // In-game-style value(s). Fusions scale LINEARLY per component (FusionLootItem.ValueAtN);
                        // single segments use the fragment value curve (PrismSegmentValueAt).
                        if (fusion is not null)
                        {
                            // "name(color) value" per component, ordered by colour (Red > Blue > Yellow) to match the game.
                            (string Name, string Color, FragmentValue Value)[] parts =
                            [
                                (fusion.PrismSlotFragment1.Name, fusion.Color1, fusion.ValueAt1(segment.Level)),
                                (fusion.PrismSlotFragment2.Name, fusion.Color2, fusion.ValueAt2(segment.Level)),
                            ];
                            string combo = string.Join(" / ", parts
                                // in-game fusion component order: Red, then Blue, then Yellow
                                .OrderBy(p => p.Color switch { "Red" => 0, "Blue" => 1, "Yellow" => 2, _ => 3 })
                                .Select(p => $"{p.Name}({p.Color}) {RenderFragmentValue(p.Value)}"));
                            logger.Information($"      [Fusion] {name} +{segment.Level} -> [{combo}]");
                        }
                        else
                        {
                            string color = single is { } ps ? $" ({ps.Color})" : "";
                            string value = single is not null
                                && ItemDb.GetItemByIdOrDefault(single.Fragment)?.As<FragmentLootItem>()?.PrismSegmentValueAt(segment.Level) is { } fv
                                ? $" -> {RenderFragmentValue(fv)}" : "";
                            logger.Information($"      {name} +{segment.Level}{color}{value}");
                            // Legendary ("Mythic") slots carry an effect description; show it underneath.
                            if (def is { Type: "legendary" } && def.Description is { Length: > 0 } desc)
                            {
                                logger.Information($"        {desc}");
                            }
                        }
                    }
                }

                // Fragments fed into the prism (the "ROLL CHANCES" block in-game)
                if (prism.Feed.Count == 0)
                {
                    logger.Information("    Roll chances: none");
                }
                else
                {
                    logger.Information("    Roll chances:");
                    foreach (PrismFeed feed in prism.Feed)
                    {
                        LootItem? def = feed.LootItem;
                        string name = def?.Name ?? feed.RowName;
                        string feedColorValue = def?.Properties.GetValueOrDefault("Color") is { } fc ? $" ({fc})" : "";
                        logger.Information($"      {name}: {feed.FedLevel}{feedColorValue}");
                    }
                }

                // Seeds (deterministic proof): CurrentSeed produces the offer below; NextSeed is what it
                // becomes after the next accepted pick (CurrentSeed advanced once per offered roll) and
                // drives the following level-up.
                logger.Information($"    Current seed: {prism.CurrentSeed}");
                logger.Information($"    Next seed:    {prism.NextSeed}");

                // Evaluated next enhancement offer (deterministic from CurrentSeed). Non-localized;
                // each offer exposes Definition.Id so this can be localized later.
                string legendaryTag = prism.IsLegendaryRoll ? "  [LEGENDARY +51 ROLL]" : "";
                logger.Information($"    Next offer{legendaryTag}  (pool {prism.NextRollPoolSize})");
                if (prism.NextRoll.Count == 0)
                {
                    logger.Information("      (no offer - empty pool)");
                }
                foreach (PrismOffer offer in prism.NextRoll)
                {
                    string fed = offer.FedLevel.HasValue ? offer.FedLevel.Value.ToString() : "-";
                    string offerColor =
                        offer.LootItem?.As<FusionLootItem>() is { } f ? $"{f.Color1}+{f.Color2}"
                        : offer.LootItem?.As<PrismSlotLootItem>() is { } s ? s.Color
                        : "None";
                    logger.Information(
                        $"      {offer.Name} +{offer.NextLevel} ({offerColor}, {offer.Kind.ToString().ToLowerInvariant()}, " +
                        $"rarity {offer.Rarity}, fed {fed}, weight {offer.Weight:F4})");
                }
            }
            logger.Information($"END Prisms, Character {index + 1} (save_{character.Index})");

            // Thaen fruit ------------------------------------------------------------
            if (character.Save.ThaenFruit == null)
            {
                logger.Information("Thaen fruit data not found");
            }
            else
            {
                logger.Information("Thaen fruit data");
                foreach (KeyValuePair<string, string> pair in character.Save.ThaenFruit.StringifiedRawData)
                {
                    logger.Information($"  {pair.Key}: {pair.Value}");
                }
            }

            // Campaign ------------------------------------------------------------
            logger.Information($"Save play time: {Utils.FormatPlaytime(character.Save.Playtime)}");
            foreach (Zone z in character.Save.Campaign.Zones)
            {
                logger.Information($"Campaign story: {z.Story}");
            }
            logger.Information($"Campaign difficulty: {character.Save.Campaign.Difficulty}");
            logger.Information($"Campaign play time: {Utils.FormatPlaytime(character.Save.Campaign.Playtime)}");
            string respawnPoint = character.Save.Campaign.RespawnPoint == null ? "Unknown" : character.Save.Campaign.RespawnPoint.ToString();
            logger.Information($"Campaign respawn point: {respawnPoint}");

            // Blood Moon
            if (character.Save.Campaign.BloodMoon == null)
            {
                logger.Information("Blood moon data not found");
            }
            else
            {
                logger.Information("Blood moon data");
                foreach (KeyValuePair<string, string> pair in character.Save.Campaign.BloodMoon.StringifiedRawData)
                {
                    logger.Information($"  {pair.Key}: {pair.Value}");
                }
            }

            // Campaign Quest Inventory ------------------------------------------------------------
            logger.Information($"BEGIN Quest inventory, Character {index + 1} (save_{character.Index}), mode: campaign");
            // TODO
            IEnumerable<LootItem> lootItems = character.Save.Campaign.QuestInventory.Select(x => ItemDb.GetItemByProfileId(x.ProfileId)).Where(x => x != null).OrderBy(x => x!.Name)!;
            IEnumerable<InventoryItem> unknown = character.Save.Campaign.QuestInventory.Where(x => ItemDb.GetItemByProfileId(x.ProfileId) == null);
            foreach (InventoryItem s in unknown)
            {
                logger.Warning($"  Quest item not found in database: {s.ProfileId}");
            }

            foreach (LootItem lootItem in lootItems)
            {
                logger.Information("  " + lootItem.Name);
            }
            logger.Information($"END Quest inventory, Character {index + 1} (save_{character.Index}), mode: campaign");

            if (character.Save.Adventure != null)
            {
                // Adventure ------------------------------------------------------------
                logger.Information($"Adventure story: {character.Save.Adventure.Zones[0].Story}");
                logger.Information($"Adventure difficulty: {character.Save.Adventure.Difficulty}");
                logger.Information($"Adventure play time: {Utils.FormatPlaytime(character.Save.Adventure.Playtime)}");
                respawnPoint = character.Save.Adventure.RespawnPoint == null ? "Unknown" : character.Save.Adventure.RespawnPoint.ToString();
                logger.Information($"Adventure respawn point: {respawnPoint}");

                // Blood Moon
                if (character.Save.Adventure.BloodMoon == null)
                {
                    logger.Information("Blood moon information not found");
                }
                else
                {
                    logger.Information("Blood moon data");
                    foreach (KeyValuePair<string, string> pair in character.Save.Adventure.BloodMoon.StringifiedRawData)
                    {
                        logger.Information($"  {pair.Key}: {pair.Value}");
                    }
                }

                // Adventure Quest Inventory ------------------------------------------------------------
                logger.Information($"BEGIN Quest inventory, Character {index + 1} (save_{character.Index}), mode: adventure");
                lootItems = character.Save.Adventure.QuestInventory.Select(x => ItemDb.GetItemByProfileId(x.ProfileId)).Where(x => x != null).OrderBy(x => x!.Name)!;
                unknown = character.Save.Adventure.QuestInventory.Where(x => ItemDb.GetItemByProfileId(x.ProfileId) == null);
                foreach (InventoryItem s in unknown)
                {
                    logger.Warning($"  Quest item not found in database: {s.ProfileId}");
                }

                foreach (LootItem lootItem in lootItems)
                {
                    logger.Information("  " + lootItem.Name);
                }

                logger.Information($"END Quest inventory, Character {index + 1} (save_{character.Index}), mode: adventure");
            }

            // Cass shop ------------------------------------------------------------
            logger.Information($"BEGIN Cass shop, Character {index + 1} (save_{character.Index})");
            foreach (LootItem lootItem in character.Save.CassShop)
            {
                logger.Information("  " + lootItem.Name);
            }
            logger.Information($"END Cass shop, Character {index + 1} (save_{character.Index})");

            // Quest log ------------------------------------------------------------
            logger.Information($"BEGIN Quest log, Character {index + 1} (save_{character.Index})");
            lootItems = character.Save.QuestCompletedLog
                .Select(x => ItemDb.GetItemByIdOrDefault($"Quest_{x}")).Where(x => x != null)!;
            IEnumerable<string> unknowns = character.Save.QuestCompletedLog.Where(x => ItemDb.GetItemByIdOrDefault($"Quest_{x}") == null);
            foreach (string s in unknowns)
            {
                logger.Warning($"  Quest not found in database: {s}");
            }
            foreach (LootItem lootItem in lootItems)
            {
                logger.Information($"  {lootItem.Name} ({lootItem.Properties["Subtype"]})");
            }
            logger.Information($"END Quest log, Character {index + 1} (save_{character.Index})");

            // Achievements ------------------------------------------------------------
            logger.Information($"BEGIN Achievements for Character {index + 1} (save_{character.Index})");
            foreach (ObjectiveProgress objective in character.Profile.Objectives)
            {
                if (objective.Type == "achievement")
                {
                    logger.Information($"  {Utils.Capitalize(objective.Type)}: {objective.Description} - {objective.Progress}");
                }
            }

            foreach (LootItem m in ItemDb.Db.Where(x => x["Type"] == "achievement" && !character.Profile.Objectives.Exists(y => y.Id == x["Id"])).Select(x => new LootItem { Properties = x }))
            {
                logger.Information($"  Missing {Utils.Capitalize(m.Type)}: {m.Name}");
            }

            logger.Information($"END Achievements for Character {index + 1} (save_{character.Index})");

            // Challenges ------------------------------------------------------------
            logger.Information($"BEGIN Challenges for Character {index + 1} (save_{character.Index})");
            foreach (ObjectiveProgress objective in character.Profile.Objectives)
            {
                if (objective.Type == "challenge")
                {
                    logger.Information($"  {Utils.Capitalize(objective.Type)}: {objective.Description} - {objective.Progress}");
                }
            }
            foreach (LootItem m in ItemDb.Db.Where(x => x["Type"] == "challenge" && !character.Profile.Objectives.Exists(y => y.Id == x["Id"])).Select(x => new LootItem { Properties = x }))
            {
                logger.Information($"  Missing {Utils.Capitalize(m.Type)}: {m.Name}");
            }
            logger.Information($"END Challenges for Character {index + 1} (save_{character.Index})");
            logger.Information("-----------------------------------------------------------------------------");
        }

        // Render a fragment value (number + unit) into in-game-style display text. The analyzer supplies the
        // value and its raw unit token, which is appended verbatim after the number (e.g. "%", "cm", "/s"). The
        // number is signed, shown to 1 decimal with trailing zeros dropped ("+10%" / "+200", not "+10.0%"), and
        // rounded half-to-even to match the game (Unreal's default; 5.25 -> 5.2). Flat "points" fragments (Base
        // Stamina/Health/Armor) have an empty unit but still scale along the curve, so they can be fractional
        // in-game (e.g. +2.8) — the decimal applies to them too, not an integer.
        static string RenderFragmentValue(FragmentValue v)
        {
            double n = Math.Round(v.Value, 1, MidpointRounding.ToEven);
            return (n < 0 ? "-" : "+") + Math.Abs(n).ToString("0.#", CultureInfo.InvariantCulture) + v.Unit;
        }
    }
    #endregion
}
