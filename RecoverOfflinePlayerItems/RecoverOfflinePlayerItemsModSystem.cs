using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Server;

namespace RecoverOfflinePlayerItems;

public class RecoverOfflinePlayerItemsModSystem : ModSystem
{

    private ICoreServerAPI _sapi = null!;
    public override void Start(ICoreAPI api)
    {
    }


    private List<ItemSlot[]> GetAllItemSlotArrays(object invManager)
    {
        var results = new List<ItemSlot[]>();
        if (invManager == null) return results;

        var type = invManager.GetType();
        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (field.FieldType == typeof(ItemSlot[]))
            {
                var val = field.GetValue(invManager) as ItemSlot[];
                if (val != null) results.Add(val);
            }
        }

        // Look for properties
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (prop.PropertyType == typeof(ItemSlot[]))
            {
                try
                {
                    var val = prop.GetValue(invManager) as ItemSlot[];
                    if (val != null) results.Add(val);
                }
                catch { }
            }
        }

        return results;
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        _sapi = api;

        try
        {
            api.ChatCommands.Create("recoveroffline")
                .WithDescription("Recover inventory from an offline player into the executing player's inventory")
                .WithArgs(api.ChatCommands.Parsers.Word("offlinePlayerUid"), api.ChatCommands.Parsers.WordRange("mode", new string[] {"add", "replace", "drop"}))
                .RequiresPrivilege(Privilege.commandplayer)
                .HandleWith(RestoreItems);
        }
        catch (Exception ex)
        {
            Mod.Logger.Warning("Could not register recoveroffline command: " + ex.Message);
        }
    }

    public override void StartClientSide(ICoreClientAPI api)
    {

    }

    enum RecoveryMode
    {
        add,
        replace,
        drop
    }

    private TextCommandResult RestoreItems(TextCommandCallingArgs args)
    {
        var byPlayer = args.Caller.Player;
        if (byPlayer == null)
        {
            return TextCommandResult.Error("This command must be run by an online player.");
        }

        var offlineUid = args.Parsers[0].GetValue() as string;
        if (string.IsNullOrEmpty(offlineUid))
        {

            return TextCommandResult.Error("Usage: /recoveroffline <offlinePlayerUid>");
        }

        var offlinePlayer = LoadOfflinePlayer(offlineUid) as ServerPlayer;
        var mode = args.Parsers[1].GetValue() as string;
        Enum.TryParse(mode.ToLower(), out RecoveryMode recoveryMode);

        if (offlinePlayer == null)
        {
            return TextCommandResult.Error("Failed to load offline player data for {offlineUid}");
        }
        _sapi.Logger.Notification("Loaded offline player data for " + offlineUid + ". Attempting restore with mode "+recoveryMode+" for player "+byPlayer.PlayerName);

        var onlineServerPlayer = byPlayer as ServerPlayer;
        if (onlineServerPlayer == null)
        {
            return TextCommandResult.Error("Could not resolve executing player as a ServerPlayer.");
        }

        var offInvManager = offlinePlayer.InventoryManager;
        var onInvManager = onlineServerPlayer.InventoryManager;

        var offSlotArrays = GetAllItemSlotArrays(offInvManager);
        var offSlotBackpacks = offInvManager.GetOwnInventory(GlobalConstants.backpackInvClassName) as InventoryPlayerBackPacks;
        var offSlotCrafting = offInvManager.GetOwnInventory(GlobalConstants.craftingInvClassName) as InventoryCraftingGrid;
        var offSlotHotbar = offInvManager.GetHotbarInventory();
        var offSlotCharacter = offInvManager.GetOwnInventory(GlobalConstants.characterInvClassName) as InventoryCharacter;
        var offSlotCursor = offInvManager.GetOwnInventory(GlobalConstants.mousecursorInvClassName) as InventoryPlayerMouseCursor;
        var onSlotArrays = GetAllItemSlotArrays(onInvManager);
        var onSlotBackpacks = onInvManager.GetOwnInventory(GlobalConstants.backpackInvClassName) as InventoryPlayerBackPacks;
        var onSlotCrafting = onInvManager.GetOwnInventory(GlobalConstants.craftingInvClassName) as InventoryCraftingGrid;
        var onSlotHotbar = onInvManager.GetHotbarInventory();
        var onSlotCharacter = onInvManager.GetOwnInventory(GlobalConstants.characterInvClassName) as InventoryCharacter;
        var onSlotCursor = onInvManager.GetOwnInventory(GlobalConstants.mousecursorInvClassName) as InventoryPlayerMouseCursor;
        var itemsToDrop = new List<ItemStack>();


        if (recoveryMode == RecoveryMode.drop)
        {
            if (offSlotBackpacks != null)
            {
                onInvManager.DropAllInventoryItems(offSlotBackpacks);
            }
            if (offSlotHotbar != null)
            {
                onInvManager.DropAllInventoryItems(offSlotHotbar);
            }
            if (offSlotCrafting != null)
            {
                onInvManager.DropAllInventoryItems(offSlotCrafting);
            }
            if (offSlotCharacter != null)
            {
                onInvManager.DropAllInventoryItems(offSlotCharacter);
            }
            if (offSlotCursor != null)
            {
                onInvManager.DropAllInventoryItems(offSlotCursor);
            }
        }
        else if (recoveryMode == RecoveryMode.add)
        {
            if (offSlotBackpacks != null)
            {
                foreach (var slot in offSlotBackpacks)
                {
                    if (slot?.Itemstack != null)
                    {
                        onlineServerPlayer.InventoryManager.TryGiveItemstack(slot.Itemstack);
                    }
                }
            }

            if (offSlotCursor != null)
            {
                foreach (var slot in offSlotCursor)
                {
                    if (slot?.Itemstack != null)
                    {
                        onlineServerPlayer.InventoryManager.TryGiveItemstack(slot.Itemstack);
                    }
                }
            }

            if (offSlotHotbar != null)
            {
                foreach (var slot in offSlotHotbar)
                {
                    if (slot?.Itemstack != null)
                    {
                        onlineServerPlayer.InventoryManager.TryGiveItemstack(slot.Itemstack);
                    }
                }
            }

            if (offSlotCrafting != null)
            {
                foreach (var slot in offSlotCrafting)
                {
                    if (slot?.Itemstack != null)
                    {
                        onlineServerPlayer.InventoryManager.TryGiveItemstack(slot.Itemstack);
                    }
                }
            }

            if (offSlotCharacter != null)
            {
                foreach (var slot in offSlotCharacter)
                {
                    if (slot?.Itemstack != null)
                    {
                        onlineServerPlayer.InventoryManager.TryGiveItemstack(slot.Itemstack);
                    }
                }
            }
        }
        else if (recoveryMode == RecoveryMode.replace)
        {
            if (offSlotBackpacks != null && onSlotBackpacks != null)
            {
                RestoreInventoryItems(offSlotBackpacks, onSlotBackpacks);
            }
            if (offSlotCursor != null && onSlotCursor != null)
            {
                RestoreInventoryItems(offSlotCursor, onSlotCursor);
            }
            if (offSlotHotbar != null && onSlotHotbar != null)
            {
                RestoreInventoryItems(offSlotHotbar, onSlotHotbar);
            }
            if (offSlotCrafting != null && onSlotCrafting != null)
            {
                RestoreInventoryItems(offSlotCrafting, onSlotCrafting);
            }
            if (offSlotCharacter != null && onSlotCharacter != null)
            {
                RestoreInventoryItems(offSlotCharacter, onSlotCharacter);
            }
            
        }

        return TextCommandResult.Success("Recovered Inventory from offline player "+offlineUid+" into "+byPlayer.PlayerName);
    }

    private static void RestoreInventoryItems(IInventory fromInventory, IInventory toInventory)
    {
        if (fromInventory == null || toInventory == null)
        {
            return;
        }
        for (int i = 0; i < fromInventory?.Count; i++)
        {
            if (toInventory[i] == null)
            {
                continue;
            }
            toInventory[i].Itemstack = fromInventory[i].Itemstack;
            toInventory.MarkSlotDirty(i);
        }
    }

    private IServerPlayer? LoadOfflinePlayer(string playerUid)
    {
        try
        {
            _sapi.Logger.Notification($"Loading offline player data for {playerUid}");
            var server = (ServerMain)_sapi.World;
            var chunkThread =
                typeof(ServerMain).GetField("chunkThread", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(server)
                    as ChunkServerThread;
            var gameDatabase =
                typeof(ChunkServerThread).GetField("gameDatabase", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(chunkThread) as GameDatabase;
            var playerData = gameDatabase?.GetPlayerData(playerUid);
            if (playerData == null || playerData.Length == 0)
            {
                return null;
            }
            ServerWorldPlayerData? playerWorldData;
            playerWorldData = SerializerUtil.Deserialize<ServerWorldPlayerData>(playerData);
            playerWorldData.Init(server);
            var serverPlayer = new ServerPlayer(server, playerWorldData);
            var initMethod = typeof(ServerPlayer).GetMethod("Init", BindingFlags.Instance | BindingFlags.NonPublic);
            initMethod?.Invoke(serverPlayer, null);
            _sapi.Logger.Notification($"Successfully loaded offline player data for {playerUid}");
            if (serverPlayer.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName) is
                InventoryPlayerBackPacks bp)
            {
                _sapi.Logger.Notification($"Reloading backpack inventory for {playerUid}");
                _sapi.Logger.Notification($"Backpack inventory: {bp}");
                _sapi.Logger.Notification($"Backpack inventory slots: {bp.Count}");

                var bagInv = typeof(InventoryPlayerBackPacks).GetField("bagInv", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(bp) as BagInventory;
                var bagSlots = typeof(InventoryPlayerBackPacks).GetField("bagSlots", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.GetValue(bp) as ItemSlot[];
                _sapi.Logger.Notification($"Bag inventory: {bagInv}");
                bagInv?.ReloadBagInventory(bp, bagSlots);
            }

            return serverPlayer;
        }
        catch (Exception e)
        {
            Mod.Logger.Error($"Failed loading offline player data for {playerUid}");
            Mod.Logger.Error(e);
            return null;
        }
    }



}
