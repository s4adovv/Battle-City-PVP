using Morpeh;
using UnityEngine;
using Unity.IL2CPP.CompilerServices;

[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
[System.Serializable]
public struct TankComponent : IComponent {

	public Transform BulletStartPoint;
	public float Velocity;
	public float FirePeriod;
	public bool Fired;
	public bool CanShootSteel;
	public bool CanDoDoubleShot;
	public bool DoubleShot;
	public float LastTimeShot;
	public Statics.TankLevels TankLevel;
	public Statics.Directions TankDirection;
	public Statics.Directions OldDirection;
	public Statics.States TankState;
	public Statics.TankTypes TankType;
	public SpriteRenderer[] TankParts;

}