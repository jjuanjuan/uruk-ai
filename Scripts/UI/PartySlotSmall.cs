using Godot;

public partial class PartySlotSmall : Control
{
    [Export] public int Row;
    [Export] public int Column;
    [Export] TextureRect CharImg;
    [Export] ColorRect HPBarParent;
    [Export] HealthBar HPBar;
    [Export] Panel Background;

    public CharacterParty Party;
    public UIPartySmall UIParty;
    public OrcInstance Orc;

    public override void _Ready()
    {
        UpdateVisual();
    }

    public void SetOrc(OrcInstance orc)
    {
        Orc = orc;

        if (orc == null)
        {
            UpdateVisual();
            return;
        }

        UpdateVisual();
    }

    public void UpdateVisual()
    {
        if (Orc == null)
        {
            CharImg.Visible = false;
            HPBarParent.Visible = false;
        }
        else
        {
            CharImg.Texture = Orc.CharacterClass.GetFrontTexture();
            //CharImg.Texture = orc.CharacterClass.GetBackTexture();
            CharImg.Visible = true;
            // esto capaz en el caso de mirar partys de enemies?
            // float charNameX = IsFront ? 1f : -1f;
            // CharName.Scale = new Vector2(charNameX, 1f);
            HPBarParent.Visible = true;
            HPBar.SetValue(Orc.CurrentHPPercentile);
        }
    }
}