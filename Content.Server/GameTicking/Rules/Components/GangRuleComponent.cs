namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores data for <see cref="GangRuleSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(GangRuleSystem))]
public sealed partial class GangRuleComponent : Component;
