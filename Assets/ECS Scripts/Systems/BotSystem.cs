using Morpeh;
using UnityEngine;
using Unity.IL2CPP.CompilerServices;
using static Statics;

[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
[CreateAssetMenu(menuName = "ECS/Systems/" + nameof(BotSystem))]
public sealed class BotSystem : UpdateSystem
{

	private const float THINKING_TIME = 3f; // Than smaller than better(i guess...)

	private static GameObject[] raycastObjects = new GameObject[(int)Directions.COUNT];
	private static IEntity[] raycastEntities = new IEntity[(int)Directions.COUNT];

	public override void OnAwake() { }

	public override void OnUpdate(float deltaTime) {
		ref Filter.ComponentsBag<GameObjectComponent> gameObjectBag = ref Only_Bots_Filter.Select<GameObjectComponent>();
		ref Filter.ComponentsBag<TankComponent> tankBag = ref Only_Bots_Filter.Select<TankComponent>();
		ref Filter.ComponentsBag<TeamComponent> teamBag = ref Only_Bots_Filter.Select<TeamComponent>();
		ref Filter.ComponentsBag<BotComponent> botBag = ref Only_Bots_Filter.Select<BotComponent>();

		for (int i = 0; i < Only_Bots_Filter.Length; i++) {
			ref GameObjectComponent gameObjectComponent = ref gameObjectBag.GetComponent(i);
			if (!gameObjectComponent.Self.activeSelf)
				continue;

			ref TankComponent tankComponent = ref tankBag.GetComponent(i);
			ref TeamComponent teamComponent = ref teamBag.GetComponent(i);
			ref BotComponent botComponent = ref botBag.GetComponent(i);

			if (teamComponent.Team != PlayManager.frozenTeam) {
				MoveAI(ref gameObjectComponent, ref tankComponent, ref teamComponent, ref botComponent);
			}
		}
	}

	/// <summary>
	/// This method tries to raycast in four directions to detect any GameObjects and decide to move the best way to win the Game for the Team.
	/// </summary>
	private void MoveAI(ref GameObjectComponent gameObjectComponent, ref TankComponent tankComponent, ref TeamComponent teamComponent, ref BotComponent botComponent) {
		Vector3 tempPosition = gameObjectComponent.SelfTransform.position;
		RaycastHit2D upRaycast = Physics2D.Raycast(tempPosition + Vector_Up, Vector_Up);
		RaycastHit2D downRaycast = Physics2D.Raycast(tempPosition + Vector_Down, Vector_Down);
		RaycastHit2D rightRaycast = Physics2D.Raycast(tempPosition + Vector_Right, Vector_Right);
		RaycastHit2D leftRaycast = Physics2D.Raycast(tempPosition + Vector_Left, Vector_Left);

		raycastObjects[(int)Directions.UP] = upRaycast.collider?.gameObject;
		raycastObjects[(int)Directions.DOWN] = downRaycast.collider?.gameObject;
		raycastObjects[(int)Directions.RIGHT] = rightRaycast.collider?.gameObject;
		raycastObjects[(int)Directions.LEFT] = leftRaycast.collider?.gameObject;

		raycastEntities[(int)Directions.UP] = raycastObjects[(int)Directions.UP]?.GetComponent<IEntityProvider>()?.Entity;
		raycastEntities[(int)Directions.DOWN] = raycastObjects[(int)Directions.DOWN]?.GetComponent<IEntityProvider>()?.Entity;
		raycastEntities[(int)Directions.RIGHT] = raycastObjects[(int)Directions.RIGHT]?.GetComponent<IEntityProvider>()?.Entity;
		raycastEntities[(int)Directions.LEFT] = raycastObjects[(int)Directions.LEFT]?.GetComponent<IEntityProvider>()?.Entity;

		// The bot tries to detect something to shoot at
		Directions tryShootDirection = TryGetShootDirection(ref tankComponent, ref teamComponent);
		if (tryShootDirection != Directions.COUNT) { // Think of this as checking for null
			tankComponent.TankDirection = tryShootDirection;
			tankComponent.Fired = true;
			return;
		}

		// The bot tries to move correctly
		if (Time.time - botComponent.LastTimeMoved >= THINKING_TIME) {
			tankComponent.TankDirection = (Directions)Random.Range(0, (int)Directions.COUNT);
			/*tankComponent.TankDirection =
				(tankComponent.TankDirection == Directions.UP || tankComponent.TankDirection == Directions.DOWN) ?
				(teamComponent.Team == Teams.TOP ? Directions.DOWN : Directions.UP) :
				tankComponent.TankDirection;*/
			tankComponent.TankState = States.MOVING;
			botComponent.LastTimeMoved = Time.time;
		}



		Directions TryGetShootDirection(ref TankComponent tempTankComponent, ref TeamComponent tempTeamComponent) {
			for (int i = 0; i < (int)Directions.COUNT; i++) {
				if (raycastEntities[i] == null) {
					continue;
				}

				// Shooting flag is more priority than shooting tanks
				if (raycastObjects[i].tag == Block_Tags[(int)BlockTypes.FLAG] && tempTeamComponent.Team != raycastEntities[i]?.GetComponent<TeamComponent>().Team) {
					return (Directions)i;
				}
				// Shooting tanks is more priority than shooting blocks
				if ((raycastObjects[i].tag == BOT_TAG ||
					raycastObjects[i].tag == PLAYER_TAG) && tempTeamComponent.Team != raycastEntities[i]?.GetComponent<TeamComponent>().Team) {
					return (Directions)i;
				}
				// Shooting blocks is the lowest priority
				if (raycastObjects[i].tag == Block_Tags[(int)BlockTypes.BRICK] ||
					raycastObjects[i].tag == Block_Tags[(int)BlockTypes.LEAVES] ||
					(raycastObjects[i].tag == Block_Tags[(int)BlockTypes.STEEL] && tempTankComponent.TankLevel == TankLevels.FOURTH)) {
					return (Directions)i;
				}
			}

			return Directions.COUNT; // Think of this as a flag to avoid shooting
		}
	}
}