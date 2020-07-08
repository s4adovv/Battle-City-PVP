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

	private const string WHEELS_ANIMATOR_BOOL_NAME = "Moving";

	private static readonly (Vector3 noFlipX, Vector3 flipX) Tank_Flip_Scales = ( Vector3.one, new Vector3(-1, 1, 1) );

	public override void OnAwake() { }

	public override void OnUpdate(float deltaTime) {
		ref Filter.ComponentsBag<GameObjectComponent> gameObjectBag = ref All_Tanks_Filter.Select<GameObjectComponent>();
		ref Filter.ComponentsBag<TankComponent> tankBag = ref All_Tanks_Filter.Select<TankComponent>();
		ref Filter.ComponentsBag<TeamComponent> teamBag = ref All_Tanks_Filter.Select<TeamComponent>();
		ref Filter.ComponentsBag<AnimatorComponent> animatorBag = ref All_Tanks_Filter.Select<AnimatorComponent>();

		for (int i = 0; i < All_Tanks_Filter.Length; i++) {
			ref GameObjectComponent gameObjectComponent = ref gameObjectBag.GetComponent(i);
			if (!gameObjectComponent.Self.activeSelf)
				continue;

			ref TankComponent tankComponent = ref tankBag.GetComponent(i);
			ref TeamComponent teamComponent = ref teamBag.GetComponent(i);
			ref AnimatorComponent wheelsAnimatorComponent = ref animatorBag.GetComponent(i);

			if (teamComponent.Team != PlayManager.frozenTeam) {
				switch (tankComponent.TankState) {
					case States.IDLE:
						wheelsAnimatorComponent.SelfAnimator.SetBool(WHEELS_ANIMATOR_BOOL_NAME, false);
						Fire(ref tankComponent, ref teamComponent);
						Rotate(ref gameObjectComponent, ref tankComponent);
						break;
					case States.MOVING:
						wheelsAnimatorComponent.SelfAnimator.SetBool(WHEELS_ANIMATOR_BOOL_NAME, true);
						Fire(ref tankComponent, ref teamComponent);
						Rotate(ref gameObjectComponent, ref tankComponent);
						Move(ref gameObjectComponent, ref tankComponent, deltaTime);
						break;
					default:
						break;
				}
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Move(ref GameObjectComponent gameObjectComponent, ref TankComponent tankComponent, float deltaTime) => gameObjectComponent.SelfTransform.Translate(tankComponent.Velocity * Vector_Up * deltaTime);

	private void Rotate(ref GameObjectComponent gameObjectComponent, ref TankComponent tankComponent) {
		if (tankComponent.TankDirection != tankComponent.OldDirection) {
			tankComponent.OldDirection = tankComponent.TankDirection;
			gameObjectComponent.SelfTransform.localRotation = Rotations[(int)tankComponent.TankDirection];
			switch (tankComponent.TankDirection) {
				case Directions.UP:
				case Directions.RIGHT:
					gameObjectComponent.SelfTransform.localScale = Tank_Flip_Scales.noFlipX;
					break;
				case Directions.DOWN:
				case Directions.LEFT:
					gameObjectComponent.SelfTransform.localScale = Tank_Flip_Scales.flipX;
					break;
				default:
					break;
			}
		}
	}

	private void Fire(ref TankComponent tankComponent, ref TeamComponent teamComponent) {
		if (tankComponent.Fired) {
			if ((Time.time - tankComponent.LastTimeShot) >= tankComponent.FirePeriod) {
				ref GameObjectComponent tempGameObjectComponent = ref BulletPoolManager.Instance.EnsureObject(tankComponent.BulletStartPoint.position, Rotations[(int)tankComponent.TankDirection], BulletPoolManager.Instance.transform);
				ref BulletComponent bulletComponent = ref NotHasGet<BulletComponent>(tempGameObjectComponent.SelfEntity);
				ref TeamComponent tempTeamComponent = ref NotHasGet<TeamComponent>(tempGameObjectComponent.SelfEntity);

				bulletComponent.Velocity = PlayManager.Instance.standardBulletVelocity;
				bulletComponent.CanDestroySteel = tankComponent.CanShootSteel;
				tempTeamComponent.Team = teamComponent.Team;
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