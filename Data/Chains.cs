using ProjectM;
using Stunlock.Core;
using System.Collections.Generic;
using Unity.Entities;
using static ProjectM.RandomizedSpawnChainSettingsAuthoring;

namespace KindredSchematics.Data;

internal static class Chains
{
    public static Dictionary<string, PrefabGUID> ChainPrefabs { get; private set; } = new();

    public static void Populate()
    {
        ChainPrefabs = new Dictionary<string, PrefabGUID>();

        foreach (var (name, prefabGuid) in Core.PrefabCollection._SpawnableNameToPrefabGuidDictionary)
        {
            if (!Core.PrefabCollection._PrefabGuidToEntityMap.TryGetValue(prefabGuid, out _))
                continue;

            if (!name.StartsWith("Chain_"))
                continue;

            ChainPrefabs[name] = prefabGuid;
        }
    }
}
