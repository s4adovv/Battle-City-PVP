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
		ref Filter.ComponentsBag<GameObjectComponent> playerGameObjectBag = ref Only_Players_Filter.Select<GameObjectComponent>();
		ref Filter.ComponentsBag<TankComponent> playerTankBag = ref Only_Players_Filter.Select<TankComponent>();
		ref Filter.ComponentsBag<PlayerComponent> playerBag = ref Only_Players_Filter.Select<PlayerComponent>();
		ref Filter.ComponentsBag<TeamComponent> playerTeamBag = ref Only_Players_Filter.Select<TeamComponent>();
		ref Filter.ComponentsBag<HealthComponent> playerHealthBag = ref Only_Players_Filter.Select<HealthComponent>();
		ref Filter.ComponentsBag<AnimatorComponent> playerAnimatorBag = ref Only_Players_Filter.Select<AnimatorComponent>();
		ref Filter.ComponentsBag<LastTimeComponent> playerLastTimeBag = ref Only_Players_Filter.Select<LastTimeComponent>();

		for (int i = 0; i < Only_Players_Filter.Length; i++) {
			ref GameObjectComponent playerGameObjectComponent = ref playerGameObjectBag.GetComponent(i);
			if (!playerGameObjectComponent.Self.activeSelf)
				continue;

			ref TankComponent playerTankComponent = ref playerTankBag.GetComponent(i);
			ref PlayerComponent playerComponent = ref playerBag.GetComponent(i);
			ref TeamComponent playerTeamComponent = ref playerTeamBag.GetComponent(i);
			ref HealthComponent playerHealthComponent = ref playerHealthBag.GetComponent(i);
			ref AnimatorComponent playerTankWheelsAnimatorComponent = ref playerAnimatorBag.GetComponent(i);
			ref LastTimeComponent playerLastTimeComponent = ref playerLastTimeBag.GetComponent(i);

			if (playerTeamComponent.Team != PlayManager.frozenTeam) {
				int keysBuffer = 0;
				if (playerTeamComponent.Team == Teams.TOP) {
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
				TryFire(ref playerTankComponent, keysBuffer);
				TryRotate(ref playerTankComponent, keysBuffer);
				TryMove(ref playerTankComponent, ref playerTankWheelsAnimatorComponent, keysBuffer);
				Physics(
					ref playerGameObjectComponent,
					ref playerTankComponent,
					ref playerTeamComponent,
					ref playerComponent,
					ref playerHealthComponent,
					ref playerLastTimeComponent);
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

	private void TryMove(ref TankComponent tankComponent, ref AnimatorComponent tankAnimatorComponent, int keysBuffer) {
		if ((keysBuffer & 0b1111) != 0) {
			if (tankComponent.TankState != States.MOVING) {
				Move(ref tankComponent, States.MOVING);
				tankAnimatorComponent.SelfAnimator.SetBool(WHEELS_ANIMATOR_BOOL_NAME, true);
				AudioManager.Instance.RequestInfiniteSound(GameSounds.MOVING);
			}
		} else {
			if (tankComponent.TankState != States.IDLE) {
				Move(ref tankComponent, States.IDLE);
				tankAnimatorComponent.SelfAnimator.SetBool(WHEELS_ANIMATOR_BOOL_NAME, false);
				AudioManager.Instance.RequestInfiniteSound(GameSounds.IDLE);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] private void Move(ref TankComponent tankComponent, States state) => tankComponent.TankState = state;

	private void Physics(
		ref GameObjectComponent playerGameObjectComponent,
		ref TankComponent playerTankComponent,
		ref TeamComponent playerTeamComponent,
		ref PlayerComponent playerComponent,
		ref HealthComponent playerHealthComponent,
		ref LastTimeComponent playerLastTimeComponent) {

		float tempTime = Time.time;
		if ((tempTime - playerLastTimeComponent.LastTimeUsed) >= PHYSICS_TIME) {
			playerLastTimeComponent.LastTimeUsed = tempTime;
			Collider2D[] bonusColliders = Physics2D.OverlapBoxAll(playerGameObjectComponent.SelfTransform.position, playerGameObjectComponent.SelfTransform.localScale, 0);
			if (bonusColliders.Length != 0) {
				for (int i = 0; i < bonusColliders.Length; i++) {
					if (bonusColliders[i].tag == BONUS_TAG) {
						ApplyBonus(
						bonusColliders[i].GetComponent<IEntityProvider>()?.Entity,
						ref playerGameObjectComponent,
						ref playerTankComponent,
						ref playerTeamComponent,
						ref playerComponent,
						ref playerHealthComponent);
					}
				}
			}
		}
	}

	private void ApplyBonus(
		IEntity bonusEntity,
		ref GameObjectComponent playerGameObjectComponent,
		ref TankComponent playerTankComponent,
		ref TeamComponent playerTeamComponent,
		ref PlayerComponent playerComponent,
		ref HealthComponent playerHealthComponent) {

		const int BONUS_SCORE = 500;
		const int KILL_ALL_SCORE = 5000;

		ref BonusComponent bonusComponent = ref bonusEntity.GetComponent<BonusComponent>();

		switch (bonusComponent.Bonus) {
			case BonusTypes.KILL_ALL: // If the player takes a "Bomb" bonus, then each of an enemy tank will be destroyed
				Default_World.UpdateFilters();
				ref Filter.ComponentsBag<GameObjectComponent> tankGameObjectBag = ref All_Tanks_Filter.Select<GameObjectComponent>();
				ref Filter.ComponentsBag<TeamComponent> tankTeamBag = ref All_Tanks_Filter.Select<TeamComponent>();
				for (int i = 0; i < All_Tanks_Filter.Length; i++) {
					ref GameObjectComponent tankGameObjectComponent = ref tankGameObjectBag.GetComponent(i);
					ref TeamComponent tankTeamComponent = ref tankTeamBag.GetComponent(i);
					if (tankTeamComponent.Team != playerTeamComponent.Team) { // Kills everybody in the other team
						BlowPoolManager.Instance.RequestBlow(false, tankGameObjectComponent.SelfTransform.position);
						Pools[(int)KnownPools.TANK].Remove(tankGameObjectComponent.SelfEntity);
					}
				}
				SpawnManager.Instance.RespawnPlayer(playerTeamComponent.Team == Teams.TOP ? Teams.BOTTOM : Teams.TOP);
				AudioManager.Instance.RequestSound(GameSounds.BONUS_TAKEN);
				UIManager.Instance.AddScoreCoroutine(KILL_ALL_SCORE, playerTeamComponent.Team);
				break;
			case BonusTypes.EXTRA_LIFE: // Bonus "Tank" == health++
				playerHealthComponent.Health++;
				AudioManager.Instance.RequestSound(GameSounds.LIFE_TAKEN);
				UIManager.Instance.SetLife(playerTeamComponent.Team, playerHealthComponent.Health);
				break;
			case BonusTypes.TIME_STOP: // Bonus "Clock" stops the time for each of an enemy tank
				PlayManager.Instance.SetTimeStopCoroutine(playerTeamComponent.Team == Teams.TOP ? Teams.BOTTOM : Teams.TOP);
				AudioManager.Instance.RequestSound(GameSounds.TIME_STOP);
				break;
			case BonusTypes.SHIELD: // Bonus "Shield" == invulnerability for a period of time
				PlayManager.Instance.SetPlayerShieldCoroutine(playerGameObjectComponent.SelfEntity);
				AudioManager.Instance.RequestSound(GameSounds.BONUS_TAKEN);
				break;
			case BonusTypes.POWER_UP: // "Star" bonus increases the player's tank level
				switch (playerTankComponent.TankLevel) {
					case TankLevels.FIRST:
						playerTankComponent.TankLevel++;
						playerTankComponent.FirePeriod = PlayManager.Instance.increasedTankFirePeriod; // If a player takes his first "Star" bonus, then the player's tank can shoot faster
						break;
					case TankLevels.SECOND:
						playerTankComponent.TankLevel++;
						playerTankComponent.CanDoDoubleShot = true; // The Second star gives the player's tank ability to do Double Shot
						break;
					case TankLevels.THIRD:
						playerTankComponent.TankLevel++;
						playerTankComponent.CanShootSteel = true; // The Third star allows the tank to shoot through Steel blocks
						break;
					default:
						break;
				}

				SkinManager.Instance.SetPlayerSkin(ref playerTankComponent, ref playerComponent, SkinManager.playersSkin[(int)playerTeamComponent.Team]); // Changes the tank skin due to level increasing
				AudioManager.Instance.RequestSound(GameSounds.BONUS_TAKEN);
				break;
			default:
				break;
		}

		if (bonusComponent.Bonus != BonusTypes.KILL_ALL) {
			UIManager.Instance.AddScoreCoroutine(BONUS_SCORE, playerTeamComponent.Team);
		}

		Pools[(int)KnownPools.BONUS].Remove(bonusEntity);
	}

}