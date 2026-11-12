using GameSystem.Core.Abstractions;
using GameSystem.Core.Characters;
using GameSystem.Core.Combat.Actions;
using GameSystem.Core.Management;

namespace GameSystem.CLI.Commands;

/// <summary>act &lt;attack|heal|defend|ability&gt; &lt;actor&gt; [target] [--id &lt;ability_id&gt;] — a single action outside of a battle.</summary>
public sealed class ActCommand(GameWorld world, IDisplayer displayer)
    : CommandBase("act", "Executes an action: act <attack|heal|defend|ability> <actor> [target] [--id <ability_id>]")
{
    private const string Usage = "Usage: act <attack|heal|defend|ability> <actor> [target] [--id <ability_id>]";

    protected override void Run(CommandContext context)
    {
        if (context.Args.Count < 2)
        {
            displayer.Error(Usage);
            return;
        }

        string actionType = context.Args[0];
        string actorName = context.Args[1];
        string? targetName = context.Args.Count >= 3 ? context.Args[2] : null;

        var actor = world.FindCharacter(actorName);
        if (actor is null)
        {
            displayer.Error($"Actor character '{actorName}' not found.");
            return;
        }

        string action = actionType.ToLowerInvariant();

        // "defend" is the only action that works without a target.
        Character? target = null;
        if (action != "defend")
        {
            if (string.IsNullOrWhiteSpace(targetName))
            {
                displayer.Error($"Action '{action}' requires a target: act {action} <actor> <target>");
                return;
            }

            target = world.FindCharacter(targetName);
            if (target is null)
            {
                displayer.Error($"Target character '{targetName}' not found.");
                return;
            }
        }

        // The text command is turned into a combat action object (see CombatActionFactory) and executed.
        ICombatAction? combatAction = CombatActionFactory.Create(action, actor, target, context.GetOption("id"));
        if (combatAction is null)
        {
            displayer.Error($"Unknown action '{actionType}'. Allowed: attack, heal, defend, ability.");
            return;
        }

        try
        {
            combatAction.Execute();
        }
        catch (InvalidOperationException ex)
        {
            displayer.Error(ex.Message);
        }
        catch (ArgumentException ex)
        {
            displayer.Error(ex.Message);
        }
    }
}
