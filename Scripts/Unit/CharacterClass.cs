using Godot;
using System;

[GlobalClass]
public partial class CharacterClass : Resource
{
	[Export] string ClassName = "Clase base";
	[Export] Texture2D ClassTextureFront, ClassTextureBack;
	[Export] Godot.Collections.Array<AttackPerPosition> AttacksPerPosition;

	// STATS AND GROWTH //
	[ExportGroup("Base Stats")]
	[Export] int BaseHP = 0;
	[Export] int BaseStr = 0;
	[Export] int BaseDex = 0;
	[Export] int BaseInt = 0;
	[Export] int BaseWis = 0;
	[Export] int BaseSpd = 10;

	[ExportGroup("Growth")]
	[Export] public int MaxLevel = 10;

	[Export] int GrowthHP = 50;
	[Export] int GrowthStr = 10;
	[Export] int GrowthDex = 10;
	[Export] int GrowthInt = 10;
	[Export] int GrowthWis = 10;
	[Export] int GrowthSpd = 10;
	//

	[Export]
	public CombatConfig.ArmorType ArmorType;
	[Export]
	public MovementType MovementType;

	[Export]
	Godot.Collections.Array<ClassRequirements> Requirements;
	[Export] int XPToNextLevel = 100;

	public string GetClassName() { return ClassName; }
	public Texture2D GetFrontTexture() { return ClassTextureFront; }
	public Texture2D GetBackTexture() { return ClassTextureBack; }
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

	// STAT GETTERS //
	public StatBlock GetBaseStats()
	{
		return new StatBlock
		{
			HP = BaseHP,
			Str = BaseStr,
			Dex = BaseDex,
			Int = BaseInt,
			Wis = BaseWis,
			Spd = BaseSpd
		};
	}
	public StatBlock GetGrowthStatsAtLevel(int level)
	{
		float t = level / (float)MaxLevel;

		return new StatBlock
		{
			HP = Mathf.RoundToInt(GrowthHP * t),
			Str = Mathf.RoundToInt(GrowthStr * t),
			Dex = Mathf.RoundToInt(GrowthDex * t),
			Int = Mathf.RoundToInt(GrowthInt * t),
			Wis = Mathf.RoundToInt(GrowthWis * t),
			Spd = Mathf.RoundToInt(GrowthSpd * t),
		};
	}
}

public enum MovementType
{
	Ground,
	Water,
	Flying,
	Mountain,
	Woods
}

public struct StatBlock
{
	public int HP;
	public int Str;
	public int Dex;
	public int Int;
	public int Wis;
	public int Spd;

	public void Add(StatBlock other)
	{
		HP += other.HP;
		Str += other.Str;
		Dex += other.Dex;
		Int += other.Int;
		Wis += other.Wis;
		Spd += other.Spd;
	}
}