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
                    if (stateTimer < GameManager.I.CombatConfig.AttackDelayWhenUI) return;

                    ExecuteAttack(currentUnit, pendingAction);
                    pendingAction = null;

                    combatContext.UnitState[currentUnit].RemainingActions--;
                    combatContext.UnitState[currentUnit].HasActedThisTurn = true;

                    UI?.SetAdvantageBar(combatContext.CalculateAdvantage());

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

    /////////////////////////////////////////
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
                anchor = GetClosestEnemy(enemies, attacker.PartyPosition);
                return new List<OrcInstance> { anchor };

            // --------------------------
            // COLUMN TARGETING
            // --------------------------

            case AttackAction.AttackActionTarget.CloseColumn:
                anchor = GetClosestEnemy(enemies, attacker.PartyPosition);
                return GetEnemyColumn(anchor, enemies);

            case AttackAction.AttackActionTarget.AnyColumn:
                anchor = enemies[GameManager.I.NextInt(0, enemies.Count - 1)];
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
    OrcInstance GetClosestEnemy(List<OrcInstance> enemies, PartyPosition pos)
    {
        OrcInstance best = null;
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
    OrcInstance GetFarthestEnemy(List<OrcInstance> enemies, PartyPosition pos)
    {
        OrcInstance best = null;
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

    private void ApplyDamage(OrcInstance attacker, OrcInstance target, AttackAction action)
    {
        int baseDamage = attacker.CharacterClass.GetBaseAttackDamage();
        double multiplier = action.BaseDamageMultiplier;
        int finalDamage = (int)(baseDamage * multiplier);

        target.TakeDamage(finalDamage);
        var team = combatContext.GetTeam(target);
        if (team == Team1) UI?.Team1UI.ShowDamageText(target, finalDamage);
        else if (team == Team2) UI?.Team2UI.ShowDamageText(target, finalDamage);
        GiveScore(attacker, finalDamage);
    }

    void GiveScore(OrcInstance attacker, int score)
    {
        var team = combatContext.GetTeam(attacker);
        if (team == combatContext.Team1) combatContext.Score1 += score;
        else if (team == combatContext.Team2) combatContext.Score2 += score;
    }
    void GiveKillScore(OrcInstance attacker)
    {
        var team = combatContext.GetTeam(attacker);
        if (team == combatContext.Team1) combatContext.Kills1++;
        else if (team == combatContext.Team2) combatContext.Kills2++;
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
    public CharacterParty GetTeam(OrcInstance source)
    {
        if (Team1.IsMember(source)) return Team1;
        else return Team2;
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

