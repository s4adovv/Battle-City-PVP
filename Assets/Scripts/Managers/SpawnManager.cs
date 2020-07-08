using System.Collections;
using System.Runtime.CompilerServices;
using Morpeh;
using UnityEngine;
using static Statics;

public class SpawnManager : MonoBehaviour
{

	private const int TANK_WHEELS_CHILD_ID = 5;
	private const int BULLET_SPAWN_POINT_CHILD_ID = 6;
	private const int PLAYER_SHIELD_CHILD_ID = 7;
	private const int PLAYER_INVULNERABILITY_CHILD_ID = 8;

	private const float BONUS_BLINK_TIME = 0.05f;
	private const float BONUS_HIDING_BELOW_LIMIT = 0.1f;

	public static SpawnManager Instance;

	private static Vector3 BufferVector = Vector3.zero;

	private static WaitForSeconds bonusBlinkWFS;
#if !UNITY_EDITOR
	private static WaitForSeconds standardSpawningWFS;
	private static WaitForSeconds botSpawningWFS;
#endif

	[SerializeField][Range(0, int.MaxValue / 2)] private int standardSpawningTime;
	[SerializeField][Range(0, int.MaxValue / 2)] private int standardBotLives, standardPlayerLives;
	[SerializeField][Range(0, float.MaxValue)] private float fromSpawnBonusDuration, toSpawnBonusDuration, bonusHidingDuration;
	[SerializeField][Range(0.1f, 1f)] private float bonusHidingScale; // Speed of hiding bonuses(if set to 1 then any of the bonuses will never be hidden)
	[SerializeField][Range(0, float.MaxValue)] private float botSpawnDuration;
	[SerializeField] private GameObject spawnPointPrefab;


	private Vector3[] botSpawnPoints = new Vector3[(int)BotSides.COUNT];
	private Vector3[] playerSpawnPoints = new Vector3[(int)Teams.COUNT];
	private GameObject[] botSpawnPointObjects = new GameObject[(int)BotSides.COUNT];
	private GameObject[] playerSpawnPointObjects = new GameObject[(int)Teams.COUNT];
	private Transform spawnTransform;
	private Transform tankPoolTransform;

	// JUST IN CASE if you reading this... I found bug in your Morpeh ECS system: if you set GameObject.SetActive(false) then the entity sets its components information to default, or if you set ComponentProvider.enabled = false then this component will be set to default.


	private void Awake() {
		if (Instance == null) {
			Instance = this;
		}

		spawnTransform = transform;
		tankPoolTransform = TankPoolManager.Instance.transform;

		bonusBlinkWFS = new WaitForSeconds(BONUS_BLINK_TIME);
#if !UNITY_EDITOR
		standardSpawningWFS = new WaitForSeconds(standardSpawningTime);
		botSpawningWFS = new WaitForSeconds(botSpawnDuration);
#endif
	}

	private void Start() {
		for (int i = 0; i < botSpawnPointObjects.Length; i++) {
			botSpawnPointObjects[i] = Instantiate(spawnPointPrefab, spawnTransform);
			botSpawnPointObjects[i].SetActive(false);
		}
		for (int i = 0; i < playerSpawnPointObjects.Length; i++) {
			playerSpawnPointObjects[i] = Instantiate(spawnPointPrefab, spawnTransform);
			playerSpawnPointObjects[i].SetActive(false);
		}
	}

