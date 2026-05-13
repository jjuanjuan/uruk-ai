using Godot;

public partial class SelectionManager : Node
{
    public static SelectionManager I;

    [Signal] public delegate void SelectedOrcChangedEventHandler(OrcInstance orc);
    [Signal] public delegate void SelectedMapUnitChangedEventHandler(MapUnit unit);
    [Signal] public delegate void SelectedPartyChangedEventHandler(CharacterParty party);

    public OrcInstance SelectedOrc;
    public MapUnit SelectedMapUnit;
    public CharacterParty SelectedParty;

    public override void _EnterTree()
    {
        I = this;
    }

    // =====================================================
    // ORCS
    // =====================================================

    public void SelectOrc(OrcInstance orc)
    {
        SelectedOrc = orc;

        EmitSignal(
            SignalName.SelectedOrcChanged,
            orc
        );
    }

    public void ClearOrcSelection()
    {
        SelectedOrc = null;

        EmitSignal(
            SignalName.SelectedOrcChanged,
            (OrcInstance)null
        );
    }

    // =====================================================
    // MAP UNITS
    // =====================================================

    public void SelectMapUnit(MapUnit unit)
    {
        if (SelectedMapUnit != null)
            SelectedMapUnit.SetSelected(false);

        SelectedMapUnit = unit;

        if (SelectedMapUnit != null)
            SelectedMapUnit.SetSelected(true);

        EmitSignal(
            SignalName.SelectedMapUnitChanged,
            unit
        );
    }

    public void ClearMapUnitSelection()
    {
        if (SelectedMapUnit != null &&
            IsInstanceValid(SelectedMapUnit))
        {
            SelectedMapUnit.SetSelected(false);
        }

        SelectedMapUnit = null;

        EmitSignal(
            SignalName.SelectedMapUnitChanged,
            (MapUnit)null
        );
    }

    // =====================================================
    // Select Party
    // =====================================================
    public void SelectParty(CharacterParty party)
    {
        SelectedParty = party;

        EmitSignal(
            SignalName.SelectedPartyChanged,
            party
        );
    }

    public void ClearPartySelection()
    {
        SelectedParty = null;

        EmitSignal(
            SignalName.SelectedPartyChanged,
            (CharacterParty)null
        );
    }
}