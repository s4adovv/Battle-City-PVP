using Morpeh;
using UnityEngine;
using Unity.IL2CPP.CompilerServices;
using static Statics;
using System.Runtime.CompilerServices;

[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
[CreateAssetMenu(menuName = "ECS/Systems/" + nameof(TankSystem))]
public sealed class TankSystem : UpdateSystem {

	private static readonly (Vector3 noFlipX, Vector3 flipX) Tank_Flip_Scales = ( Vector3.one, new Vector3(-1, 1, 1) );

	public override void OnAwake() { }

	public override void OnUpdate(float deltaTime) {
		ref Filter.ComponentsBag<GameObjectComponent> tankGameObjectBag = ref All_Tanks_Filter.Select<GameObjectComponent>();
		ref Filter.ComponentsBag<TankComponent> tankBag = ref All_Tanks_Filter.Select<TankComponent>();
		ref Filter.ComponentsBag<TeamComponent> tankTeamBag = ref All_Tanks_Filter.Select<TeamComponent>();

		for (int i = 0; i < All_Tanks_Filter.Length; i++) {
			ref GameObjectComponent tankGameObjectComponent = ref tankGameObjectBag.GetComponent(i);
			if (!tankGameObjectComponent.Self.activeSelf)
				continue;

			ref TankComponent tankComponent = ref tankBag.GetComponent(i);
			ref TeamComponent tankTeamComponent = ref tankTeamBag.GetComponent(i);

			if (tankTeamComponent.Team != PlayManager.frozenTeam) {
				switch (tankComponent.TankState) {
					case States.IDLE:
						Fire(ref tankComponent, ref tankTeamComponent);
						Rotate(ref tankGameObjectComponent, ref tankComponent);
						break;
					case States.MOVING:
						Fire(ref tankComponent, ref tankTeamComponent);
						Rotate(ref tankGameObjectComponent, ref tankComponent);
						Move(ref tankGameObjectComponent, ref tankComponent, deltaTime);
						break;
					default:
						break;
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Move(ref GameObjectComponent tankGameObjectComponent, ref TankComponent tankComponent, float deltaTime) => tankGameObjectComponent.SelfTransform.Translate(tankComponent.Velocity * Vector_Up * deltaTime);

	private void Rotate(ref GameObjectComponent tankGameObjectComponent, ref TankComponent tankComponent) {
		if (tankComponent.TankDirection != tankComponent.OldDirection) {
			tankComponent.OldDirection = tankComponent.TankDirection;
			tankGameObjectComponent.SelfTransform.localRotation = Rotations[(int)tankComponent.TankDirection];
			switch (tankComponent.TankDirection) {
				case Directions.UP:
				case Directions.RIGHT:
					tankGameObjectComponent.SelfTransform.localScale = Tank_Flip_Scales.noFlipX;
					break;
				case Directions.DOWN:
				case Directions.LEFT:
					tankGameObjectComponent.SelfTransform.localScale = Tank_Flip_Scales.flipX;
					break;
				default:
					break;
			}
		}
	}

	private void Fire(ref TankComponent tankComponent, ref TeamComponent tankTeamComponent) {
		if (tankComponent.Fired) {
			if ((Time.time - tankComponent.LastTimeShot) >= tankComponent.FirePeriod) {
				IEntity bulletEntity = BulletPoolManager.Instance.Get(tankComponent.BulletStartPoint.position, Rotations[(int)tankComponent.TankDirection], BulletPoolManager.Instance.transform);
				ref GameObjectComponent bulletGameObjectComponent = ref bulletEntity.GetComponent<GameObjectComponent>();
				ref BulletComponent bulletComponent = ref NotHasGet<BulletComponent>(bulletEntity);
				ref TeamComponent bulletTeamComponent = ref NotHasGet<TeamComponent>(bulletEntity);

				bulletComponent.Velocity = PlayManager.Instance.standardBulletVelocity;
				bulletComponent.CanDestroySteel = tankComponent.CanShootSteel;
				bulletTeamComponent.Team = tankTeamComponent.Team;
				tankComponent.LastTimeShot = Time.time;

				if (tankComponent.CanDoDoubleShot) {
					if (!tankComponent.DoubleShot) {
						tankComponent.LastTimeShot -= tankComponent.FirePeriod;
						tankComponent.DoubleShot = true;
					} else {
						tankComponent.DoubleShot = false;
					}
				}

				AudioManager.Instance.RequestSound(GameSounds.SHOOT);
			}

			tankComponent.Fired = false;
		}
	}

}