	public void ClearAll() {
		StopAllCoroutines();
		for (int i = 0; i < playerSpawnPointObjects.Length; i++) {
			playerSpawnPointObjects[i].SetActive(false);
		}
		for (int i = 0; i < botSpawnPointObjects.Length; i++) {
			botSpawnPointObjects[i].SetActive(false);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void StartBonusRespawning() => StartCoroutine("BonusSpawnerRoutine");
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void StartBotRespawning() => StartCoroutine("RespawnBotRoutine");

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RespawnPlayer(Teams team) {
		ref GameObjectComponent gameObjectComponent = ref TankPoolManager.Instance.EnsureObject(playerSpawnPoints[(int)team], tankPoolTransform);
		CorrectData(ref gameObjectComponent, team, TankTypes.PLAYER, PLAYER_TAG);
		SpawnPlayerCoroutine((gameObjectComponent.SelfEntity, team));
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RespawnBot(BotSides botSide) {
		ref GameObjectComponent gameObjectComponent = ref TankPoolManager.Instance.EnsureObject(botSpawnPoints[(int)botSide], tankPoolTransform);
		CorrectData(ref gameObjectComponent, (botSide == BotSides.TOP_LEFT || botSide == BotSides.TOP_RIGHT ? Teams.TOP : Teams.BOTTOM), TankTypes.BOT, BOT_TAG);
		SpawnBotCoroutine((gameObjectComponent.SelfEntity, botSide));

		AudioManager.Instance.RequestSound(GameSounds.BOT_CREATED);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetPlayerSpawnPoint(Teams playerTeam, Vector3 position) {
		playerSpawnPoints[(int)playerTeam] = position;
		playerSpawnPointObjects[(int)playerTeam].transform.position = playerSpawnPoints[(int)playerTeam];
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SetBotSpawnPoint(BotSides botSide, Vector3 position) {
		botSpawnPoints[(int)botSide] = position;
		botSpawnPointObjects[(int)botSide].transform.position = botSpawnPoints[(int)botSide];
	}

	private void CorrectData(ref GameObjectComponent gameObjectComponent, Teams team, TankTypes tankType, string tankTag) {
		ref SpriteRendererComponent rendererComponent = ref NotHasGet<SpriteRendererComponent>(gameObjectComponent.SelfEntity);
		ref ColliderComponent colliderComponent = ref NotHasGet<ColliderComponent>(gameObjectComponent.SelfEntity);
		ref AnimatorComponent animatorComponent = ref NotHasGet<AnimatorComponent>(gameObjectComponent.SelfEntity);
		if (rendererComponent.SelfRenderer == null) {
			rendererComponent.SelfRenderer = gameObjectComponent.Self.GetComponent<SpriteRenderer>();
		}
		if (colliderComponent.SelfCollider == null) {
			colliderComponent.SelfCollider = gameObjectComponent.Self.GetComponent<Collider2D>();
		}
		if (animatorComponent.SelfAnimator == null) {
			animatorComponent.SelfAnimator = gameObjectComponent.SelfTransform.GetChild(TANK_WHEELS_CHILD_ID).GetComponent<Animator>();
		}
		ref HealthComponent healthComponent = ref NotHasGet<HealthComponent>(gameObjectComponent.SelfEntity);
		ref TankComponent tankComponent = ref NotHasGet<TankComponent>(gameObjectComponent.SelfEntity);
		ref TeamComponent teamComponent = ref NotHasGet<TeamComponent>(gameObjectComponent.SelfEntity);

		// If this is an object with other presets, then set standard components
		if (gameObjectComponent.Self.tag != tankTag) {
			gameObjectComponent.Self.tag = tankTag;

			switch (tankType) {
				case TankTypes.PLAYER:
					HasRemove<BotComponent>(gameObjectComponent.SelfEntity);
					ref PlayerComponent playerComponent = ref NotHasGet<PlayerComponent>(gameObjectComponent.SelfEntity);
					playerComponent.Invulnerability = gameObjectComponent.SelfTransform.GetChild(PLAYER_INVULNERABILITY_CHILD_ID).gameObject;
					playerComponent.Shield = gameObjectComponent.SelfTransform.GetChild(PLAYER_SHIELD_CHILD_ID).gameObject;
					playerComponent.ShieldRenderer = playerComponent.Shield.GetComponent<SpriteRenderer>();
					break;
				case TankTypes.BOT:
					HasRemove<PlayerComponent>(gameObjectComponent.SelfEntity);
					NotHasGet<BotComponent>(gameObjectComponent.SelfEntity);
					break;
				default:
					break;
			}
			tankComponent.TankType = tankType;
			tankComponent.TankLevel = TankLevels.FIRST;
			tankComponent.FirePeriod = PlayManager.Instance.standardTankFirePeriod;
			tankComponent.Velocity = PlayManager.Instance.standardTankVelocity;
			teamComponent.Team = team;

			if (tankComponent.BulletStartPoint == null) {
				tankComponent.BulletStartPoint = gameObjectComponent.SelfTransform.GetChild(BULLET_SPAWN_POINT_CHILD_ID);
			}
			if (tankComponent.TankParts == null) {
				tankComponent.TankParts = new SpriteRenderer[(int)TankParts.COUNT];
				for (int i = 0; i < tankComponent.TankParts.Length; i++) {
					tankComponent.TankParts[i] = gameObjectComponent.SelfTransform.GetChild(i).GetComponent<SpriteRenderer>(); // This implies that the body parts will have child id from 0 to 5
				}
			}
		}

		// Set default values
		switch (tankType) {
			case TankTypes.PLAYER:
				healthComponent.Health = (int)standardPlayerLives;
				UIManager.Instance.SetLife(teamComponent.Team, (int)standardPlayerLives);
				ref PlayerComponent playerComponent = ref NotHasGet<PlayerComponent>(gameObjectComponent.SelfEntity);
				SkinManager.Instance.SetPlayerSkin(ref tankComponent, ref playerComponent, SkinManager.playersSkin[(int)team]);
				break;
			case TankTypes.BOT:
				tankComponent.TankLevel = (TankLevels)Random.Range(0, (int)TankLevels.COUNT);
				healthComponent.Health = (int)tankComponent.TankLevel * (int)standardBotLives;
				NotHasGet<BotComponent>(gameObjectComponent.SelfEntity);
				SkinManager.Instance.SetBotSkin(ref tankComponent, SkinManager.botsSkin[(int)team]);
				break;
			default:
				break;
		}
		tankComponent.TankDirection = team == Teams.TOP ? Directions.DOWN : Directions.UP;
		tankComponent.TankState = States.IDLE;
	}

	/// <summary>
	/// Generates a random bonus and places it in a random position.
	/// </summary>
	private void GenerateBonus() {
		BufferVector.x = Random.Range(MapTopLeftCorner.x + 1f, MapBottomRightCorner.x - 1f);
		BufferVector.y = Random.Range(MapTopLeftCorner.y - 1f, MapBottomRightCorner.y + 1f);
		BonusTypes bonus = (BonusTypes)Random.Range(0, (int)BonusTypes.COUNT);
		ref GameObjectComponent gameObjectComponent = ref BonusPoolManager.Instance.EnsureObject(BufferVector, spawnTransform);
		ref BonusComponent bonusComponent = ref NotHasGet<BonusComponent>(gameObjectComponent.SelfEntity);
		ref SpriteRendererComponent rendererComponent = ref NotHasGet<SpriteRendererComponent>(gameObjectComponent.SelfEntity);
		if (rendererComponent.SelfRenderer == null) {
			rendererComponent.SelfRenderer = gameObjectComponent.Self.GetComponent<SpriteRenderer>();
		}
		bonusComponent.Bonus = bonus;
		rendererComponent.SelfRenderer.sprite = SkinManager.Instance.GetBonusSprite(bonus);

		AudioManager.Instance.RequestSound(GameSounds.BONUS_CREATED);
		HidingBonusCoroutine(gameObjectComponent.SelfEntity);
	}

	// Bot coroutines
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SpawnBotCoroutine((IEntity botEntity, BotSides botSide) data) => StartCoroutine("SpawnBotInitializationRoutine", (data.botEntity, data.botSide));
	private void FlipBotSpawns(IEntity botEntity, BotSides botSide, bool botState) {
		ref GameObjectComponent gameObjectComponent = ref botEntity.GetComponent<GameObjectComponent>();
		gameObjectComponent.Self.SetActive(botState);
		botSpawnPointObjects[(int)botSide].SetActive(!botState);
	}
	private IEnumerator SpawnBotInitializationRoutine((IEntity botEntity, BotSides botSide) data) {
		FlipBotSpawns(data.botEntity, data.botSide, false);

#if UNITY_EDITOR
		yield return new WaitForSeconds(standardSpawningTime);
#else
		yield return standardSpawningWFS;
#endif

		FlipBotSpawns(data.botEntity, data.botSide, true);

		yield break;
	}
	private IEnumerator RespawnBotRoutine() {
		while (true) {
#if UNITY_EDITOR
			yield return new WaitForSeconds(botSpawnDuration);
#else
			yield return botSpawningWFS;
#endif
			RespawnBot(BotSides.TOP_LEFT);
			RespawnBot(BotSides.TOP_RIGHT);
			RespawnBot(BotSides.BOTTOM_LEFT);
			RespawnBot(BotSides.BOTTOM_RIGHT);
		}
	}

	//

	// Player coroutines

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SpawnPlayerCoroutine((IEntity playerEntity, Teams team) data) => StartCoroutine("SpawnPlayerInitializationRoutine", (data.playerEntity, data.team));
	private void FlipPlayerSpawnsHelper(IEntity playerEntity, Teams player, bool playerState) {
		ref GameObjectComponent gameObjectComponent = ref playerEntity.GetComponent<GameObjectComponent>();
		gameObjectComponent.Self.SetActive(playerState);
		playerSpawnPointObjects[(int)player].SetActive(!playerState);
	}
	private IEnumerator SpawnPlayerInitializationRoutine((IEntity playerEntity, Teams team) data) {
		FlipPlayerSpawnsHelper(data.playerEntity, data.team, false);

#if UNITY_EDITOR
		yield return new WaitForSeconds(standardSpawningTime);
#else
		yield return standardSpawningWFS;
#endif

		FlipPlayerSpawnsHelper(data.playerEntity, data.team, true);

		PlayManager.Instance.SetPlayerInvulnerableCoroutine(data.playerEntity);
		
		yield break;
	}

	//

	// Bonus coroutines

	private IEnumerator BonusSpawnerRoutine() {
		while (true) {
			yield return new WaitForSeconds(Random.Range(fromSpawnBonusDuration, toSpawnBonusDuration));
			GenerateBonus();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] private void HidingBonusCoroutine(IEntity bonusEntity) => StartCoroutine("BonusHidingRoutine", bonusEntity);
	private IEnumerator BonusHidingRoutine(IEntity bonusEntity) {
		SpriteRenderer renderer = bonusEntity.GetComponent<SpriteRendererComponent>().SelfRenderer;
		float tempDuration = bonusHidingDuration;
		while (tempDuration > BONUS_HIDING_BELOW_LIMIT) {
			tempDuration *= bonusHidingScale;
			yield return new WaitForSeconds(tempDuration);
			renderer.enabled = false;
			yield return bonusBlinkWFS;
			renderer.enabled = true;
		}
		BonusPoolManager.Instance.DestroyObject(bonusEntity, true);
	}

	//

}
