using Godot;

public partial class UIPartyInfo : Control
{
    [Export] RichTextLabel UnitName;
    [Export] RichTextLabel UnitLvl;
    [Export] RichTextLabel UnitClass;
    [Export] RichTextLabel UnitHP;
    [Export] RichTextLabel UnitSTR;
    [Export] RichTextLabel UnitDEX;
    [Export] RichTextLabel UnitINT;
    [Export] RichTextLabel UnitWIS;
    [Export] RichTextLabel UnitSPD;
    [Export] RichTextLabel UnitArmor;
    [Export] RichTextLabel UnitMovement;
    [Export] RichTextLabel UnitAttack;
    //

    [Export] Button SetLeaderButton;
    [Export] Button ChangeClassButton;
    [Export] Button CloseButton;

    [Export] Control SlotsContainer;

    CharacterParty Party;

    public override void _Ready()
    {
        SelectionManager.I.SelectedOrcChanged += OnSelectedOrcChanged;
        CloseButton.Pressed += CloseMenu;
    }

    void CloseMenu()
    {
        QueueFree(); // TODO: animations
    }

    public override void _ExitTree()
    {
        if (SelectionManager.I != null)
        {
            SelectionManager.I.SelectedOrcChanged -= OnSelectedOrcChanged;
        }
    }

    public void Setup(CharacterParty party)
    {
        if (Party != null)
            Party.PartyChanged -= RefreshParty;

        Party = party;

        if (Party != null)
            Party.PartyChanged += RefreshParty;

        RefreshParty();
    }

    void RefreshParty()
    {
        foreach (PartySlotEdit slot in SlotsContainer.GetChildren())
        {
            slot.Party = Party;

            var orc = Party.GetOrc(
                slot.Row,
                slot.Column
            );

            slot.SetOrc(orc);
        }
    }

    void OnSelectedOrcChanged(OrcInstance orc)
    {
        Refresh(orc);
    }

    void Refresh(OrcInstance orc)
    {
        if (orc == null)
        {
            UnitName.Text = "None selected";
            UnitClass.Text = "";
            UnitLvl.Text = "";
            UnitSTR.Text = "";
            UnitDEX.Text = "";
            UnitINT.Text = "";
            UnitWIS.Text = "";
            UnitSPD.Text = "";
            UnitArmor.Text = "";
            UnitMovement.Text = "";
            UnitAttack.Text = "";
            return;
        }

        var attack = orc.CharacterClass.GetAttackPerPosition(orc.PartyPosition.Row);

        UnitName.Text = orc.GetCustomName();
        UnitClass.Text = orc.CharacterClass.GetClassName() + " lvl X"; // TODO
        UnitLvl.Text = "Lvl 17"; // TODO
        UnitSTR.Text = "" + orc.Str;
        UnitDEX.Text = "" + orc.Dex;
        UnitINT.Text = "" + orc.Int;
        UnitWIS.Text = "" + orc.Wis;
        UnitSPD.Text = "" + orc.Spd;
        UnitArmor.Text = orc.CharacterClass.ArmorType + "armor";
        UnitMovement.Text = "" + orc.CharacterClass.MovementType;
        UnitAttack.Text =
        $@"{attack.AttackAction.AttackName}
        ({attack.AttackAction.AttackType})
        {attack.Amount} times";
    }
}