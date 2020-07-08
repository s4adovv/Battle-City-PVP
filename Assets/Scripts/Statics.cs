using System.Runtime.CompilerServices;
using Morpeh;
using UnityEngine;

public static class Statics
{

	#region Enums

	public enum MapCodes { EMPTY, BRICK, STEEL, WATER, LEAVES, GRAVEL, FLAG, PLAYER, BOT, COUNT }
   
   public enum BlockTypes { BRICK, STEEL, WATER, LEAVES, GRAVEL, FLAG, COUNT }

   public enum BonusTypes { KILL_ALL, EXTRA_LIFE, TIME_STOP, SHIELD, POWER_UP, COUNT }

   public enum PlayerColors { STANDARD, GOLD, SILVER, BRONZE, PURPLE, BLACK, RED, GREEN, BLUE, COUNT }

   public enum BotColors { STANDARD, GRAY, GRAY_RED, GRAY_GREEN, GRAY_BLUE, COUNT }

   public enum Teams { TOP, BOTTOM, COUNT }

   public enum BotSides { TOP_LEFT, TOP_RIGHT, BOTTOM_LEFT, BOTTOM_RIGHT, COUNT }

   public enum TankTypes { PLAYER, BOT, COUNT }

   public enum TankLevels { FIRST, SECOND, THIRD, FOURTH, COUNT }

   public enum TankParts { BODY, LIGHT, SHADOW, WHEEL_BODY, WHEEL_LIGHT, WHEEL_SHADOW, COUNT }

   public enum Directions { UP, DOWN, LEFT, RIGHT, COUNT }

   public enum States { MOVING, IDLE, COUNT }

   public enum GameStates { MENU, GAME, COUNT }

   public enum KnownPools { BLOCK, TANK, BULLET, BONUS, BLOW, COUNT }

   public enum GameSounds { BONUS_CREATED, BONUS_TAKEN, TIME_STOP, LIFE_TAKEN, BOT_CREATED, SHIELD_HIT, BRICK_HIT, STEEL_HIT, SHOOT, PLAYER_EXPLOSION, BOT_EXPLOSION, IDLE, MOVING, GAME_OVER, GAME_START, COUNT }

   #endregion

   public const string WHEELS_ANIMATOR_BOOL_NAME = "Moving";

   public const string PLAYER_TAG = "Player";
   public const string BOT_TAG = "Bot";
   public const string FLAG_TAG = "Flag";
   public const string BONUS_TAG = "Bonus";

   public static readonly string[] Block_Tags = new string[] {
      "Brick block",
      "Steel block",
      "Water block",
      "Leaves block",
      "Gravel block",
      "Flag" }; // Like BlockTypes enum

   public static readonly Quaternion[] Rotations = new Quaternion[] {
      Quaternion.Euler(0, 0, 0),
      Quaternion.Euler(0, 0, 180),
      Quaternion.Euler(0, 0, 90),
      Quaternion.Euler(0, 0, -90) }; // UP, BOTTOM LEFT, RIGHT

   // Cached objects

   public static readonly Vector3 Vector_Up = Vector3.up;
   public static readonly Vector3 Vector_Down = Vector3.down;
   public static readonly Vector3 Vector_Right = Vector3.right;
   public static readonly Vector3 Vector_Left = Vector3.left;
   public static readonly Vector3 Vector_Zero = Vector3.zero;
   public static readonly Vector3 Vector_One = Vector3.one;
   public static readonly Vector3 Vector_Half = new Vector3(0.5f, 0.5f, 1f);

   public static readonly Quaternion Quaternion_Identity = Quaternion.identity;

   public static readonly World Default_World = World.Default;

   public static readonly Filter All_Entities_Filter = Default_World.Filter.With<GameObjectComponent>();
   public static readonly Filter All_Tanks_Filter = Default_World.Filter.With<TankComponent>();
   public static readonly Filter Only_Players_Filter = All_Tanks_Filter.With<PlayerComponent>();
   public static readonly Filter Only_Bots_Filter = All_Tanks_Filter.With<BotComponent>();
   public static readonly Filter All_Bullets_Filter = Default_World.Filter.With<BulletComponent>();
   public static readonly Filter All_Health_Filter = Default_World.Filter.With<HealthComponent>();
   public static readonly Filter All_Blocks_Filter = Default_World.Filter.With<BlockComponent>();
   public static readonly Filter Only_Blocks_Health_Filter = All_Blocks_Filter.With<HealthComponent>();
   public static readonly Filter Only_Blocks_Not_Health_Filter = All_Blocks_Filter.Without<HealthComponent>();
   public static readonly Filter All_Bonuses_Filter = All_Blocks_Filter.Without<BonusComponent>();


   public static Vector3 MapTopLeftCorner;
   public static Vector3 MapBottomRightCorner;

   public static EntityPool[] Pools = new EntityPool[(int)KnownPools.COUNT]; // You should fill it after instancing

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static ref T NotHasGet<T>(IEntity entity) where T : struct, IComponent {
		if (!entity.Has<T>()) {
         ref T component = ref entity.AddComponent<T>();
         return ref component;
		}

      return ref entity.GetComponent<T>();
	}

   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   public static void HasRemove<T>(IEntity entity) where T : struct, IComponent {
      if (entity.Has<T>()) {
         entity.RemoveComponent<T>();
      }
   }

}
