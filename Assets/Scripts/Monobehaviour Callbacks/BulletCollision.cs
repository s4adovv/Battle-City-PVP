using System.Collections;
using System.Collections.Generic;
using Morpeh;
using Morpeh.Globals;
using UnityEngine;
using static Statics;

public class BulletCollision : MonoBehaviour
{

	private IEntity bulletEntity;

	private void Start() {
		bulletEntity = GetComponent<IEntityProvider>().Entity;
	}

	// Check if bullet hit something with Collider
	private void OnCollisionEnter2D(Collision2D collision) {
		TryDecreaseHealth(collision.collider.GetComponent<IEntityProvider>()?.Entity);

		if (bulletEntity == null) {
			;
		}
		ref GameObjectComponent bulletGameObjectComponent = ref bulletEntity.GetComponent<GameObjectComponent>();
		BlowPoolManager.Instance.RequestBlow(true, bulletGameObjectComponent.SelfTransform.position);
		BulletPoolManager.Instance.Remove(bulletGameObjectComponent.SelfEntity);
	}

	// Check if the bullet hit something with Trigger(Leaves)
	private void OnTriggerEnter2D(Collider2D collider) {
		TryDecreaseHealth(collider.GetComponent<IEntityProvider>()?.Entity);
	}

	private void TryDecreaseHealth(IEntity hitEntity) {
		if (hitEntity == null)
			return;

		// Check if the bullet hit something with Health
		if (hitEntity.Has<HealthComponent>()) {
			ref BulletComponent bulletComponent = ref bulletEntity.GetComponent<BulletComponent>();
			ref HealthComponent hitHealthComponent = ref hitEntity.GetComponent<HealthComponent>();
			ref GameObjectComponent hitGameObjectComponent = ref hitEntity.GetComponent<GameObjectComponent>();
			if (!hitHealthComponent.Invulnerable || (bulletComponent.CanDestroySteel && hitGameObjectComponent.Self.tag == Block_Tags[(int)BlockTypes.STEEL])) {
				ref TeamComponent bulletTeamComponent = ref bulletEntity.GetComponent<TeamComponent>();

				// Check if the bullet hit the tank or flag
				if (hitEntity.Has<TeamComponent>()) {
					ref TeamComponent hitTeamComponent = ref hitEntity.GetComponent<TeamComponent>();
					if (bulletTeamComponent.Team != hitTeamComponent.Team) {
						hitHealthComponent.Health--;

						if (hitGameObjectComponent.Self.tag == PLAYER_TAG) { // Check if the bullet hit player
							UIManager.Instance.SetLife(hitTeamComponent.Team, hitHealthComponent.Health);
						}
					}
				} else {
					hitHealthComponent.Health--;
				}

				TryToDestroy(ref hitGameObjectComponent, ref hitHealthComponent);
			} else if (hitHealthComponent.Invulnerable && hitEntity.Has<PlayerComponent>()) {
				AudioManager.Instance.RequestSound(GameSounds.SHIELD_HIT);
			}
		}
	}

	private void TryToDestroy(ref GameObjectComponent hitGameObjectComponent, ref HealthComponent hitHealthComponent) {
		const string BRICK_TAG = "Brick block";
		const string STEEL_TAG = "Steel block";
		const int KILL_TANK_SCORE = 1000;
		const int KILL_FLAG_SCORE = 10000;

		ref TeamComponent bulletTeamComponent = ref bulletEntity.GetComponent<TeamComponent>();
		if (hitHealthComponent.Health <= 0) {
			switch (hitGameObjectComponent.Self.tag) {
				case PLAYER_TAG:
					ref TeamComponent hitTeamComponent = ref hitGameObjectComponent.SelfEntity.GetComponent<TeamComponent>();
					SpawnManager.Instance.RespawnPlayer(hitTeamComponent.Team);
					BlowPoolManager.Instance.RequestBlow(false, hitGameObjectComponent.SelfTransform.position);
					AudioManager.Instance.RequestSound(GameSounds.PLAYER_EXPLOSION);

					UIManager.Instance.AddScoreCoroutine(KILL_TANK_SCORE, bulletTeamComponent.Team);
					break;
				case BOT_TAG:
					BlowPoolManager.Instance.RequestBlow(false, hitGameObjectComponent.SelfTransform.position);
					AudioManager.Instance.RequestSound(GameSounds.BOT_EXPLOSION);

					UIManager.Instance.AddScoreCoroutine(KILL_TANK_SCORE, bulletTeamComponent.Team);
					break;
				case FLAG_TAG:
					hitTeamComponent = ref hitGameObjectComponent.SelfEntity.GetComponent<TeamComponent>();
					PlayManager.playersScore[(int)bulletTeamComponent.Team] += KILL_FLAG_SCORE;
					UIManager.Instance.SetScore(bulletTeamComponent.Team, PlayManager.playersScore[(int)bulletTeamComponent.Team]);

					PlayManager.Instance.GameOver(hitTeamComponent.Team == Teams.TOP ? Teams.BOTTOM : Teams.TOP);
					break;
				case BRICK_TAG:
					AudioManager.Instance.RequestSound(GameSounds.BRICK_HIT);
					break;
				case STEEL_TAG:
					AudioManager.Instance.RequestSound(GameSounds.STEEL_HIT);
					break;
				default:
					break;
			}

			Pools[(int)hitHealthComponent.PoolOwner].Remove(hitGameObjectComponent.SelfEntity);
		}
	}

}
