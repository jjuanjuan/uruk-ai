using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CombatManager : Node
{
    [Export] public CharacterParty Team1;
    [Export] public CharacterParty Team2;
    [Export] public UICombatScene UI;
    [Export] CombatConfig CombatConfig;

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
        CheckVictory,
        Ended
    }

    CombatState state = CombatState.Init;
    float stateTimer = 0f;
    CombatContext combatContext;

    List<Orc> turnOrder = new();
    int currentIndex = 0;
    Orc currentUnit;
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
    [Signal] public delegate void UnitChangedEventHandler(Orc unit);

    private RowType GetRow(int row)
    {
        return row switch
        {
            0 => RowType.Front,
            1 => RowType.Middle,
            _ => RowType.Back
        };
    }
    //

    private void SetState(CombatState newState)
    {
        state = newState;
        stateTimer = 0f;

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
                    if (stateTimer >= CombatConfig.CombatStartDelay)
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
                    if (stateTimer < CombatConfig.AttackDelayWhenUI) return;

                    ExecuteAttack(currentUnit, pendingAction);
                    pendingAction = null;

                    combatContext.UnitState[currentUnit].RemainingActions--;
                    combatContext.UnitState[currentUnit].HasActedThisTurn = true;

                    SetState(CombatState.WaitingAttackInterval);
                    break;
                }

            case CombatState.WaitingAttackInterval:
                {
                    if (stateTimer < CombatConfig.AttackInterval)
                        return;

                    SetState(CombatState.Resolving);
                    break;
                }

            case CombatState.UnitActing:
                break;

            case CombatState.Resolving:
                UpdateResolving();
                break;

            case CombatState.CheckVictory:
                UpdateCheckVictory();
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
            UnitState = new Dictionary<Orc, CombatUnitState>(),
        };

        InitUnitState();
        SetState(CombatState.WaitingForStart);
    }
    public void StartCombat()
    {
        if (state == CombatState.WaitingForStart)
            SetState(CombatState.WaitingForStartDelay);
    }

    private void InitUnitState()
    {
        combatContext.UnitState.Clear();

        foreach (var o in combatContext.Team1.GetAllLivingOrcs())
            AddUnit(o);

        foreach (var o in combatContext.Team2.GetAllLivingOrcs())
            AddUnit(o);
    }

    private void AddUnit(Orc o)
    {
        if (o == null) return;

        combatContext.UnitState[o] = new CombatUnitState
        {
            Orc = o,
            RemainingActions = o.GetCharacterClass().GetAttackPerPosition(o.PartyPosition.Row).Amount,
            HasActedThisTurn = false
        };
    }

    private void BuildTurnOrder()
    {
        turnOrder.Clear();
        currentIndex = 0;

        var all = new List<Orc>(combatContext.UnitState.Keys);

        var groups = new Dictionary<int, List<Orc>>();

        foreach (var orc in all)
        {
            if (orc == null) continue;

            int speed = orc.GetCharacterClass().GetBaseSpeed();

            if (!groups.ContainsKey(speed))
                groups[speed] = new List<Orc>();

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

        SetState(CombatState.CheckVictory);
    }

    private void EnterUnitActing()
    {
        if (currentUnit == null)
        {
            SetState(CombatState.CheckVictory);
            return;
        }

        if (pendingAction == null)
        {
            pendingAction = GetActionByRow(currentUnit);

            SetState(CombatState.WaitingAttackDelay);
        }
    }
    AttackAction GetActionByRow(Orc orc)
    {
        return orc.GetCharacterClass()
                  .GetAttackPerPosition(orc.PartyPosition.Row)
                  .AttackAction;
    }
    private void ExecuteAttack(Orc attacker, AttackAction action)
    {
        var enemies = combatContext.GetEnemies(attacker);
        var targets = ResolveTargets(attacker, enemies, action.Target);

        string names = string.Join(", ", targets.Select(t => t.GetFirstName()));
        UI?.AddLog($"{attacker.GetFirstName()} hits {names}");

        foreach (var t in targets)
        {
            ApplyDamage(attacker, t, action);
        }
    }
    private void UpdateResolving()
    {
        // placeholder para animaciones / VFX

        SetState(CombatState.WaitingForUnit);
    }

    private void UpdateCheckVictory()
    {
        if (Team1.IsDefeated() || Team2.IsDefeated())
        {
            SetState(CombatState.Ended);
            GD.Print("Combat finished");
            return;
        }

        NextTurn();
    }

    private void NextTurn()
    {
        turnNumber++;

        foreach (var kv in combatContext.UnitState) { kv.Value.HasActedThisTurn = false; }

        SetState(CombatState.BuildTurnOrder);
    }

    /////////////////////////////////////////
    // TARGETING
    List<Orc> ResolveTargets(
        Orc attacker,
        List<Orc> enemies,
        AttackAction.AttackActionTarget targetType)
    {
        if (enemies == null || enemies.Count == 0)
            return new List<Orc>();

        Orc anchor;

        switch (targetType)
        {
            // --------------------------
            // SINGLE TARGET
            // --------------------------

            case AttackAction.AttackActionTarget.AnySingle:
            case AttackAction.AttackActionTarget.RandomSingle:
                anchor = enemies[GameManager.I.NextInt(0, enemies.Count)];
                return new List<Orc> { anchor };

            case AttackAction.AttackActionTarget.CloseSingle:
            default:
                anchor = GetClosestEnemy(enemies, attacker.PartyPosition);
                return new List<Orc> { anchor };

            // --------------------------
            // COLUMN TARGETING
            // --------------------------

            case AttackAction.AttackActionTarget.CloseColumn:
                anchor = GetClosestEnemy(enemies, attacker.PartyPosition);
                return GetEnemyColumn(anchor, enemies);

            case AttackAction.AttackActionTarget.AnyColumn:
                anchor = enemies[GameManager.I.NextInt(0, enemies.Count)];
                return GetEnemyColumn(anchor, enemies);

            // --------------------------
            // ROW TARGETING
            // --------------------------

            case AttackAction.AttackActionTarget.CloseRow:
                anchor = GetClosestEnemy(enemies, attacker.PartyPosition);
                return GetEnemyRow(anchor, enemies);

            case AttackAction.AttackActionTarget.FarRow:
                anchor = GetFarthestEnemy(enemies, attacker.PartyPosition);
                return GetEnemyRow(anchor, enemies);

            case AttackAction.AttackActionTarget.AnyRow:
                anchor = enemies[GameManager.I.NextInt(0, enemies.Count)];
                return GetEnemyRow(anchor, enemies);

            // --------------------------
            // GLOBAL
            // --------------------------

            case AttackAction.AttackActionTarget.AllEnemies:
                return enemies;
        }
    }

    List<Orc> GetEnemyColumn(Orc anchor, List<Orc> enemies)
    {
        return enemies.FindAll(e =>
            e.PartyPosition.Column == anchor.PartyPosition.Column);
    }
    List<Orc> GetEnemyRow(Orc anchor, List<Orc> enemies)
    {
        return enemies.FindAll(e =>
            e.PartyPosition.Row == anchor.PartyPosition.Row);
    }
    Orc GetClosestEnemy(List<Orc> enemies, PartyPosition pos)
    {
        Orc best = null;
        int bestDist = int.MaxValue;

        foreach (var e in enemies)
        {
            int d = Mathf.Abs(e.PartyPosition.Row - pos.Row)
                  + Mathf.Abs(e.PartyPosition.Column - pos.Column);

            if (d < bestDist)
            {
                bestDist = d;
                best = e;
            }
        }

        return best;
    }
    Orc GetFarthestEnemy(List<Orc> enemies, PartyPosition pos)
    {
        Orc best = null;
        int bestDist = -1;

        foreach (var e in enemies)
        {
            int d = Mathf.Abs(e.PartyPosition.Row - pos.Row)
                  + Mathf.Abs(e.PartyPosition.Column - pos.Column);

            if (d > bestDist)
            {
                bestDist = d;
                best = e;
            }
        }

        return best;
    }
    // TARGETING
    /////////////////////////////////////////

    private void ApplyDamage(Orc attacker, Orc target, AttackAction action)
    {
        int baseDamage = attacker.GetCharacterClass().GetBaseAttackDamage();
        double multiplier = action.BaseDamageMultiplier;
        int finalDamage = (int)(baseDamage * multiplier);

        target.TakeDamage(finalDamage);
    }

    private void Shuffle(List<Orc> list)
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
    public Dictionary<Orc, CombatUnitState> UnitState;

    public List<Orc> GetEnemies(Orc source)
    {
        if (Team1.IsMember(source))
            return Team2.GetAllLivingOrcs();

        return Team1.GetAllLivingOrcs();
    }

    public List<Orc> GetAllies(Orc source)
    {
        if (Team1.IsMember(source))
            return Team1.GetAllLivingOrcs();

        return Team2.GetAllLivingOrcs();
    }
}

public class CombatUnitState
{
    public Orc Orc;
    public int RemainingActions = 1;
    public bool HasActedThisTurn = false;
}

