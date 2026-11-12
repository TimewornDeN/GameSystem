using GameSystem.CLI.Commands;
using GameSystem.Core.Abstractions;
using GameSystem.Core.Engine;

namespace GameSystem.CLI.Battle;

/// <summary>attack | heal | defend | ability [id] — one move of the current fighter. The turn passes only if the move worked.</summary>
public sealed class BattleActionCommand(string actionName, string description, BattleSession session, IDisplayer displayer)
    : CommandBase(actionName, description)
{
    protected override void Run(CommandContext context)
    {
        string? abilityId = Name.Equals("ability", StringComparison.OrdinalIgnoreCase)
            ? context.Args.FirstOrDefault() ?? context.GetOption("id")
            : null;

        var action = session.CreateAction(Name, abilityId);
        if (action is null)
        {
            displayer.Error($"Cannot perform '{Name}'.");
            return;
        }

        try
        {
            session.Perform(action);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            displayer.Error(ex.Message);
        }
    }
}
