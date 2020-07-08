using System.Collections;
using System.Collections.Generic;
using Morpeh;
using Morpeh.Globals;
using UnityEngine;
using static Statics;

public class BulletCollision : MonoBehaviour
{

	private IEntity selfEntity;

	private void Start() {
		selfEntity = GetComponent<IEntityProvider>().Entity;
	}

	// Check if bullet hit something with Collider
	private void OnCollisionEnter2D(Collision2D collision) {
		TryDecreaseHealth(collision.collider.GetComponent<IEntityProvider>()?.Entity);

		ref GameObjectComponent gameObjectComponent = ref selfEntity.GetComponent<GameObjectComponent>();
		BlowPoolManager.Instance.RequestBlow(true, gameObjectComponent.SelfTransform.position);
		BulletPoolManager.Instance.DestroyObject(gameObjectComponent.SelfEntity, true);
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
			ref BulletComponent bulletComponent = ref selfEntity.GetComponent<BulletComponent>();
			ref HealthComponent healthComponent = ref hitEntity.GetComponent<HealthComponent>();
			ref GameObjectComponent gameObjectComponent = ref hitEntity.GetComponent<GameObjectComponent>();
			if (!healthComponent.Invulnerable || (bulletComponent.CanDestroySteel && gameObjectComponent.Self.tag == Block_Tags[(int)BlockTypes.STEEL])) {
				ref TeamComponent selfTeamComponent = ref selfEntity.GetComponent<TeamComponent>();

				// Check if the bullet hit the tank or flag
				if (hitEntity.Has<TeamComponent>()) {
					ref TeamComponent hitTeamComponent = ref hitEntity.GetComponent<TeamComponent>();
					if (selfTeamComponent.Team != hitTeamComponent.Team) {
						healthComponent.Health--;

						if (gameObjectComponent.Self.tag == PLAYER_TAG) { // Check if the bullet hit player
							UIManager.Instance.SetLife(hitTeamComponent.Team, healthComponent.Health);
						}
					}
				} else {
					healthComponent.Health--;
				}

				TryToDestroy(ref gameObjectComponent, ref healthComponent);
			} else if (healthComponent.Invulnerable && hitEntity.Has<PlayerComponent>()) {
				AudioManager.Instance.RequestSound(GameSounds.SHIELD_HIT);
			}
		}
	}

	private void TryToDestroy(ref GameObjectComponent gameObjectComponent, ref HealthComponent healthComponent) {
		const string BRICK_TAG = "Brick block";
		const string STEEL_TAG = "Steel block";
		const int KILL_TANK_SCORE = 1000;
		const int KILL_FLAG_SCORE = 10000;

		ref TeamComponent selfTeamComponent = ref selfEntity.GetComponent<TeamComponent>();
		if (healthComponent.Health <= 0) {
			switch (gameObjectComponent.Self.tag) {
				case PLAYER_TAG:
					ref TeamComponent hitTeamComponent = ref gameObjectComponent.SelfEntity.GetComponent<TeamComponent>();
					SpawnManager.Instance.RespawnPlayer(hitTeamComponent.Team);
					BlowPoolManager.Instance.RequestBlow(false, gameObjectComponent.SelfTransform.position);
					AudioManager.Instance.RequestSound(GameSounds.PLAYER_EXPLOSION);

					UIManager.Instance.AddScoreCoroutine(KILL_TANK_SCORE, selfTeamComponent.Team);
					break;
				case BOT_TAG:
					BlowPoolManager.Instance.RequestBlow(false, gameObjectComponent.SelfTransform.position);
					AudioManager.Instance.RequestSound(GameSounds.BOT_EXPLOSION);

					UIManager.Instance.AddScoreCoroutine(KILL_TANK_SCORE, selfTeamComponent.Team);
					break;
				case FLAG_TAG:
					hitTeamComponent = ref gameObjectComponent.SelfEntity.GetComponent<TeamComponent>();
					PlayManager.playersScore[(int)selfTeamComponent.Team] += KILL_FLAG_SCORE;
					UIManager.Instance.SetScore(selfTeamComponent.Team, PlayManager.playersScore[(int)selfTeamComponent.Team]);

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

			Pools[(int)healthComponent.PoolOwner].DestroyObject(gameObjectComponent.SelfEntity, true);
		}
	}

}
