using Morpeh;
using UnityEngine;
using Unity.IL2CPP.CompilerServices;
using static Statics;
using System.Collections;
using System.Runtime.CompilerServices;

[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
[CreateAssetMenu(menuName = "ECS/Systems/" + nameof(PlayerSystem))]
public sealed class PlayerSystem : UpdateSystem {

	private const float NETWORK_DURATION = 0.1f;

	public override void OnAwake() { }

	public override void OnUpdate(float deltaTime) {
		ref Filter.ComponentsBag<GameObjectComponent> gameObjectBag = ref Only_Players_Filter.Select<GameObjectComponent>();
		ref Filter.ComponentsBag<TankComponent> tankBag = ref Only_Players_Filter.Select<TankComponent>();
		ref Filter.ComponentsBag<PlayerComponent> playerBag = ref Only_Players_Filter.Select<PlayerComponent>();
		ref Filter.ComponentsBag<TeamComponent> teamBag = ref Only_Players_Filter.Select<TeamComponent>();
		ref Filter.ComponentsBag<HealthComponent> healthBag = ref Only_Players_Filter.Select<HealthComponent>();

		for (int i = 0; i < Only_Players_Filter.Length; i++) {
			ref GameObjectComponent gameObjectComponent = ref gameObjectBag.GetComponent(i);
			if (!gameObjectComponent.Self.activeSelf)
				continue;

			ref TankComponent tankComponent = ref tankBag.GetComponent(i);
			ref PlayerComponent playerComponent = ref playerBag.GetComponent(i);
			ref TeamComponent teamComponent = ref teamBag.GetComponent(i);
			ref HealthComponent healthComponent = ref healthBag.GetComponent(i);

			if (teamComponent.Team != PlayManager.frozenTeam) {
				int keysBuffer = 0;
				if (teamComponent.Team == Teams.TOP) {
					keysBuffer |= Input.GetKey(KeyCode.W) ? 0b1 : 0b0;
					keysBuffer |= Input.GetKey(KeyCode.A) ? 0b1 << 1 : 0b0;
					keysBuffer |= Input.GetKey(KeyCode.S) ? 0b1 << 2 : 0b0;
					keysBuffer |= Input.GetKey(KeyCode.D) ? 0b1 << 3 : 0b0;
					keysBuffer |= Input.GetKeyDown(KeyCode.Space) ? 0b1 << 4 : 0b0;
				} else {
					keysBuffer |= Input.GetKey(KeyCode.UpArrow) ? 0b1 : 0b0;
					keysBuffer |= Input.GetKey(KeyCode.LeftArrow) ? 0b1 << 1 : 0b0;
					keysBuffer |= Input.GetKey(KeyCode.DownArrow) ? 0b1 << 2 : 0b0;
					keysBuffer |= Input.GetKey(KeyCode.RightArrow) ? 0b1 << 3 : 0b0;
					keysBuffer |= Input.GetKeyDown(KeyCode.RightControl) ? 0b1 << 4 : 0b0;
				}
				TryFire(ref tankComponent, keysBuffer);
				TryRotate(ref tankComponent, keysBuffer);
				TryMove(ref tankComponent, keysBuffer);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void TryFire(ref TankComponent tankComponent, int keysBuffer) {
		if ((keysBuffer & 0b10000) != 0) {
			Fire(ref tankComponent);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] private void Fire(ref TankComponent tankComponent) => tankComponent.Fired = true;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void TryRotate(ref TankComponent tankComponent, int keysBuffer) {
		if ((keysBuffer & 0b0001) != 0) {
			if (tankComponent.TankDirection != Directions.UP) {
				Rotate(ref tankComponent, Directions.UP);
			}
		} else if ((keysBuffer & 0b0010) != 0) {
			if (tankComponent.TankDirection != Directions.LEFT) {
				Rotate(ref tankComponent, Directions.LEFT);
			}
		} else if ((keysBuffer & 0b0100) != 0) {
			if (tankComponent.TankDirection != Directions.DOWN) {
				Rotate(ref tankComponent, Directions.DOWN);
			}
		} else if ((keysBuffer & 0b1000) != 0) {
			if (tankComponent.TankDirection != Directions.RIGHT) {
				Rotate(ref tankComponent, Directions.RIGHT);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] private void Rotate(ref TankComponent tankComponent, Directions direction) => tankComponent.TankDirection = direction;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void TryMove(ref TankComponent tankComponent, int keysBuffer) {
		if ((keysBuffer & 0b1111) != 0) {
			if (tankComponent.TankState != States.MOVING) {
				Move(ref tankComponent, States.MOVING);
				AudioManager.Instance.RequestInfiniteSound(GameSounds.MOVING);
			}
		} else {
			if (tankComponent.TankState != States.IDLE) {
				Move(ref tankComponent, States.IDLE);
				AudioManager.Instance.RequestInfiniteSound(GameSounds.IDLE);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] private void Move(ref TankComponent tankComponent, States state) => tankComponent.TankState = state;

}