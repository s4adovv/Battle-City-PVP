using Morpeh;
using UnityEngine;
using Unity.IL2CPP.CompilerServices;
using static Statics;
using System.Runtime.CompilerServices;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Rendering;

[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
[CreateAssetMenu(menuName = "ECS/Systems/" + nameof(BulletSystem))]
public sealed class BulletSystem : UpdateSystem {

	public override void OnAwake() { }

	public override void OnUpdate(float deltaTime) {
		ref Filter.ComponentsBag<GameObjectComponent> bulletGameObjectBag = ref All_Bullets_Filter.Select<GameObjectComponent>();
		ref Filter.ComponentsBag<BulletComponent> bulletBag = ref All_Bullets_Filter.Select<BulletComponent>();
		ref Filter.ComponentsBag<TeamComponent> bulletTeamBag = ref All_Bullets_Filter.Select<TeamComponent>();
		ref Filter.ComponentsBag<LastTimeComponent> bulletLastTimeBag = ref All_Bullets_Filter.Select<LastTimeComponent>();

		for (int i = 0; i < All_Bullets_Filter.Length; i++) {
			ref GameObjectComponent bulletGameObjectComponent = ref bulletGameObjectBag.GetComponent(i);
			if (!bulletGameObjectComponent.Self.activeSelf)
				continue;

			ref BulletComponent bulletComponent = ref bulletBag.GetComponent(i);
			ref TeamComponent bulletTeamComponent = ref bulletTeamBag.GetComponent(i);
			ref LastTimeComponent bulletLastTimeComponent = ref bulletLastTimeBag.GetComponent(i);

			Move(ref bulletGameObjectComponent, ref bulletComponent, deltaTime);
			Physics(ref bulletGameObjectComponent, ref bulletComponent, ref bulletTeamComponent, ref bulletLastTimeComponent);
		}
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)] private void Move(ref GameObjectComponent bulletGameObjectComponent, ref BulletComponent bulletComponent, float deltaTime) => bulletGameObjectComponent.SelfTransform.transform.Translate(bulletComponent.Velocity * Vector_Up * deltaTime);

	private void Physics(
		ref GameObjectComponent bulletGameObjectComponent,
		ref BulletComponent bulletComponent,
		ref TeamComponent bulletTeamComponent,
		ref LastTimeComponent bulletLastTimeComponent) {

		float tempTime = Time.time;
		if ((tempTime - bulletLastTimeComponent.LastTimeUsed) >= PHYSICS_TIME) {
			bulletLastTimeComponent.LastTimeUsed = tempTime;
			Collider2D[] hitColliders = Physics2D.OverlapBoxAll(bulletGameObjectComponent.SelfTransform.position, bulletGameObjectComponent.SelfTransform.localScale, 0);
			if (hitColliders.Length != 0) {
				bool hitSolidCollider = false;
				for (int i = 0; i < hitColliders.Length; i++) {
					TryDecreaseHealth(hitColliders[i].GetComponent<IEntityProvider>()?.Entity, ref bulletComponent, ref bulletTeamComponent);
					if (!hitColliders[i].isTrigger) {
						BlowPoolManager.Instance.RequestBlow(true, bulletGameObjectComponent.SelfTransform.position);
						hitSolidCollider = true;
					}
				}
				if (hitSolidCollider) {
					BulletPoolManager.Instance.Remove(bulletGameObjectComponent.SelfEntity);
				}
			}
		}
	}

	private void TryDecreaseHealth(IEntity hitEntity, ref BulletComponent bulletComponent, ref TeamComponent bulletTeamComponent) {
		if (hitEntity == null || hitEntity.ID == -1)
			return;

		// Check if a bullet hit something with Health
		if (hitEntity.Has<HealthComponent>()) {
			ref HealthComponent hitHealthComponent = ref hitEntity.GetComponent<HealthComponent>();
			ref GameObjectComponent hitGameObjectComponent = ref hitEntity.GetComponent<GameObjectComponent>();
			if (!hitHealthComponent.Invulnerable || (bulletComponent.CanDestroySteel && hitGameObjectComponent.Self.tag == Block_Tags[(int)BlockTypes.STEEL])) {
				// Check if a bullet hit a tank or flag
				if (hitEntity.Has<TeamComponent>()) {
					ref TeamComponent hitTeamComponent = ref hitEntity.GetComponent<TeamComponent>();
					if (bulletTeamComponent.Team != hitTeamComponent.Team) {
						hitHealthComponent.Health--;

						if (hitGameObjectComponent.Self.tag == PLAYER_TAG) { // Check if a bullet hit a player
							UIManager.Instance.SetLife(hitTeamComponent.Team, hitHealthComponent.Health);
						}
					}
				} else {
					hitHealthComponent.Health--;
				}

				TryToDestroy(ref hitGameObjectComponent, ref hitHealthComponent, ref bulletTeamComponent);
			} else if (hitHealthComponent.Invulnerable && hitEntity.Has<PlayerComponent>()) {
				AudioManager.Instance.RequestSound(GameSounds.SHIELD_HIT);
			}
		}
	}

	private void TryToDestroy(ref GameObjectComponent hitGameObjectComponent, ref HealthComponent hitHealthComponent, ref TeamComponent bulletTeamComponent) {
		const string BRICK_TAG = "Brick block";
		const string STEEL_TAG = "Steel block";
		const int KILL_TANK_SCORE = 1000;
		const int KILL_FLAG_SCORE = 10000;

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