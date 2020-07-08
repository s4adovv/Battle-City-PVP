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
		IEntity playerEntity = TankPoolManager.Instance.EnsureObject(playerSpawnPoints[(int)team], tankPoolTransform);
		CorrectData(playerEntity, team, TankTypes.PLAYER, PLAYER_TAG);
		SpawnPlayerCoroutine(playerEntity, team);
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RespawnBot(BotSides botSide) {
		IEntity botEntity = TankPoolManager.Instance.EnsureObject(botSpawnPoints[(int)botSide], tankPoolTransform);
		CorrectData(botEntity, (botSide == BotSides.TOP_LEFT || botSide == BotSides.TOP_RIGHT ? Teams.TOP : Teams.BOTTOM), TankTypes.BOT, BOT_TAG);
		SpawnBotCoroutine(botEntity, botSide);

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

	private void CorrectData(IEntity tankEntity, Teams team, TankTypes tankType, string tankTag) {
		ref GameObjectComponent tankGameObjectComponent = ref tankEntity.GetComponent<GameObjectComponent>();
		ref SpriteRendererComponent tankRendererComponent = ref NotHasGet<SpriteRendererComponent>(tankEntity);
		ref ColliderComponent tankColliderComponent = ref NotHasGet<ColliderComponent>(tankEntity);
		ref AnimatorComponent tankAnimatorComponent = ref NotHasGet<AnimatorComponent>(tankEntity);
		if (tankRendererComponent.SelfRenderer == null) {
			tankRendererComponent.SelfRenderer = tankGameObjectComponent.Self.GetComponent<SpriteRenderer>();
		}
		if (tankColliderComponent.SelfCollider == null) {
			tankColliderComponent.SelfCollider = tankGameObjectComponent.Self.GetComponent<Collider2D>();
		}
		if (tankAnimatorComponent.SelfAnimator == null) {
			tankAnimatorComponent.SelfAnimator = tankGameObjectComponent.SelfTransform.GetChild(TANK_WHEELS_CHILD_ID).GetComponent<Animator>();
		}
		ref HealthComponent tankHealthComponent = ref NotHasGet<HealthComponent>(tankEntity);
		ref TankComponent tankComponent = ref NotHasGet<TankComponent>(tankEntity);
		ref TeamComponent tankTeamComponent = ref NotHasGet<TeamComponent>(tankEntity);

		// If this is an object with other presets, then set standard components
		if (tankGameObjectComponent.Self.tag != tankTag) {
			tankGameObjectComponent.Self.tag = tankTag;

			switch (tankType) {
				case TankTypes.PLAYER:
					HasRemove<BotComponent>(tankEntity);
					ref PlayerComponent playerComponent = ref NotHasGet<PlayerComponent>(tankEntity);
					playerComponent.Invulnerability = tankGameObjectComponent.SelfTransform.GetChild(PLAYER_INVULNERABILITY_CHILD_ID).gameObject;
					playerComponent.Shield = tankGameObjectComponent.SelfTransform.GetChild(PLAYER_SHIELD_CHILD_ID).gameObject;
					playerComponent.ShieldRenderer = playerComponent.Shield.GetComponent<SpriteRenderer>();
					break;
				case TankTypes.BOT:
					HasRemove<PlayerComponent>(tankEntity);
					NotHasGet<BotComponent>(tankEntity);
					break;
				default:
					break;
			}
			tankComponent.TankType = tankType;
			tankComponent.TankLevel = TankLevels.FIRST;
			tankComponent.FirePeriod = PlayManager.Instance.standardTankFirePeriod;
			tankComponent.Velocity = PlayManager.Instance.standardTankVelocity;
			tankTeamComponent.Team = team;

			if (tankComponent.BulletStartPoint == null) {
				tankComponent.BulletStartPoint = tankGameObjectComponent.SelfTransform.GetChild(BULLET_SPAWN_POINT_CHILD_ID);
			}
			if (tankComponent.TankParts == null) {
				tankComponent.TankParts = new SpriteRenderer[(int)TankParts.COUNT];
				for (int i = 0; i < tankComponent.TankParts.Length; i++) {
					tankComponent.TankParts[i] = tankGameObjectComponent.SelfTransform.GetChild(i).GetComponent<SpriteRenderer>(); // This implies that the body parts will have child id from 0 to 5
				}
			}
		}

		// Set default values
		switch (tankType) {
			case TankTypes.PLAYER:
				tankHealthComponent.Health = (int)standardPlayerLives;
				UIManager.Instance.SetLife(tankTeamComponent.Team, (int)standardPlayerLives);
				ref PlayerComponent playerComponent = ref NotHasGet<PlayerComponent>(tankEntity);
				SkinManager.Instance.SetPlayerSkin(ref tankComponent, ref playerComponent, SkinManager.playersSkin[(int)team]);
				break;
			case TankTypes.BOT:
				tankComponent.TankLevel = (TankLevels)Random.Range(0, (int)TankLevels.COUNT);
				tankComponent.TankState = States.MOVING;
				tankAnimatorComponent.SelfAnimator.SetBool(WHEELS_ANIMATOR_BOOL_NAME, false);
				tankHealthComponent.Health = (int)tankComponent.TankLevel * (int)standardBotLives;
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
		IEntity bonusEntity = BonusPoolManager.Instance.EnsureObject(BufferVector, spawnTransform);
		ref GameObjectComponent bonusGameObjectComponent = ref bonusEntity.GetComponent<GameObjectComponent>();
		ref BonusComponent bonusComponent = ref NotHasGet<BonusComponent>(bonusEntity);
		ref SpriteRendererComponent bonusRendererComponent = ref NotHasGet<SpriteRendererComponent>(bonusEntity);
		if (bonusRendererComponent.SelfRenderer == null) {
			bonusRendererComponent.SelfRenderer = bonusGameObjectComponent.Self.GetComponent<SpriteRenderer>();
		}
		bonusComponent.Bonus = bonus;
		bonusRendererComponent.SelfRenderer.sprite = SkinManager.Instance.GetBonusSprite(bonus);

		AudioManager.Instance.RequestSound(GameSounds.BONUS_CREATED);
		HidingBonusCoroutine(bonusEntity);
	}

	// Bot coroutines
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SpawnBotCoroutine(IEntity botEntity, BotSides botSide) => StartCoroutine("SpawnBotInitializationRoutine", (botEntity, botSide));
	private void FlipBotSpawns(IEntity botEntity, BotSides botSide, bool botState) {
		ref GameObjectComponent botGameObjectComponent = ref botEntity.GetComponent<GameObjectComponent>();
		botGameObjectComponent.Self.SetActive(botState);
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void SpawnPlayerCoroutine(IEntity playerEntity, Teams team) => StartCoroutine("SpawnPlayerInitializationRoutine", (playerEntity, team));
	private void FlipPlayerSpawnsHelper(IEntity playerEntity, Teams player, bool playerState) {
		ref GameObjectComponent playerGameObjectComponent = ref playerEntity.GetComponent<GameObjectComponent>();
		playerGameObjectComponent.Self.SetActive(playerState);
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
		SpriteRenderer bonusRenderer = bonusEntity.GetComponent<SpriteRendererComponent>().SelfRenderer;
		float tempDuration = bonusHidingDuration;
		while (tempDuration > BONUS_HIDING_BELOW_LIMIT) {
			tempDuration *= bonusHidingScale;
			yield return new WaitForSeconds(tempDuration);
			bonusRenderer.enabled = false;
			yield return bonusBlinkWFS;
			bonusRenderer.enabled = true;
		}
		BonusPoolManager.Instance.DestroyObject(bonusEntity);
	}

	//

}
