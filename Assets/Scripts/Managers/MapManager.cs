using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Morpeh;
using UnityEngine;
using static Statics;

public unsafe class MapManager : MonoBehaviour
{

	private const string GRASS_LAYER_NAME = "Grass";
	private const string CLOUD_LAYER_NAME = "Cloud";

	private const int MAP_WIDTH = 30, MAP_HEIGHT = 18;
	private const float BLOCK_SIZE = 0.5f;
	private const float FULL_BLOCK_STEP = 0.5f;
	private const float HALF_BLOCK_STEP = FULL_BLOCK_STEP / 2;

	private static readonly int Salted_Map_Width = MAP_WIDTH + Environment.NewLine.Length; // Blocks count + NewLine chars

	private static readonly Vector3 Block_Scale = new Vector3(BLOCK_SIZE, BLOCK_SIZE, 1);
	private static readonly Vector3 Half_Block_Vector = new Vector3(HALF_BLOCK_STEP, -HALF_BLOCK_STEP);

	public static MapManager Instance;

	private static Vector3 BufferVector = Vector3.zero;

	[SerializeField][Range(0, int.MaxValue / 2)] private int flagLives, brickLives, steelLives, leavesLives;
	[SerializeField] private Sprite[] blockSprites;
	[SerializeField] private TextAsset[] maps;

	private Transform mapTransform;

	private void Awake() {
		if (Instance == null) {
			Instance = this;
		}

		mapTransform = transform;
		MapTopLeftCorner = mapTransform.position;
		MapBottomRightCorner = new Vector3(MapTopLeftCorner.x + FULL_BLOCK_STEP * MAP_WIDTH, MapTopLeftCorner.y - FULL_BLOCK_STEP * MAP_HEIGHT);
	}

	public void CreateRandomMap() {
		BlockPoolManager.Instance.FreePool(false);
		CreateMap(UnityEngine.Random.Range(0, maps.Length));
	}

	public void CreateMap(int mapID) {
		fixed (char* fixedPtr = maps[mapID].text) {
			char* ptr = fixedPtr;

			// To make sure i selected the right(Blue/Red Flag, Top/Bottom Player...)
			BotSides botStack = BotSides.TOP_LEFT;
			Teams playerStack = Teams.TOP;
			Teams flagStack = Teams.TOP;
			//

			int newLineLength = Environment.NewLine.Length;
			char charToAvoid = Environment.NewLine[0];
			for (int i = 0, j = 0; i < MAP_HEIGHT; i++) {
				while (*ptr != charToAvoid && j < MAP_WIDTH) {
					MapCodes tempCode = (MapCodes)(*ptr & 0b1111); // 0b1111 - This is a bit mask that allows you to convert digit chars to the real digits
					switch (tempCode) {
						case MapCodes.EMPTY:
							ptr++;
							j++;
							break;
						case MapCodes.FLAG:
							if (i == 0 || (MapCodes)(*(ptr - Salted_Map_Width) & 0b1111) != MapCodes.FLAG) { // Check upper code to avoid duplication of Flag
								ref GameObjectComponent flagGameObjectComponent = ref BlockPoolManager.Instance.EnsureObject(Get2x2BlockPosition(MapTopLeftCorner, j, i), Quaternion_Identity, mapTransform);
								CorrectBlockBehaviour(ref flagGameObjectComponent, BlockTypes.FLAG, flagStack++);
							}
							ptr += 2; // Skip because of (2x2) block size
							j += 2;
							break;
						case MapCodes.PLAYER:
							if (i == 0 || (MapCodes)(*(ptr - Salted_Map_Width) & 0b1111) != MapCodes.PLAYER) { // Check upper code to avoid duplication of Player spawn point
								SpawnManager.Instance.SetPlayerSpawnPoint(playerStack++, Get2x2BlockPosition(MapTopLeftCorner, j, i));
							}
							ptr += 2; // Skip because of (2x2) block size
							j += 2;
							break;
						case MapCodes.BOT:
							if (i == 0 || (MapCodes)(*(ptr - Salted_Map_Width) & 0b1111) != MapCodes.BOT) { // Check upper code to avoid duplication of Bot spawn point
								SpawnManager.Instance.SetBotSpawnPoint(botStack++, Get2x2BlockPosition(MapTopLeftCorner, j, i));
							}
							ptr += 2; // Skip because of (2x2) block size
							j += 2;
							break;
						default:
							BlockTypes tempType = (BlockTypes)(tempCode - 1); // Minus one because of the empty block
							ref GameObjectComponent gameObjectComponent = ref BlockPoolManager.Instance.EnsureObject(GetBlockPosition(MapTopLeftCorner, j, i), Quaternion_Identity, mapTransform);
							CorrectBlockBehaviour(ref gameObjectComponent, tempType, Teams.COUNT); // I set Teams.COUNT(It doesn't matter actually) because neither block has no team, except for flags
							ptr++;
							j++;
							break;
					}
				}
				ptr += newLineLength; // Skip new line chars
				j = 0;
			}
		}

		Vector3 GetBlockPosition(Vector3 rootPos, int XPoint, int YPoint) {
			BufferVector.x = FULL_BLOCK_STEP * XPoint;
			BufferVector.y = -FULL_BLOCK_STEP * YPoint;
			return rootPos + BufferVector;
		}
		Vector3 Get2x2BlockPosition(Vector3 rootPos, int XPoint, int YPoint) {
			BufferVector.x = FULL_BLOCK_STEP * XPoint;
			BufferVector.y = -FULL_BLOCK_STEP * YPoint;
			return rootPos + BufferVector + Half_Block_Vector;
		}
	}

