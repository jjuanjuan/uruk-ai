using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CombatManager : Node
{
    // hacer victoria

    public CharacterParty Team1;
    public CharacterParty Team2;

    [Export] public UICombatScene UI;
    [Export] float AttackFlightDuration = 0.5f; // TODO: mover esto a cada ataque individual
    [Export] PackedScene AttackDebugScene;
    [Export] Control VFXLayer;


    public enum CombatState
    {
        Init,
        WaitingForStartDelay,
        WaitingForStart,
        BuildTurnOrder,
        WaitingForUnit,
        WaitingAttackDelay,
        UnitActing,
        Resolving,
        WaitingAttackInterval,
        CheckEnd,
        Ended
    }

    CombatState state = CombatState.Init;
    float stateTimer = 0f;
    CombatContext combatContext;

    List<OrcInstance> turnOrder = new();
    int currentIndex = 0;
    OrcInstance currentUnit;
    int turnNumber = 0;
    AttackAction pendingAction;

    private enum RowType
    {
        Front,
        Middle,
        Back
    }

    // SIGNALS
    [Signal] public delegate void CombatStateChangedEventHandler();
    [Signal] public delegate void UnitChangedEventHandler(OrcInstance unit);

    private RowType GetRow(int row)
    {
        return row switch
        {
            0 => RowType.Back,
            1 => RowType.Middle,
            _ => RowType.Front
        };
    }
    //

    private void SetState(CombatState newState)
    {
        state = newState;
        stateTimer = 0f;

        GD.Print("Combat " + state.ToString());

        EmitSignal(SignalName.CombatStateChanged);
    }

    public override void _Process(double delta)
    {
        stateTimer += (float)delta;

        switch (state)
        {
            case CombatState.Init:
                InitCombat();
                break;

            case CombatState.WaitingForStart:
                break;

            case CombatState.WaitingForStartDelay:
                {
                    if (stateTimer >= GameManager.I.CombatConfig.CombatStartDelay)
                        SetState(CombatState.BuildTurnOrder);
                    break;
                }

            case CombatState.BuildTurnOrder:
                BuildTurnOrder();
                break;

            case CombatState.WaitingForUnit:
                UpdateWaitingForUnit();
                break;

            case CombatState.WaitingAttackDelay:
                {
                    if (stateTimer < GameManager.I.CombatConfig.AttackDelayWhenUI)
                        return;

                    var enemies = combatContext.GetEnemies(currentUnit);
                    var targets = ResolveTargets(currentUnit, enemies, pendingAction.Target);

                    var actionSnapshot = pendingAction;
                    var attackerSnapshot = currentUnit;

                    SpawnAttackDebug(targets, attackerSnapshot, actionSnapshot);

                    pendingAction = null;

                    SetState(CombatState.WaitingAttackInterval);
                    break;
                }

            case CombatState.WaitingAttackInterval:
                {
                    if (stateTimer < GameManager.I.CombatConfig.AttackInterval)
                        return;

                    SetState(CombatState.Resolving);
                    break;
                }

            case CombatState.UnitActing:
                break;

            case CombatState.Resolving:
                UpdateResolving();
                break;

            case CombatState.CheckEnd:
                UpdateCheckEnd();
                break;

            case CombatState.Ended:
                break;
        }
    }

    private void InitCombat()
    {
        combatContext = new CombatContext
        {
            Team1 = Team1,
            Team2 = Team2,
            UnitState = new Dictionary<OrcInstance, CombatUnitState>(),
            Score1 = 0,
            Score2 = 0,
        };

        UI?.SetAdvantageBar(.5f);
        SetState(CombatState.WaitingForStart);
    }
    public void StartCombat()
    {
        InitUnitState();
        if (state == CombatState.WaitingForStart)
            SetState(CombatState.WaitingForStartDelay);
    }

    private void InitUnitState()
    {
        combatContext.UnitState.Clear();

        var print = "<<Turn 0>>";
        print += "\nTeam 1: ";

        foreach (var o in combatContext.Team1.GetAllLivingOrcs())
        {
            AddUnit(o);
            print += o.GetCustomName() + ", ";
        }

        print += "\nTeam 2: ";

        foreach (var o in combatContext.Team2.GetAllLivingOrcs())
        {
            AddUnit(o);
            print += o.GetCustomName() + ", ";
        }

        UI?.AddLog(print);
    }

    private void AddUnit(OrcInstance o)
    {
        if (o == null) return;

        combatContext.UnitState[o] = new CombatUnitState
        {
            Orc = o,
            RemainingActions = o.CharacterClass.GetAttackPerPosition(o.PartyPosition.Row).Amount,
            HasActedThisTurn = false
        };
    }

    private void BuildTurnOrder()
    {
        turnOrder.Clear();
        currentIndex = 0;

        var all = new List<OrcInstance>(combatContext.UnitState.Keys);

        var groups = new Dictionary<int, List<OrcInstance>>();

        foreach (var orc in all)
        {
            if (orc == null) continue;

            int speed = orc.CharacterClass.GetBaseSpeed();

            if (!groups.ContainsKey(speed))
                groups[speed] = new List<OrcInstance>();

            groups[speed].Add(orc);
        }

        var speeds = new List<int>(groups.Keys);
        speeds.Sort();
        speeds.Reverse();

        foreach (var speed in speeds)
        {
            Shuffle(groups[speed]);
            turnOrder.AddRange(groups[speed]);
        }

        SetState(CombatState.WaitingForUnit);
    }

    private void UpdateWaitingForUnit()
    {
        while (currentIndex < turnOrder.Count)
        {
            var orc = turnOrder[currentIndex++];

            GD.Print($"{orc.GetCustomName()}'s turn");

            if (orc == null) continue;
            if (!orc.IsAlive) continue;

            var state = combatContext.UnitState[orc];

            if (state.RemainingActions <= 0)
                continue;

            currentUnit = orc;

            EmitSignal(SignalName.UnitChanged, currentUnit);
            SetState(CombatState.UnitActing);
            EnterUnitActing();
            return;
        }

        SetState(CombatState.CheckEnd);
    }

    private void EnterUnitActing()
    {
        if (currentUnit == null)
        {
            SetState(CombatState.CheckEnd);
            return;
        }

        if (pendingAction == null)
        {
            pendingAction = GetActionByRow(currentUnit);

            SetState(CombatState.WaitingAttackDelay);
        }
    }
    AttackAction GetActionByRow(OrcInstance orc)
    {
        return orc.CharacterClass
                  .GetAttackPerPosition(orc.PartyPosition.Row)
                  .AttackAction;
    }
    private void ExecuteAttack(OrcInstance attacker, AttackAction action)
    {
        var enemies = combatContext.GetEnemies(attacker);
        var targets = ResolveTargets(attacker, enemies, action.Target);

        string names = string.Join(", ", targets.Select(t => t.GetCustomName()));
        UI?.AddLog($"{attacker.GetCustomName()} hits {names}");

        foreach (var t in targets)
        {
            ApplyDamage(attacker, t, action);
        }

        foreach (var d in targets)
        {
            if (!d.IsAlive)
            {
                UI?.AddLog($"{d.GetCustomName()} dies!!");
                GiveKillScore(attacker);
            }
        }

        UI?.SetAdvantageBar(combatContext.CalculateAdvantage());
    }
    void SpawnAttackDebug(List<OrcInstance> targets, OrcInstance attacker, AttackAction attackAction)
    {
        var cube = AttackDebugScene.Instantiate<Control>();
        VFXLayer.AddChild(cube);

        var attackerTeamId = combatContext.GetTeamId(attacker);
        var enemyTeamId = combatContext.GetTeamId(targets[0]);

        var attackerSlot = combatContext
            .GetUI(attackerTeamId, UI)
            .GetSlot(attacker);

        Vector2 start = attackerSlot.GlobalPosition;

        CharacterParty enemyTeam = combatContext.GetParty(enemyTeamId);
        Vector2 end = GetTargetCenter(targets);

        cube.GlobalPosition = start;

        var tween = cube.CreateTween();

        tween.TweenProperty(cube, "global_position", end, AttackFlightDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        tween.TweenCallback(Callable.From(() =>
        {
            cube.QueueFree();

            if (attackAction == null || attacker == null)
                return;

            ExecuteAttack(attacker, attackAction);
        }));
    }
    Vector2 GetTargetCenter(List<OrcInstance> targets)
    {
        Vector2 sum = Vector2.Zero;
        var teamId = combatContext.GetTeamId(targets[0]);

        foreach (var target in targets)
        {
            var slot = combatContext.GetUI(teamId, UI).GetSlot(target);
            sum += slot.GlobalPosition;
        }
        return sum / targets.Count;
    }
    private void UpdateResolving()
    {
        // placeholder para animaciones / VFX

        SetState(CombatState.WaitingForUnit);
    }

    private void UpdateCheckEnd()
    {
        if (Team1.IsDefeated() || Team2.IsDefeated())
        {
            EndCombat();
            return;
        }

        bool anyActionsLeft = combatContext.UnitState
            .Values
            .Any(u => u.Orc != null && u.Orc.IsAlive && u.RemainingActions > 0);

        if (anyActionsLeft)
        {
            NextTurn();
            return;
        }
        else
        {
            EndCombat();
            return;
        }
    }

    void NextTurn()
    {
        turnNumber++;
        UI?.AddLog($"<<Turno {turnNumber}>>");

        foreach (var kv in combatContext.UnitState) { kv.Value.HasActedThisTurn = false; }

        SetState(CombatState.BuildTurnOrder);
    }

    void EndCombat()
    {
        SetState(CombatState.Ended);
        UI?.AddLog($"<<COMBAT END>>");
        // TODO: poner quién ganó
    }

    /////////////////////////////////////////////////////////////////
    // TARGETING
    List<OrcInstance> ResolveTargets(
        OrcInstance attacker,
        List<OrcInstance> enemies,
        AttackAction.AttackActionTarget targetType)
    {
        if (enemies == null || enemies.Count == 0)
            return new List<OrcInstance>();

        OrcInstance anchor;

        switch (targetType)
        {
            // --------------------------
            // SINGLE TARGET
            // --------------------------

            case AttackAction.AttackActionTarget.AnySingle:
            case AttackAction.AttackActionTarget.RandomSingle:
                anchor = enemies[GameManager.I.NextInt(0, enemies.Count - 1)];
                return new List<OrcInstance> { anchor };

            case AttackAction.AttackActionTarget.CloseSingle:
            default:
                anchor = GetClosestEnemy(enemies, attacker);
                return new List<OrcInstance> { anchor };

            // --------------------------
            // COLUMN TARGETING
            // --------------------------

            case AttackAction.AttackActionTarget.CloseColumn:
                anchor = GetClosestEnemy(enemies, attacker);
                return GetEnemyColumn(anchor, enemies);

            case AttackAction.AttackActionTarget.AnyColumn:
                anchor = enemies[GameManager.I.NextInt(0, enemies.Count - 1)];
                return GetEnemyColumn(anchor, enemies);

            // --------------------------
            // ROW TARGETING
            // --------------------------

            case AttackAction.AttackActionTarget.CloseRow:
                anchor = GetClosestEnemy(enemies, attacker);
                return GetEnemyRow(anchor, enemies);

            case AttackAction.AttackActionTarget.FarRow:
                anchor = GetFarthestEnemy(enemies, attacker);
                return GetEnemyRow(anchor, enemies);

            case AttackAction.AttackActionTarget.AnyRow:
                anchor = enemies[GameManager.I.NextInt(0, enemies.Count - 1)];
                return GetEnemyRow(anchor, enemies);

            // --------------------------
            // GLOBAL
            // --------------------------

            case AttackAction.AttackActionTarget.AllEnemies:
                return enemies;
        }
    }

    List<OrcInstance> GetEnemyColumn(OrcInstance anchor, List<OrcInstance> enemies)
    {
        return enemies.FindAll(e =>
            e.PartyPosition.Column == anchor.PartyPosition.Column);
    }
    List<OrcInstance> GetEnemyRow(OrcInstance anchor, List<OrcInstance> enemies)
    {
        return enemies.FindAll(e =>
            e.PartyPosition.Row == anchor.PartyPosition.Row);
    }
    OrcInstance GetClosestEnemy(List<OrcInstance> enemies, OrcInstance attacker)
    {
        OrcInstance best = null;
        float bestDist = float.MaxValue;
        var attackerSlot = combatContext
            .GetUI(combatContext.GetTeamId(attacker), UI)
            .GetSlot(attacker);

        foreach (var e in enemies)
        {
            var enemySlot = combatContext
                .GetUI(combatContext.GetTeamId(e), UI)
                .GetSlot(e);

            float d = attackerSlot.GlobalPosition.DistanceSquaredTo(enemySlot.GlobalPosition);

            if (d < bestDist)
            {
                bestDist = d;
                best = e;
            }
        }
        return best;
    }
    OrcInstance GetFarthestEnemy(List<OrcInstance> enemies, OrcInstance attacker)
    {
        OrcInstance best = null;
        float bestDist = float.MinValue;
        var attackerSlot = combatContext
            .GetUI(combatContext.GetTeamId(attacker), UI)
            .GetSlot(attacker);

        foreach (var e in enemies)
        {
            var enemySlot = combatContext
                .GetUI(combatContext.GetTeamId(e), UI)
                .GetSlot(e);

            float d = attackerSlot.GlobalPosition.DistanceSquaredTo(enemySlot.GlobalPosition);

            if (d > bestDist)
            {
                bestDist = d;
                best = e;
            }
        }
        return best;
    }
    // TARGETING
    /////////////////////////////////////////////////////////////////

    private void ApplyDamage(OrcInstance attacker, OrcInstance target, AttackAction action)
    {
        int baseDamage = attacker.CharacterClass.GetBaseAttackDamage();
        double multiplier = action.BaseDamageMultiplier;
        int finalDamage = (int)(baseDamage * multiplier);

        float oldHP = target.CurrentHP;

        target.TakeDamage(finalDamage);

        var teamId = combatContext.GetTeamId(target);
        var ui = combatContext.GetUI(teamId, UI);
        var slot = ui.GetSlot(target);

        ui.ShowDamageText(target, finalDamage);

        slot?.UpdateHPBarAnimated(target, oldHP, target.CurrentHP);
        slot?.PlayHitShake(finalDamage);
        slot?.PlayHitSquash(finalDamage);

        if (!target.IsAlive)
            slot?.PlayDeathFade();

        GiveScore(attacker, finalDamage);
    }

    void GiveScore(OrcInstance attacker, int score)
    {
        var teamId = combatContext.GetTeamId(attacker);
        combatContext.AddScore(teamId, score);
    }
    void GiveKillScore(OrcInstance attacker)
    {
        var teamId = combatContext.GetTeamId(attacker);
        combatContext.AddKill(teamId);
    }

    private void Shuffle(List<OrcInstance> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = GameManager.I.NextInt(0, i);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}

public class CombatContext
{
    public CharacterParty Team1;
    public CharacterParty Team2;
    public Dictionary<OrcInstance, CombatUnitState> UnitState;
    public int Score1 = 0;
    public int Score2 = 0;
    public int Kills1 = 0;
    public int Kills2 = 0;
    public enum TeamId
    {
        Team1,
        Team2
    }

    public List<OrcInstance> GetEnemies(OrcInstance source)
    {
        if (Team1.IsMember(source))
            return Team2.GetAllLivingOrcs();

        return Team1.GetAllLivingOrcs();
    }
    public List<OrcInstance> GetAllies(OrcInstance source)
    {
        if (Team1.IsMember(source))
            return Team1.GetAllLivingOrcs();

        return Team2.GetAllLivingOrcs();
    }
    public TeamId GetTeamId(OrcInstance source)
    {
        return Team1.IsMember(source) ? TeamId.Team1 : TeamId.Team2;
    }
    public CharacterParty GetParty(TeamId teamId)
    {
        return teamId == TeamId.Team1 ? Team1 : Team2;
    }
    public UIParty GetUI(TeamId teamId, UICombatScene ui)
    {
        return teamId == TeamId.Team1 ? ui.Team1UI : ui.Team2UI;
    }

    public void AddScore(TeamId team, int score)
    {
        if (team == TeamId.Team1) Score1 += score;
        else Score2 += score;
    }
    public void AddKill(TeamId team)
    {
        if (team == TeamId.Team1) Kills1++;
        else Kills2++;
    }
    public float CalculateAdvantage()
    {
        float tempScore1 = Score1 * (1f + GameManager.I.CombatConfig.ScoreMultiplierPerKill * Kills1);
        float tempScore2 = Score2 * (1f + GameManager.I.CombatConfig.ScoreMultiplierPerKill * Kills2);
        float advantage = tempScore1 / (tempScore1 + tempScore2);
        GD.Print("Advantage " + advantage);

        return advantage; // debería dar entre 0 y 1
    }
}

public class CombatUnitState
{
    public OrcInstance Orc;
    public int RemainingActions = 1;
    public bool HasActedThisTurn = false;
}

