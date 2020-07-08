using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Morpeh;
using static Statics;

public class PlayerCollision : MonoBehaviour
{

	private IEntity selfEntity;

	private void Start() {
		selfEntity = GetComponent<IEntityProvider>().Entity;
	}

	private void OnTriggerEnter2D(Collider2D collider) {
		if (collider.tag == BONUS_TAG && selfEntity.Has<PlayerComponent>()) {
			ApplyBonus(collider.GetComponent<IEntityProvider>().Entity);
		}
	}

	private void ApplyBonus(IEntity bonusEntity) {
		const int BONUS_SCORE = 500;

		ref GameObjectComponent gameObjectComponent = ref selfEntity.GetComponent<GameObjectComponent>();
		ref TankComponent tankComponent = ref selfEntity.GetComponent<TankComponent>();
		ref TeamComponent teamComponent = ref selfEntity.GetComponent<TeamComponent>();
		ref PlayerComponent playerComponent = ref selfEntity.GetComponent<PlayerComponent>();
		ref HealthComponent healthComponent = ref selfEntity.GetComponent<HealthComponent>();

		ref BonusComponent bonusComponent = ref bonusEntity.GetComponent<BonusComponent>();

		switch (bonusComponent.Bonus) {
			case BonusTypes.KILL_ALL: // If the player takes a "Bomb" bonus, then each of an enemy tank will be destroyed
				Default_World.UpdateFilters();
				ref Filter.ComponentsBag<GameObjectComponent> gameObjectBag = ref All_Tanks_Filter.Select<GameObjectComponent>();
				ref Filter.ComponentsBag<TeamComponent> teamBag = ref All_Tanks_Filter.Select<TeamComponent>();
				for (int i = 0; i < All_Tanks_Filter.Length; i++) {
					ref GameObjectComponent tempGameObjectComponent = ref gameObjectBag.GetComponent(i);
					ref TeamComponent tempTeamComponent = ref teamBag.GetComponent(i);
					if (tempTeamComponent.Team != teamComponent.Team) { // Kills everybody in the other team
						BlowPoolManager.Instance.RequestBlow(false, tempGameObjectComponent.SelfTransform.position);
						Pools[(int)KnownPools.TANK].DestroyObject(tempGameObjectComponent.SelfEntity, true);
					}
				}
				SpawnManager.Instance.RespawnPlayer(teamComponent.Team == Teams.TOP ? Teams.BOTTOM : Teams.TOP);
				AudioManager.Instance.RequestSound(GameSounds.BONUS_TAKEN);
				break;
			case BonusTypes.EXTRA_LIFE: // Bonus "Tank" == health++
				healthComponent.Health++;
				UIManager.Instance.SetLife(teamComponent.Team, healthComponent.Health);
				AudioManager.Instance.RequestSound(GameSounds.LIFE_TAKEN);
				break;
			case BonusTypes.TIME_STOP: // Bonus "Clock" stops the time for each of an enemy tank
				PlayManager.Instance.SetTimeStopCoroutine(teamComponent.Team == Teams.TOP ? Teams.BOTTOM : Teams.TOP);
				AudioManager.Instance.RequestSound(GameSounds.TIME_STOP);
				break;
			case BonusTypes.SHIELD: // Bonus "Shield" == invulnerability for a period of time
				PlayManager.Instance.SetPlayerShieldCoroutine(gameObjectComponent.SelfEntity);
				AudioManager.Instance.RequestSound(GameSounds.BONUS_TAKEN);
				break;
			case BonusTypes.POWER_UP: // "Star" bonus increases the player's tank level
				switch (tankComponent.TankLevel) {
					case TankLevels.FIRST:
						tankComponent.TankLevel++;
						tankComponent.FirePeriod = PlayManager.Instance.increasedTankFirePeriod; // If a player takes his first "Star" bonus, then the player's tank can shoot faster
						break;
					case TankLevels.SECOND:
						tankComponent.TankLevel++;
						tankComponent.CanDoDoubleShot = true; // The Second star gives the player's tank ability to do Double Shot
						break;
					case TankLevels.THIRD:
						tankComponent.TankLevel++;
						tankComponent.CanShootSteel = true; // The Third star allows the tank to shoot through Steel blocks
						break;
					default:
						break;
				}

				SkinManager.Instance.SetPlayerSkin(ref tankComponent, ref playerComponent, playerComponent.SkinColor); // Changes the tank skin due to level increasing
				AudioManager.Instance.RequestSound(GameSounds.BONUS_TAKEN);
				break;
			default:
				break;
		}

		UIManager.Instance.AddScoreCoroutine(BONUS_SCORE, teamComponent.Team);

		Pools[(int)KnownPools.BONUS].DestroyObject(bonusEntity, true);
	}

}
