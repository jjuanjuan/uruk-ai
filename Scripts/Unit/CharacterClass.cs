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
	Godot.Collections.Array<ClassRequirements> Requirements;
	[Export] int XPToNextLevel = 100;

	public string GetClassName() { return ClassName; }
	public int GetBaseHP() { return BaseHP; }
	public Texture2D GetFrontTexture() { return ClassTextureFront; }
	public Texture2D GetBackTexture() { return ClassTextureBack; }
	public AttackPerPosition GetAttackPerPosition(int Row) { return AttacksPerPosition[Row]; }
	public int GetBaseAttackDamage() { return BaseAttackDamage; }
	public int GetBaseSpeed() { return BaseSpeed; }
	public CombatConfig.ArmorType GetArmorType() { return ArmorType; }
	public Godot.Collections.Array<ClassRequirements> GetRequirements() { return Requirements; }
	public int GetXPToNextLevel() { return XPToNextLevel; }
}
