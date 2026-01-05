using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using VampireCommandFramework;
using static ProjectM.SpawnChainData;

namespace KindredSchematics.Commands
{
    [CommandGroup("chain")]
    internal class ChainCommands
    {
        [Command("search", description: "Search for Chain_ prefabs by name", adminOnly: true)]
        public static void SearchChain(ChatCommandContext ctx, string searchTerm, int page = 1)
        {
            var matches = new List<(string Name, PrefabGUID Prefab)>();

            foreach (var kvp in Core.PrefabCollection._SpawnableNameToPrefabGuidDictionary)
            {
                if (kvp.Key.StartsWith("Chain_", StringComparison.OrdinalIgnoreCase) &&
                    kvp.Key.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add((kvp.Key, kvp.Value));
                }
            }

            if (matches.Count == 0)
            {
                ctx.Reply($"No Chain_ prefabs found matching '{searchTerm}'");
                return;
            }

            matches = matches.OrderBy(m => m.Name).ToList();

            var sb = new System.Text.StringBuilder();
            var totalCount = matches.Count;
            var pageSize = 7;
            var pageLabel = totalCount > pageSize ? $" (Page {page}/{System.Math.Ceiling(totalCount / (float)pageSize)})" : "";

            if (totalCount > pageSize)
            {
                matches = matches.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            }

            sb.AppendLine($"Found {totalCount} Chain_ prefabs matching '{searchTerm}'{pageLabel}:");
            foreach (var (name, prefab) in matches)
            {
                sb.AppendLine($"({prefab.GuidHash}) {name.Replace(searchTerm, $"<b>{searchTerm}</b>", StringComparison.OrdinalIgnoreCase)}");
            }

            ctx.Reply(sb.ToString());
        }

        [Command("spawn", description: "Spawn Chain_ prefab by name or GUID", adminOnly: true)]
        public static void SpawnChain(ChatCommandContext ctx, string nameOrGuid)
        {
            PrefabGUID prefabGuid;
            string prefabName = null;

            // Try to parse as GUID first
            if (int.TryParse(nameOrGuid, out int guidHash))
            {
                prefabGuid = new PrefabGUID(guidHash);

                // Find the name from the dictionary
                foreach (var kvp in Core.PrefabCollection._SpawnableNameToPrefabGuidDictionary)
                {
                    if (kvp.Value.GuidHash == guidHash)
                    {
                        prefabName = kvp.Key;
                        break;
                    }
                }

                if (prefabName == null)
                {
                    prefabName = $"Unknown_{guidHash}";
                }
            }
            else
            {
                // Search by name in Chain_ prefabs
                var matches = new List<(string Name, PrefabGUID Prefab)>();

                foreach (var kvp in Core.PrefabCollection._SpawnableNameToPrefabGuidDictionary)
                {
                    if (kvp.Key.StartsWith("Chain_", StringComparison.OrdinalIgnoreCase) &&
                        (kvp.Key.Equals(nameOrGuid, StringComparison.OrdinalIgnoreCase) ||
                         kvp.Key.Contains(nameOrGuid, StringComparison.OrdinalIgnoreCase)))
                    {
                        matches.Add((kvp.Key, kvp.Value));
                    }
                }

                if (matches.Count == 0)
                {
                    ctx.Reply($"No Chain_ prefab found matching '{nameOrGuid}'");
                    return;
                }

                if (matches.Count > 1)
                {
                    ctx.Reply($"Multiple matches found for '{nameOrGuid}':");
                    foreach (var (name, prefab) in matches.Take(10))
                    {
                        ctx.Reply($"  {name} ({prefab.GuidHash})");
                    }
                    ctx.Reply("Please be more specific");
                    return;
                }

                prefabGuid = matches[0].Prefab;
                prefabName = matches[0].Name;
            }

            // Spawn the prefab
            if (!Core.PrefabCollection._PrefabGuidToEntityMap.TryGetValue(prefabGuid, out var prefabEntity))
            {
                ctx.Reply($"Prefab not found in collection");
                return;
            }

            var spawnPos = ctx.Event.SenderCharacterEntity.Read<EntityAimData>().AimPosition;
            var rot = ctx.Event.SenderCharacterEntity.Read<Rotation>().Value;

            var entity = Core.EntityManager.Instantiate(prefabEntity);
            entity.Add<PhysicsCustomTags>();
            entity.Write(new Translation { Value = spawnPos });
            entity.Write(new Rotation { Value = rot });

            ctx.Reply($"Spawned {prefabName}");
        }

        [Command("delete", description: "Delete the chain entity at cursor and prevent respawn", adminOnly: true)]
        public static void DeleteChain(ChatCommandContext ctx)
        {
            var aimPos = ctx.Event.SenderCharacterEntity.Read<EntityAimData>().AimPosition;

            var closestTile = Helper.FindClosestTilePosition(aimPos);

            if (closestTile == Entity.Null)
            {
                ctx.Reply("No tile found at cursor");
                return;
            }

            var tilePrefabName = closestTile.Read<PrefabGUID>().LookupName();

            // Check if this tile is part of a spawn chain
            if (!closestTile.Has<SpawnChainChild>())
            {
                ctx.Reply($"Tile {tilePrefabName} is not part of a spawn chain");
                return;
            }

            // Get the chain parent
            var spawnChainChild = closestTile.Read<SpawnChainChild>();
            var chainParent = spawnChainChild.SpawnChain;

            if (chainParent == Entity.Null || !Core.EntityManager.Exists(chainParent))
            {
                ctx.Reply("Chain parent not found");
                return;
            }

            var chainPrefabName = chainParent.Read<PrefabGUID>().LookupName();

            // Delete the active tile model
            DestroyUtility.Destroy(Core.EntityManager, closestTile);

            // Delete the chain parent to prevent respawning
            DestroyUtility.Destroy(Core.EntityManager, chainParent);

            ctx.Reply($"Deleted {tilePrefabName} and chain parent {chainPrefabName}");
        }
    }
}