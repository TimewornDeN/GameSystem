using GameSystem.Core.Engine;

namespace GameSystem.Core.Rendering;

/// <summary>The current state of a battle: fighters, health, round and whose turn it is.</summary>
public sealed class BattleStatusRenderable(BattleSession session) : Renderable<BattleSession>(session);
