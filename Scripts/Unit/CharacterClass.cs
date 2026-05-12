using Godot;
using System;

[GlobalClass]
public partial class CharacterClass : Resource
{
	[Export]
	string ClassName = "Clase base";
	[Export]
	int BaseHP = 100;
	[Export]
	Texture2D ClassTextureFront, ClassTextureBack;
	[Export]
	Godot.Collections.Array<AttackPerPosition> AttacksPerPosition;
	[Export]
	int BaseAttackDamage = 10;
	[Export]
	int BaseSpeed = 1;
	[Export]
	CombatConfig.ArmorType ArmorType;
	[Export]
	public MovementType MovementType;

	[Export]
	Godot.Collections.Array<ClassRequirements> Requirements;
	[Export] int XPToNextLevel = 100;

	public string GetClassName() { return ClassName; }
	public int GetBaseHP() { return BaseHP; }
	public Texture2D GetFrontTexture() { return ClassTextureFront; }
	public Texture2D GetBackTexture() { return ClassTextureBack; }
	public int GetBaseAttackDamage() { return BaseAttackDamage; }
	public int GetBaseSpeed() { return BaseSpeed; }
	public CombatConfig.ArmorType GetArmorType() { return ArmorType; }
	public Godot.Collections.Array<ClassRequirements> GetRequirements() { return Requirements; }
	public int GetXPToNextLevel() { return XPToNextLevel; }
	public AttackPerPosition GetAttackPerPosition(int Row)
	{
		var position = Row;
		switch (Row)
		{
			case 1:
			case 2:
				position = 1;
				break;
			case 3:
			case 4:
				position = 2;
				break;
			case 0:
			default:
				position = 0;
				break;
		}

		return AttacksPerPosition[position];
	}

	// row 0 back
	// row 1 y 2 middle
	// row 3 y 4 front
}

public enum MovementType
{
	Ground,
	Water,
	Flying,
	Mountain,
	Woods
}