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

	private static readonly int New_Line_Length = Environment.NewLine.Length;
	private static readonly char Char_To_Avoid = Environment.NewLine[0];

	public static MapManager Instance;

	private static Vector3 BufferVector = Vector3.zero;

	[SerializeField][Range(0, int.MaxValue / 2)] private int flagLives, brickLives, steelLives, leavesLives;
	[SerializeField] private Sprite[] blockSprites;
	[SerializeField] private TextAsset[] maps;

	private void Awake() {
		if (Instance == null) {
			Instance = this;
		}

		MapTopLeftCorner = transform.position;
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

			for (int i = 0, j = 0; i < MAP_HEIGHT; i++) {
				while (*ptr != Char_To_Avoid && j < MAP_WIDTH) {
					MapCodes tempCode = (MapCodes)(*ptr & 0b1111); // 0b1111 - This is a bit mask that allows you to convert digit chars to the real digits
					switch (tempCode) {
						case MapCodes.EMPTY:
							ptr++;
							j++;
							break;
						case MapCodes.FLAG:
							if (i == 0 || (MapCodes)(*(ptr - Salted_Map_Width) & 0b1111) != MapCodes.FLAG) { // Check upper code to avoid duplication of Flag
								CorrectBlockBehaviour(BlockPoolManager.Instance.Get(Get2x2BlockPosition(MapTopLeftCorner, j, i), Quaternion_Identity), BlockTypes.FLAG, flagStack++);
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
							CorrectBlockBehaviour(BlockPoolManager.Instance.Get(GetBlockPosition(MapTopLeftCorner, j, i), Quaternion_Identity), tempType, Teams.COUNT); // I set Teams.COUNT(It doesn't matter actually) because neither block has no team, except for flags
							ptr++;
							j++;
							break;
					}
				}
				ptr += New_Line_Length; // Skip new line chars
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

	private void CorrectBlockBehaviour(IEntity blockEntity, BlockTypes blockType, Teams team) {
		ref GameObjectComponent blockGameObjectComponent = ref blockEntity.GetComponent<GameObjectComponent>();
		ref HealthComponent blockHealthComponent = ref NotHasGet<HealthComponent>(blockEntity);
		if (blockGameObjectComponent.Self.tag != Block_Tags[(int)blockType]) {
			HasRemove<TeamComponent>(blockEntity);
			ref BlockComponent blockComponent = ref NotHasGet<BlockComponent>(blockEntity);
			ref SpriteRendererComponent blockRendererComponent = ref NotHasGet<SpriteRendererComponent>(blockEntity);
			ref ColliderComponent blockColliderComponent = ref NotHasGet<ColliderComponent>(blockEntity);
			ref AnimatorComponent blockAnimatorComponent = ref NotHasGet<AnimatorComponent>(blockEntity);
			if (blockRendererComponent.SelfRenderer == null) {
				blockRendererComponent.SelfRenderer = blockGameObjectComponent.Self.GetComponent<SpriteRenderer>();
			}
			if (blockColliderComponent.SelfCollider == null) {
				blockColliderComponent.SelfCollider = blockGameObjectComponent.Self.GetComponent<Collider2D>();
			}
			if (blockAnimatorComponent.SelfAnimator == null) {
				blockAnimatorComponent.SelfAnimator = blockGameObjectComponent.Self.GetComponent<Animator>();
			}

			blockAnimatorComponent.SelfAnimator.enabled = false;
			blockColliderComponent.SelfCollider.enabled = true;
			blockColliderComponent.SelfCollider.isTrigger = false;
			blockGameObjectComponent.SelfTransform.localScale = Vector_Half;
			blockRendererComponent.SelfRenderer.sortingLayerName = GRASS_LAYER_NAME;

			blockComponent.BlockType = blockType;
			blockGameObjectComponent.Self.tag = Block_Tags[(int)blockType];
			blockRendererComponent.SelfRenderer.sprite = blockSprites[(int)blockType];
			switch (blockType) {
				case BlockTypes.STEEL:
					blockHealthComponent.Invulnerable = true;
					break;
				case BlockTypes.WATER:
					blockAnimatorComponent.SelfAnimator.enabled = true;
					HasRemove<HealthComponent>(blockEntity);
					break;
				case BlockTypes.LEAVES:
					blockColliderComponent.SelfCollider.isTrigger = true;
					blockRendererComponent.SelfRenderer.sortingLayerName = CLOUD_LAYER_NAME;
					break;
				case BlockTypes.GRAVEL:
					blockColliderComponent.SelfCollider.enabled = false;
					HasRemove<HealthComponent>(blockEntity);
					break;
				case BlockTypes.FLAG:
					blockGameObjectComponent.SelfTransform.localScale = Vector_One;
					break;
				default:
					break;
			}
		}
		switch (blockType) {
			case BlockTypes.BRICK:
				blockHealthComponent.Health = brickLives;
				break;
			case BlockTypes.STEEL:
				blockHealthComponent.Health = steelLives;
				break;
			case BlockTypes.LEAVES:
				blockHealthComponent.Health = leavesLives;
				break;
			case BlockTypes.FLAG:
				blockHealthComponent.Health = flagLives;
				ref TeamComponent flagTeamComponent = ref NotHasGet<TeamComponent>(blockEntity);
				flagTeamComponent.Team = team;
				break;
			default:
				break;
		}
	}

}
