using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Morpeh;
using static Statics;

public class PlayerCollision : MonoBehaviour
{

	private IEntity playerEntity;

	private void Start() {
		playerEntity = GetComponent<IEntityProvider>().Entity;
	}

	private void OnTriggerEnter2D(Collider2D collider) {
		if (collider.tag == BONUS_TAG && playerEntity.Has<PlayerComponent>()) {
			ApplyBonus(collider.GetComponent<IEntityProvider>().Entity);
		}
	}

	private void ApplyBonus(IEntity bonusEntity) {
		const int BONUS_SCORE = 500;
		const int KILL_ALL_SCORE = 5000;

		ref GameObjectComponent playerGameObjectComponent = ref playerEntity.GetComponent<GameObjectComponent>();
		ref TankComponent playerTankComponent = ref playerEntity.GetComponent<TankComponent>();
		ref TeamComponent playerTeamComponent = ref playerEntity.GetComponent<TeamComponent>();
		ref PlayerComponent playerComponent = ref playerEntity.GetComponent<PlayerComponent>();
		ref HealthComponent playerHealthComponent = ref playerEntity.GetComponent<HealthComponent>();

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

				SkinManager.Instance.SetPlayerSkin(ref playerTankComponent, ref playerComponent, playerComponent.SkinColor); // Changes the tank skin due to level increasing
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
