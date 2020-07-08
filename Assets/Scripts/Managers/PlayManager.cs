using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Morpeh;
using UnityEngine;
using static Statics;

public class PlayManager : MonoBehaviour
{

	public static int[] playersScore = new int[(int)Teams.COUNT];

	public static PlayManager Instance;
	public static Teams frozenTeam;

	[Range(0, float.MaxValue)] public float standardTankFirePeriod, increasedTankFirePeriod;
	[Range(0, float.MaxValue)] public float standardTankVelocity, standardBulletVelocity;
	[SerializeField][Range(0, float.MaxValue)] private float playerShieldDuration, playerInvulnerabilityDuration, timeStopDuration;

	private void Awake() {
		if (Instance == null) {
			Instance = this;
		}
	}

	public void InitializeGame() {
		frozenTeam = Teams.COUNT;
		MapManager.Instance.CreateRandomMap();
		SkinManager.botsSkin[(int)Teams.TOP] = (BotColors)Random.Range(0, (int)BotColors.GRAY_GREEN);
		SkinManager.botsSkin[(int)Teams.BOTTOM] = (BotColors)Random.Range((int)BotColors.GRAY_GREEN, (int)BotColors.COUNT);
		SpawnManager.Instance.RespawnPlayer(Teams.TOP);
		SpawnManager.Instance.RespawnPlayer(Teams.BOTTOM);
		SpawnManager.Instance.StartBonusRespawning();
		SpawnManager.Instance.StartBotRespawning();
		UIManager.Instance.SetUI(GameStates.GAME);
		AudioManager.Instance.RequestSound(GameSounds.GAME_START);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void GameOver(Teams wonTeam) => UIManager.Instance.GameOverCoroutine(wonTeam);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetPlayerInvulnerableCoroutine(IEntity playerEntity) => StartCoroutine("SetPlayerInvulnerableRoutine", playerEntity);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetPlayerInvulnerableHelper(ref HealthComponent playerHealthComponent, ref PlayerComponent playerComponent, bool invulnerable) {
		playerHealthComponent.Invulnerable = invulnerable;
		playerComponent.Invulnerability.SetActive(invulnerable);
	}
	private IEnumerator SetPlayerInvulnerableRoutine(IEntity playerEntity) {
		SetPlayerInvulnerableHelper(ref playerEntity.GetComponent<HealthComponent>(), ref playerEntity.GetComponent<PlayerComponent>(), true);
		yield return new WaitForSeconds(playerInvulnerabilityDuration);
		SetPlayerInvulnerableHelper(ref playerEntity.GetComponent<HealthComponent>(), ref playerEntity.GetComponent<PlayerComponent>(), false);
		yield break;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetPlayerShieldCoroutine(IEntity playerEntity) => StartCoroutine("SetPlayerShieldRoutine", playerEntity);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetPlayerShieldHelper(ref HealthComponent playerHealthComponent, ref PlayerComponent playerComponent, bool invulnerable) {
		playerHealthComponent.Invulnerable = invulnerable;
		playerComponent.Shield.SetActive(invulnerable);
	}
	private IEnumerator SetPlayerShieldRoutine(IEntity playerEntity) {
		SetPlayerShieldHelper(ref playerEntity.GetComponent<HealthComponent>(), ref playerEntity.GetComponent<PlayerComponent>(), true);
		yield return new WaitForSeconds(playerShieldDuration);
		SetPlayerShieldHelper(ref playerEntity.GetComponent<HealthComponent>(), ref playerEntity.GetComponent<PlayerComponent>(), false);
		yield break;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetTimeStopCoroutine(Teams team) => StartCoroutine("TimeStopRoutine", team);
	private IEnumerator TimeStopRoutine(Teams team) {
		frozenTeam = team;
		yield return new WaitForSeconds(timeStopDuration);
		frozenTeam = Teams.COUNT;
		yield break;
	}

}
