using Morpeh;
using UnityEngine;
using Unity.IL2CPP.CompilerServices;
using static Statics;
using System.Runtime.CompilerServices;
using UnityEngine.Scripting.APIUpdating;

[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
[CreateAssetMenu(menuName = "ECS/Systems/" + nameof(BulletSystem))]
public sealed class BulletSystem : UpdateSystem {

	public override void OnAwake() { }

	public override void OnUpdate(float deltaTime) {
		ref Filter.ComponentsBag<GameObjectComponent> gameObjectBag = ref All_Bullets_Filter.Select<GameObjectComponent>();
		ref Filter.ComponentsBag<BulletComponent> bulletBag = ref All_Bullets_Filter.Select<BulletComponent>();

		for (int i = 0; i < All_Bullets_Filter.Length; i++) {
			ref GameObjectComponent gameObjectComponent = ref gameObjectBag.GetComponent(i);
			if (!gameObjectComponent.Self.activeSelf)
				continue;

			ref BulletComponent bulletComponent = ref bulletBag.GetComponent(i);

			Move(ref gameObjectComponent, ref bulletComponent, deltaTime);
		}
	}


	[MethodImpl(MethodImplOptions.AggressiveInlining)] private void Move(ref GameObjectComponent gameObjectComponent, ref BulletComponent bulletComponent, float deltaTime) => gameObjectComponent.SelfTransform.transform.Translate(bulletComponent.Velocity * Vector_Up * deltaTime);

}