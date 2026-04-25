using Godot;
using System;

public partial class Orc : Node2D
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
	[Export]
	Godot.Collections.Array<ClassProgress> ClassProgresses = new Godot.Collections.Array<ClassProgress>();

	int damage = 0;
	bool alive => CurrentHP > 0;
	public int CurrentHP { get => Mathf.Max(CharacterClass.GetBaseHP() - damage, 0); }
	public bool IsAlive { get => alive; }
	public PartyPosition PartyPosition;

	public override void _Ready()
	{
		nameLabel.Text = FirstName;
		UpdateClass();
		AddToGroup("orcs");

		SetProcessInput(false);
		SetProcessUnhandledInput(false);

		nameLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
		classLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
		hpLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
	}

	public override void _Process(double delta)
	{
		hpLabel.Text = CurrentHP.ToString();
	}

	public void ChangeClass(CharacterClass characterClass)
	{
		if (!CanChangeToClass(characterClass))
		{
			GD.Print("No cumple requisitos");
			return;
		}

		CharacterClass = characterClass;
		UpdateClass();
	}

	public void UpdateClass()
	{
		classLabel.Text = CharacterClass.GetClassName();
		hpLabel.Text = CurrentHP.ToString();
		Sprite2D.Texture = CharacterClass.GetFrontTexture();
	}

	public ClassProgress GetProgress(CharacterClass characterClass)
	{
		foreach (var cp in ClassProgresses)
		{
			if (cp.CharacterClass == characterClass)
				return cp;
		}

		// si no existe, crear
		var newProgress = new ClassProgress
		{
			CharacterClass = characterClass,
			Level = 0,
			XP = 0
		};

		ClassProgresses.Add(newProgress);
		return newProgress;
	}

	public bool CanChangeToClass(CharacterClass newClass)
	{
		foreach (var req in newClass.GetRequirements())
		{
			var progress = GetProgress(req.RequiredClass);

			if (progress.Level < req.RequiredLevel)
				return false;
		}

		return true;
	}

	public void GainXP(int amount)
	{
		var progress = GetProgress(CharacterClass);

		progress.XP += amount;

		int xpToLevel = CharacterClass.GetXPToNextLevel();

		while (progress.XP >= xpToLevel)
		{
			progress.XP -= xpToLevel;
			progress.Level++;

			GD.Print($"Subió a nivel {progress.Level} en {CharacterClass.GetClassName()}");
		}
	}

	public void TakeDamage(int amount)
	{
		damage += amount;
	}
	public void Heal(int amount)
	{
		damage = Mathf.Max(damage - amount, 0);
	}

	public string GetFirstName() { return FirstName; }
	public CharacterClass GetCharacterClass() { return CharacterClass; }
}
