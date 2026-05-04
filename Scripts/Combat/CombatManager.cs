using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CombatManager : Node
{
    public CharacterParty PartyFront;
    public CharacterParty PartyBack;

    [Export] public float AttackFlightDuration = 0.5f;

    public enum CombatState
    {
        Init,
        WaitingForStart,
        WaitingForStartDelay,
        BuildTurnOrder,
        WaitingForUnit,
        UnitActing,
        WaitingAttackDelay,
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

    // =========================
    // SIGNALS (UI / VFX hooks)
    // =========================

    [Signal] public delegate void CombatStateChangedEventHandler();
    [Signal] public delegate void UnitChangedEventHandler(OrcInstance unit);

    [Signal]
    public delegate void AttackStartedEventHandler(
        OrcInstance attacker,
        Godot.Collections.Array<OrcInstance> targets,
        AttackAction action
    );

    [Signal]
    public delegate void DamageAppliedEventHandler(
        OrcInstance attacker,
        OrcInstance target,
        int damage,
        bool died
    );

    [Signal] public delegate void CombatFinishedEventHandler();
    [Signal] public delegate void CombatLogEventHandler(string text);

    // =========================

    public override void _Ready()
    {
        GameManager.I.CombatManager = this;
    }

    public override void _ExitTree()
    {
        if (GameManager.I.CombatManager == this)
            GameManager.I.CombatManager = null;
    }

    void SetState(CombatState newState)
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
                if (stateTimer >= GameManager.I.CombatConfig.CombatStartDelay)
                    SetState(CombatState.BuildTurnOrder);
                break;

            case CombatState.BuildTurnOrder:
                BuildTurnOrder();
                break;

            case CombatState.WaitingForUnit:
                UpdateWaitingForUnit();
                break;

            case CombatState.UnitActing:
                break;

            case CombatState.WaitingAttackDelay:
                if (stateTimer >= GameManager.I.CombatConfig.AttackDelayWhenUI)
                    StartAttack();
                break;

            case CombatState.WaitingAttackInterval:
                if (stateTimer >= GameManager.I.CombatConfig.AttackInterval)
                    ResolveAttack();
                break;

            case CombatState.Resolving:
                SetState(CombatState.WaitingForUnit);
                break;

            case CombatState.CheckEnd:
                UpdateCheckEnd();
                break;

            case CombatState.Ended:
                break;
        }
    }

    // =========================================================
    // INIT
    // =========================================================

    void InitCombat()
    {
        combatContext = new CombatContext
        {
            Team1 = PartyFront,
            Team2 = PartyBack,
            UnitState = new Dictionary<OrcInstance, CombatUnitState>(),
            Score1 = 0,
            Score2 = 0
        };

        InitUnitState();

        EmitSignal("CombatLog", "<<COMBAT START>>");

        SetState(CombatState.WaitingForStart);
    }

    public void StartCombat(CharacterParty partyFront, CharacterParty partyBack)
    {
        PartyFront = partyFront;
        PartyBack = partyBack;

        InitCombat();
        SetState(CombatState.WaitingForStartDelay);
    }

    void InitUnitState()
    {
        combatContext.UnitState.Clear();

        foreach (var o in PartyFront.GetAllLivingOrcs())
            AddUnit(o);

        foreach (var o in PartyBack.GetAllLivingOrcs())
            AddUnit(o);
    }

    void AddUnit(OrcInstance o)
    {
        combatContext.UnitState[o] = new CombatUnitState
        {
            Orc = o,
            RemainingActions =
                o.CharacterClass
                 .GetAttackPerPosition(o.PartyPosition.Row)
                 .Amount,
            HasActedThisTurn = false
        };
    }

    // =========================================================
    // TURN ORDER
    // =========================================================

    void BuildTurnOrder()
    {
        turnOrder.Clear();
        currentIndex = 0;

        var all = combatContext.UnitState
            .Where(kv => kv.Value.Orc.IsAlive && kv.Value.RemainingActions > 0)
            .Select(kv => kv.Key)
            .ToList();

        var groups = new Dictionary<int, List<OrcInstance>>();

        foreach (var orc in all)
        {
            int speed = orc.CharacterClass.GetBaseSpeed();

            if (!groups.ContainsKey(speed))
                groups[speed] = new List<OrcInstance>();

            groups[speed].Add(orc);
        }

        var speeds = groups.Keys.ToList();
        speeds.Sort();
        speeds.Reverse();

        foreach (var speed in speeds)
        {
            Shuffle(groups[speed]);
            turnOrder.AddRange(groups[speed]);
        }

        SetState(CombatState.WaitingForUnit);
    }

    void UpdateWaitingForUnit()
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

            pendingAction = GetActionByRow(currentUnit);

            SetState(CombatState.WaitingAttackDelay);
            return;
        }

        SetState(CombatState.CheckEnd);
    }

    // =========================================================
    // ATTACK FLOW
    // =========================================================

    void StartAttack()
    {
        if (currentUnit == null || pendingAction == null)
        {
            SetState(CombatState.CheckEnd);
            return;
        }

        var enemies = combatContext.GetEnemies(currentUnit);
        var targets = ResolveTargets(currentUnit, enemies, pendingAction.Target);

        EmitSignal(
            SignalName.AttackStarted,
            currentUnit,
            new Godot.Collections.Array<OrcInstance>(targets),
            pendingAction
        );

        SetState(CombatState.WaitingAttackInterval);
    }

    void ResolveAttack()
    {
        if (currentUnit == null || pendingAction == null)
        {
            SetState(CombatState.CheckEnd);
            return;
        }

        var enemies = combatContext.GetEnemies(currentUnit);
        var targets = ResolveTargets(currentUnit, enemies, pendingAction.Target);

        foreach (var t in targets)
        {
            ApplyDamage(currentUnit, t, pendingAction);
        }

        combatContext.UnitState[currentUnit].RemainingActions--;

        pendingAction = null;

        SetState(CombatState.Resolving);
    }

    // =========================================================
    // DAMAGE
    // =========================================================

    void ApplyDamage(OrcInstance attacker, OrcInstance target, AttackAction action)
    {
        int baseDamage = attacker.CharacterClass.GetBaseAttackDamage();
        int finalDamage = (int)(baseDamage * action.BaseDamageMultiplier);

        target.TakeDamage(finalDamage);

        bool died = !target.IsAlive;

        EmitSignal(
            SignalName.DamageApplied,
            attacker,
            target,
            finalDamage,
            died
        );

        GiveScore(attacker, finalDamage);

        if (died)
            GiveKillScore(attacker);
    }

    // =========================================================
    // END
    // =========================================================
    void UpdateCheckEnd()
    {
        if (PartyFront.HasLivingOrcs() || PartyBack.HasLivingOrcs())
        {
            EndCombat();
            return;
        }

        bool anyActionsLeft = combatContext.UnitState
            .Values
            .Any(u => u.Orc.IsAlive && u.RemainingActions > 0);

        if (anyActionsLeft)
        {
            NextTurn();
        }
        else
        {
            EndCombat();
        }
    }

    void NextTurn()
    {
        turnNumber++;

        EmitSignal("CombatLog", $"<<TURN {turnNumber}>>");

        foreach (var kv in combatContext.UnitState)
            kv.Value.HasActedThisTurn = false;

        SetState(CombatState.BuildTurnOrder);
    }

    void EndCombat()
    {
        EmitSignal("CombatLog", "<<COMBAT END>>");

        float adv = combatContext.CalculateAdvantage();

        if (adv > 0.5f)
            EmitSignal("CombatLog", "<<TEAM 1 WINS>>");
        else if (adv < 0.5f)
            EmitSignal("CombatLog", "<<TEAM 2 WINS>>");
        else
            EmitSignal("CombatLog", "<<DRAW>>");

        EmitSignal(SignalName.CombatFinished);

        SetState(CombatState.Ended);
    }
    // =========================================================
    // TARGETING
    // =========================================================

    AttackAction GetActionByRow(OrcInstance orc)
    {
        return orc.CharacterClass
            .GetAttackPerPosition(orc.PartyPosition.Row)
            .AttackAction;
    }

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
            case AttackAction.AttackActionTarget.RandomSingle:
                anchor = enemies[GameManager.I.NextInt(0, enemies.Count - 1)];
                return new() { anchor };

            case AttackAction.AttackActionTarget.CloseSingle:
            default:
                anchor = GetClosestEnemy(enemies, attacker);
                return new() { anchor };

            case AttackAction.AttackActionTarget.CloseColumn:
                anchor = GetClosestEnemy(enemies, attacker);
                return GetEnemyColumn(anchor, enemies);

            case AttackAction.AttackActionTarget.AnyColumn:
                anchor = enemies[GameManager.I.NextInt(0, enemies.Count - 1)];
                return GetEnemyColumn(anchor, enemies);

            case AttackAction.AttackActionTarget.CloseRow:
                anchor = GetClosestEnemy(enemies, attacker);
                return GetEnemyRow(anchor, enemies);

            case AttackAction.AttackActionTarget.FarRow:
                anchor = GetFarthestEnemy(enemies, attacker);
                return GetEnemyRow(anchor, enemies);

            case AttackAction.AttackActionTarget.AnyRow:
                anchor = enemies[GameManager.I.NextInt(0, enemies.Count - 1)];
                return GetEnemyRow(anchor, enemies);

            case AttackAction.AttackActionTarget.AllEnemies:
                return enemies;
        }
    }

    List<OrcInstance> GetEnemyColumn(OrcInstance anchor, List<OrcInstance> enemies)
    {
        int col = anchor.PartyPosition.Column;

        int min = Mathf.Max(0, col - 1);
        int max = Mathf.Min(CharacterParty.COLUMNS - 1, col + 1);

        return enemies.FindAll(e =>
            e.PartyPosition.Column >= min &&
            e.PartyPosition.Column <= max);
    }

    List<OrcInstance> GetEnemyRow(OrcInstance anchor, List<OrcInstance> enemies)
    {
        return enemies.FindAll(e =>
            e.PartyPosition.Row == anchor.PartyPosition.Row);
    }

    OrcInstance GetClosestEnemy(List<OrcInstance> enemies, OrcInstance attacker)
    {
        return enemies
            .OrderBy(e => Distance(attacker, e))
            .First();
    }

    OrcInstance GetFarthestEnemy(List<OrcInstance> enemies, OrcInstance attacker)
    {
        return enemies
            .OrderByDescending(e => Distance(attacker, e))
            .First();
    }

    float Distance(OrcInstance a, OrcInstance b)
    {
        return Mathf.Abs(a.PartyPosition.Row - b.PartyPosition.Row)
             + Mathf.Abs(a.PartyPosition.Column - b.PartyPosition.Column);
    }

    // =========================================================
    // UTILS
    // =========================================================

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

    void Shuffle(List<OrcInstance> list)
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

    // =====================================
    // TEAM RESOLUTION
    // =====================================

    public TeamId GetTeamId(OrcInstance orc)
    {
        if (Team1.IsMember(orc)) return TeamId.Team1;
        if (Team2.IsMember(orc)) return TeamId.Team2;

        GD.PrintErr("Orc not found in any team");
        return TeamId.Team1; // fallback defensivo
    }

    public CharacterParty GetParty(TeamId id)
    {
        return id == TeamId.Team1 ? Team1 : Team2;
    }

    public CharacterParty GetPartyOf(OrcInstance orc)
    {
        return GetParty(GetTeamId(orc));
    }

    // =====================================
    // RELATIONS
    // =====================================

    public List<OrcInstance> GetEnemies(OrcInstance source)
    {
        return GetTeamId(source) == TeamId.Team1
            ? Team2.GetAllLivingOrcs()
            : Team1.GetAllLivingOrcs();
    }

    public List<OrcInstance> GetAllies(OrcInstance source)
    {
        return GetPartyOf(source).GetAllLivingOrcs();
    }

    // =====================================
    // STATE HELPERS
    // =====================================

    public bool HasActionsLeft()
    {
        return UnitState.Values.Any(u =>
            u.Orc != null &&
            u.Orc.IsAlive &&
            u.RemainingActions > 0
        );
    }

    public IEnumerable<OrcInstance> GetAllActableUnits()
    {
        return UnitState.Values
            .Where(u => u.Orc.IsAlive && u.RemainingActions > 0)
            .Select(u => u.Orc);
    }

    // =====================================
    // SCORE
    // =====================================

    public void AddScore(OrcInstance orc, int score)
    {
        AddScore(GetTeamId(orc), score);
    }

    public void AddScore(TeamId team, int score)
    {
        if (team == TeamId.Team1) Score1 += score;
        else Score2 += score;
    }

    public void AddKill(OrcInstance orc)
    {
        AddKill(GetTeamId(orc));
    }

    public void AddKill(TeamId team)
    {
        if (team == TeamId.Team1) Kills1++;
        else Kills2++;
    }

    public float CalculateAdvantage()
    {
        float temp1 = Score1 * (1f + GameManager.I.CombatConfig.ScoreMultiplierPerKill * Kills1);
        float temp2 = Score2 * (1f + GameManager.I.CombatConfig.ScoreMultiplierPerKill * Kills2);

        if (temp1 + temp2 == 0)
            return 0.5f;

        return temp1 / (temp1 + temp2);
    }
}

public class CombatUnitState
{
    public OrcInstance Orc;
    public int RemainingActions = 1;
    public bool HasActedThisTurn = false;
}