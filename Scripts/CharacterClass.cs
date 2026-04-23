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

	public string GetClassName() { return ClassName; }
	public int GetBaseHP() { return BaseHP; }
	public Texture2D GetFrontTexture() { return ClassTextureFront; }
	public Texture2D GetBackTexture() { return ClassTextureBack; }
	public AttackPerPosition GetAttackPerPosition(int Row) { return AttacksPerPosition[Row]; }
	public int GetBaseAttackDamage() { return BaseAttackDamage; }
}