	private void CorrectBlockBehaviour(ref GameObjectComponent gameObjectComponent, BlockTypes blockType, Teams team) {
		if (gameObjectComponent.Self.tag != Block_Tags[(int)blockType]) {
			NotHasGet<HealthComponent>(gameObjectComponent.SelfEntity);
			HasRemove<TeamComponent>(gameObjectComponent.SelfEntity);
			ref BlockComponent blockComponent = ref NotHasGet<BlockComponent>(gameObjectComponent.SelfEntity);
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
				animatorComponent.SelfAnimator = gameObjectComponent.Self.GetComponent<Animator>();
			}

			animatorComponent.SelfAnimator.enabled = false;
			colliderComponent.SelfCollider.enabled = true;
			colliderComponent.SelfCollider.isTrigger = false;
			gameObjectComponent.SelfTransform.localScale = Vector_Half;
			rendererComponent.SelfRenderer.sortingLayerName = GRASS_LAYER_NAME;

			blockComponent.BlockType = blockType;
			gameObjectComponent.Self.tag = Block_Tags[(int)blockType];
			rendererComponent.SelfRenderer.sprite = blockSprites[(int)blockType];
			switch (blockType) {
				case BlockTypes.STEEL:
					ref HealthComponent tempHealthComponent = ref NotHasGet<HealthComponent>(gameObjectComponent.SelfEntity);
					tempHealthComponent.Invulnerable = true;
					break;
				case BlockTypes.WATER:
					animatorComponent.SelfAnimator.enabled = true;
					HasRemove<HealthComponent>(gameObjectComponent.SelfEntity);
					break;
				case BlockTypes.LEAVES:
					colliderComponent.SelfCollider.isTrigger = true;
					rendererComponent.SelfRenderer.sortingLayerName = CLOUD_LAYER_NAME;
					break;
				case BlockTypes.GRAVEL:
					colliderComponent.SelfCollider.enabled = false;
					HasRemove<HealthComponent>(gameObjectComponent.SelfEntity);
					break;
				case BlockTypes.FLAG:
					gameObjectComponent.SelfTransform.localScale = Vector_One;
					ref TeamComponent teamComponent = ref NotHasGet<TeamComponent>(gameObjectComponent.SelfEntity);
					teamComponent.Team = team;
					break;
				default:
					break;
			}
		}
		switch (blockType) {
			case BlockTypes.BRICK:
				ref HealthComponent healthComponent = ref NotHasGet<HealthComponent>(gameObjectComponent.SelfEntity); // I copy pasted it just so the health component won't be added to non health blocks(Gravel, Water)
				healthComponent.Health = brickLives;
				break;
			case BlockTypes.STEEL:
				healthComponent = ref NotHasGet<HealthComponent>(gameObjectComponent.SelfEntity);
				healthComponent.Health = steelLives;
				break;
			case BlockTypes.LEAVES:
				healthComponent = ref NotHasGet<HealthComponent>(gameObjectComponent.SelfEntity);
				healthComponent.Health = leavesLives;
				break;
			case BlockTypes.FLAG:
				healthComponent = ref NotHasGet<HealthComponent>(gameObjectComponent.SelfEntity);
				healthComponent.Health = flagLives;
				break;
			default:
				break;
		}
	}

}
