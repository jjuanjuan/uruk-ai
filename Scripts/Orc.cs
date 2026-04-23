using Godot;
using System;

public partial class Orc : Node
{
	[Export]
	string FirstName;
	
	[Export]
	CharacterClass CharacterClass;

	[Export]
	Sprite2D Sprite2D;
	[Export]
	RichTextLabel nameLabel;
	[Export]
	RichTextLabel classLabel;
	[Export]
	RichTextLabel hpLabel;

	int damage = 0;
	bool alive => CurrentHP > 0;

	public override void _Ready()
	{
		nameLabel.Text = FirstName;
		classLabel.Text = CharacterClass.GetClassName();
		hpLabel.Text = CurrentHP.ToString();
		Sprite2D.Texture = CharacterClass.GetFrontTexture();
	}

	public override void _Process(double delta)
	{
		hpLabel.Text = CurrentHP.ToString();
	}

	public int CurrentHP{ get => CharacterClass.GetBaseHP() - damage; }
	public bool IsAlive{ get => alive; }
}
