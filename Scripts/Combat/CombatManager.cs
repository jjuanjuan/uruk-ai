using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CombatManager : Node
{
    public CharacterParty PartyFront;
    public CharacterParty PartyBack;

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

    [Signal]
    public delegate void CombatFinishedEventHandler(
        CharacterParty winner,
        CharacterParty loser,
        bool isDraw
    );
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

        GD.Print("Combat " + state.ToString());

        if (state == CombatState.WaitingForStartDelay)
        {
            UI.Team1UI.SetNamesVisible(false);
            UI.Team2UI.SetNamesVisible(false);
        }
        else if (state == CombatState.Ended)
        {
            UI.Team1UI.SetNamesVisible(true);
            UI.Team2UI.SetNamesVisible(true);
        }

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
                SetState(CombatState.WaitingForUnit);
                break;

            case CombatState.CheckEnd:
                UpdateCheckEnd();
                break;

            case CombatState.Ended:
                break;
        }
    }

    void InitCombat()
    {
        UI?.SetAdvantageBar(.5f);
        SetState(CombatState.WaitingForStart);
    }
    public void StartCombat()
    {
        if (state == CombatState.WaitingForStart)
            SetState(CombatState.WaitingForStartDelay);
    }
    public void SetupCombat(CharacterParty partyFront, CharacterParty partyBack)
    {
        PartyFront = partyFront;
        PartyBack = partyBack;

        combatContext = new CombatContext
        {
            Team1 = PartyFront,
            Team2 = PartyBack,
            UnitState = new Dictionary<OrcInstance, CombatUnitState>(),
            Score1 = 0,
            Score2 = 0,
        };

        InitUnitState();
    }

    void InitUnitState()
    {
        combatContext.UnitState.Clear();

        var print = "<<Turn 0>>";
        print += "\nTeam 1: ";

        foreach (var o in PartyFront.GetAllLivingOrcs())
        {
            AddUnit(o);
            print += o.GetCustomName() + ", ";
        }

        print += "\nTeam 2: ";

        foreach (var o in PartyBack.GetAllLivingOrcs())
        {
            AddUnit(o);
            print += o.GetCustomName() + ", ";
        }

        UI?.AddLog(print);
    }

    void AddUnit(OrcInstance o)
    {
        if (o == null) return;

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

        // solo orcos vivos que tengan acciones
        var all = combatContext.UnitState
            .Where(kv => kv.Value.Orc.IsAlive && kv.Value.RemainingActions > 0)
            .Select(kv => kv.Key)
            .ToList();

        var groups = new Dictionary<int, List<OrcInstance>>();

        foreach (var orc in all)
        {
            if (orc == null) continue;

            int speed = orc.Spd;
            int extraUnits = Mathf.Max(0, orc.CurrentParty.CurrentUnits - 1);

            int speedCalc = Mathf.RoundToInt(speed * (
                1f - extraUnits * GameManager.I.CombatConfig.SpeedDecreasePerSize
            ));

            if (!groups.ContainsKey(speedCalc))
                groups[speedCalc] = new List<OrcInstance>();

            groups[speedCalc].Add(orc);
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
        GD.Print(orc.CharacterClass.GetClassName() + " attacks with " +

                    orc.CharacterClass
                          .GetAttackPerPosition(orc.PartyPosition.Row)
                          .AttackAction.AttackName);

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

        UI?.AnimateAdvantageBar(combatContext.CalculateAdvantage());

        var state = combatContext.UnitState[attacker];
        state.RemainingActions--;
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

        Vector2 start = attackerSlot.GetHitPosition();

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
            sum += slot.GetHitPosition();
        }
        return sum / targets.Count;
    }

    // =========================================================
    // END
    // =========================================================
    void UpdateCheckEnd()
    {
        // termina solo si alguno murió completamente
        if (!PartyFront.HasLivingOrcs() || !PartyBack.HasLivingOrcs())
        {
            EndCombat();
            return;
        }

        // si quedan acciones → siguiente turno
        if (combatContext.HasActionsLeft())
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
        UI?.AddLog($"<<Turno {turnNumber}>>");

        foreach (var kv in combatContext.UnitState)
            kv.Value.HasActedThisTurn = false;

        SetState(CombatState.BuildTurnOrder);
    }

    void EndCombat()
    {
        SetState(CombatState.Ended);
        UI?.AddLog($"<<COMBAT END>>");

        CharacterParty winner = null;
        CharacterParty loser = null;
        bool isDraw = false;

        float adv = combatContext.CalculateAdvantage();

        if (adv > 0.5f)
        {
            winner = PartyFront;
            loser = PartyBack;
            UI?.AddLog("<<TEAM 1 WINS!>>");
            GD.Print("<<TEAM 1 WINS!>>");
        }
        else if (adv < 0.5f)
        {
            winner = PartyBack;
            loser = PartyFront;
            UI?.AddLog("<<TEAM 2 WINS!>>");
            GD.Print("<<TEAM 2 WINS!>>");
        }
        else
        {
            isDraw = true;
            UI?.AddLog("<<COMBAT IS TIED!!>>");
            GD.Print("<<COMBAT IS TIED!!>>");
        }

        EmitSignal(SignalName.CombatFinished, winner, loser, isDraw);

        SetState(CombatState.Ended);
    }

    // =========================================================
    // TARGETING
    // =========================================================
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

            case AttackAction.AttackActionTarget.FarSingle:
                anchor = GetFarthestEnemy(enemies, attacker);
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
        int col = anchor.PartyPosition.Column;

        int min = Mathf.Max(0, col - 1);
        int max = Mathf.Min(CharacterParty.COLUMNS - 1, col + 1);

        return enemies.FindAll(e =>
            e.PartyPosition.Column >= min &&
            e.PartyPosition.Column <= max);
    }
    List<OrcInstance> GetEnemyRow(OrcInstance anchor, List<OrcInstance> enemies)
    {
        int row = anchor.PartyPosition.Column;

        int min = Mathf.Max(row - 1, 0);
        int max = Mathf.Min(row + 1, CharacterParty.COLUMNS - 1);

        return enemies.FindAll(e =>
            e.PartyPosition.Row >= min &&
            e.PartyPosition.Row <= max);
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
    /////////////////////////////////////////////////////////////////

    private void ApplyDamage(OrcInstance attacker, OrcInstance target, AttackAction action)
    {
        int baseDamage = action.BaseDamage;
        float strBonus = action.StrFactor * attacker.Str / 100f;
        float dexBonus = action.DexFactor * attacker.Dex / 100f;
        float intBonus = action.IntFactor * attacker.Int / 100f;
        float wisBonus = action.WisFactor * attacker.Wis / 100f;

        float statMultiplier =
            1f +
            strBonus +
            dexBonus +
            intBonus +
            wisBonus;

        float typeMultiplier =
            GameManager.I.CombatConfig.GetMultiplier(
                action.AttackType,
                target.CharacterClass.ArmorType
            );

        float wisDefenderMultiplier = 1f;
        switch (action.AttackType)
        {
            case CombatConfig.AttackType.Fire:
            case CombatConfig.AttackType.Ice:
            case CombatConfig.AttackType.Electric:
                wisDefenderMultiplier =
                    Mathf.Max(0.35f, 200f / (200f + target.Wis)); // 100 WIS = 33% reducción
                break;
            default:
                break;
        }

        int finalDamage = Mathf.RoundToInt(
            baseDamage *
            statMultiplier *
            typeMultiplier *
            wisDefenderMultiplier
        );

        float oldHP = target.CurrentHP;

        target.TakeDamage(finalDamage);

        var teamId = combatContext.GetTeamId(target);
        var ui = combatContext.GetUI(teamId, UI);
        var slot = ui.GetSlot(target);

        ui.ShowDamageText(target, finalDamage);

        slot?.UpdateHPBarAnimated(target, oldHP, target.CurrentHP);
        slot?.PlayHitShake(finalDamage);
        slot?.PlayHitSquash(finalDamage);
        slot?.PlayHitFlash();

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
    public UIPartyCombat GetUI(TeamId teamId, UICombatScene ui)
    {
        return TeamId.Team1 == teamId ? ui.Team1UI : ui.Team2UI;
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

public enum EncounterType
{
    None,
    A_Ambushes_B,
    B_Ambushes_A,
    Equal,
}