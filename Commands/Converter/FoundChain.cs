using KindredSchematics.Data;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VampireCommandFramework;

namespace KindredSchematics.Commands.Converter;

public record FoundChain(PrefabGUID Value, string Name);

class FoundChainConverter : CommandArgumentConverter<FoundChain>
{
    static Dictionary<string, PrefabGUID> _cache = Chains.ChainPrefabs.ToDictionary(
        x => x.Key,
        x => x.Value,
        StringComparer.OrdinalIgnoreCase
    );

    public override FoundChain Parse(ICommandContext ctx, string input)
    {
        if (int.TryParse(input, out int guid))
        {
            return new FoundChain(new PrefabGUID(guid), $"Chain_{guid}");
        }

        if (_cache.TryGetValue(input, out var prefab))
        {
            return new FoundChain(prefab, input);
        }

        var matches = _cache.Keys.Where(k => k.Contains(input, StringComparison.OrdinalIgnoreCase)).ToList();
        if (matches.Count == 1)
        {
            return new FoundChain(_cache[matches[0]], matches[0]);
        }
        else if (matches.Count > 1)
        {
            throw ctx.Error($"Ambiguous chain name '{input}'. Matches: {string.Join(", ", matches)}");
        }

        throw ctx.Error($"Unknown chain: '{input}'. Use a chain name or PrefabGUID.");
    }
}

