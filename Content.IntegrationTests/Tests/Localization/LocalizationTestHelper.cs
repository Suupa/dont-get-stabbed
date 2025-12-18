using System.Text.RegularExpressions;
using Robust.Shared.Localization;

namespace Content.IntegrationTests.Tests.Localization;

public static class LocalizationTestHelper
{
    private const string Whatever = "_WHATEVER85643584163_"; //temp placeholder, doesn't matter as long is it reasonably unique

    public static Regex GetRegex_AllVarsWildCards(LocBlock[] locBlocks)
    {
        var regexStr = "";
        for(int i = 0; i < locBlocks.Length; i++)
        {
            regexStr += GetLocRegex(locBlocks[i].Id, locBlocks[i].ArgNames);
            if (i != locBlocks.Length - 1)
                regexStr += ' ';
        }
        return new Regex(regexStr.Replace(Whatever,".*"));
    }

    private static string GetLocRegex(string locId, params string[] argNames)
    {
        var args = new (string, object)[argNames.Length];
        for (var i = 0; i < argNames.Length; i++)
        {
            args[i] = (argNames[i], Whatever);
        }

        return Regex.Escape(Loc.GetString(locId, args));
    }

    public readonly struct LocBlock(string id, params string[] argNames)
    {
        public string Id { get; } = id;
        public string[] ArgNames { get; } = argNames;

        public override string ToString()
        {
            return $"({Id}: {ArgNames})";
        }
    }
}
