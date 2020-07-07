using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Morpeh;
using UnityEngine;
using static Statics;

public class PoolManager : MonoBehaviour
{

	private static readonly Predicate<IEntity> Default_Predicate = (entity) => !entity.GetComponent<GameObjectComponent>().InUsage;

	//HACK: if you want to Instantiate and Destroy without pooling objects, then just set it to 0
	public int MaxPoolSize => maxPoolSize;
	public GameObject DefaultPrefab => defaultPrefab;

	[SerializeField][Range(0, int.MaxValue / 2)] protected int maxPoolSize;
	/// <summary>
	/// How many objects to create at start.
	/// </summary>
	[SerializeField][Range(0, int.MaxValue / 2)] protected int prePoolCount;
	/// <summary>
	/// Default GameObject which will be used during the Instantiation.
	/// </summary>
	[SerializeField] protected GameObject defaultPrefab;
	/// <summary>
	/// The default parent to attach an object to.
	/// </summary>
	[SerializeField] protected Transform unsortedPoolParent;

	private int poolSize;

	/// <summary>
	/// Entity pool.
	/// Every entity will have GameObjectComponent.
	/// </summary>
	private List<IEntity> pool = new List<IEntity>();

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject() => ref EnsureObject(defaultPrefab);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(Vector3 position) => ref EnsureObject(defaultPrefab, position);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(Quaternion rotation) => ref EnsureObject(defaultPrefab, rotation);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(Transform parent) => ref EnsureObject(defaultPrefab, parent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(Predicate<IEntity> freeObjectPredicate) => ref EnsureObject(defaultPrefab, freeObjectPredicate);


	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(GameObject prefab) => ref EnsureObject(prefab, Vector_Zero, unsortedPoolParent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(GameObject prefab, Vector3 position) => ref EnsureObject(prefab, position, unsortedPoolParent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(GameObject prefab, Quaternion rotation) => ref EnsureObject(prefab, Vector_Zero, rotation, unsortedPoolParent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(GameObject prefab, Transform parent) => ref EnsureObject(prefab, Vector_Zero, parent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(GameObject prefab, Predicate<IEntity> freeObjectPredicate) => ref EnsureObject(prefab, Vector_Zero, unsortedPoolParent, freeObjectPredicate);


	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(GameObject prefab, Vector3 position, Quaternion rotation) => ref EnsureObject(prefab, position, rotation, unsortedPoolParent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(GameObject prefab, Vector3 position, Transform parent) => ref EnsureObject(prefab, position, Quaternion_Identity, parent);														  
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent) => ref EnsureObject(prefab, position, rotation, parent, Default_Predicate);				  
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(GameObject prefab, Vector3 position, Transform parent, Predicate<IEntity> freeObjectPredicate) => ref EnsureObject(prefab, position, Quaternion_Identity, parent, freeObjectPredicate);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(Vector3 position, Quaternion rotation) => ref EnsureObject(position, rotation, unsortedPoolParent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(Vector3 position, Transform parent) => ref EnsureObject(position, parent, Default_Predicate);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(Vector3 position, Transform parent, Predicate<IEntity> freeObjectPredicate) => ref EnsureObject(position, Quaternion_Identity, parent, freeObjectPredicate);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(Vector3 position, Quaternion rotation, Transform parent) => ref EnsureObject(position, rotation, parent, Default_Predicate);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public ref GameObjectComponent EnsureObject(Vector3 position, Quaternion rotation, Transform parent, Predicate<IEntity> freeObjectPredicate) => ref EnsureObject(defaultPrefab, position, rotation, parent, freeObjectPredicate);

	/// <summary>
	/// Get IEntity object with specified GameObject prefab.
	/// </summary>
	/// <param name="prefab">GameObject to instantiate.</param>
	/// <param name="position">Start position.</param>
	/// <param name="rotation">Start rotation.</param>
	/// <param name="parent">The parent to attach an object to.</param>
	/// <param name="freeObjectPredicate">A Predicate rule for finding free objects in the pool.</param>
	public ref GameObjectComponent EnsureObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, Predicate<IEntity> freeObjectPredicate) {
		if (maxPoolSize == 0) {
			ref GameObjectComponent gameObjectComponent = ref CreateStandardEntity(prefab, position, rotation, parent, true, true);
			return ref gameObjectComponent;
		}

		DestroyOutOfRangeObjects(freeObjectPredicate);
		int index = pool.FindIndex(freeObjectPredicate);

		if (index != -1) {
			IEntity entity = pool[index];
			ref GameObjectComponent gameObjectComponent = ref entity.GetComponent<GameObjectComponent>();
			gameObjectComponent.SelfTransform.position = position;
			gameObjectComponent.SelfTransform.rotation = rotation;
			gameObjectComponent.SelfTransform.parent = parent;
			gameObjectComponent.Self.SetActive(true);
			gameObjectComponent.InUsage = true;

			return ref gameObjectComponent;
		} else {
			ref GameObjectComponent gameObjectComponent = ref CreateStandardEntity(prefab, position, rotation, parent, true, true);
			pool.Add(gameObjectComponent.SelfEntity);
			poolSize++;

			return ref gameObjectComponent;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Obsolete("Use it if you want straight control of your pool")]
	public void DestroyObject(IEntity entity, bool destroyAttachedGameObject, Predicate<IEntity> freeObjectPredicate) {
		DestroyOutOfRangeObjects(freeObjectPredicate);
		DestroyObject(entity, destroyAttachedGameObject);
	}

	public void DestroyObject(IEntity entity, bool destroyAttachedGameObject) {
		if (maxPoolSize == 0) {
			if (destroyAttachedGameObject) {
				ref GameObjectComponent gameObjectComponent = ref entity.GetComponent<GameObjectComponent>();
				Destroy(gameObjectComponent.Self);
			}
			Default_World.RemoveEntity(entity);
			return;
		}

		if (poolSize <= maxPoolSize) {
			ref GameObjectComponent gameObjectComponent = ref entity.GetComponent<GameObjectComponent>();
			gameObjectComponent.InUsage = false;
			if (destroyAttachedGameObject) {
				gameObjectComponent.Self.SetActive(false);
			}
		} else {
			pool.RemoveAt(FindPoolIDByEntityID(entity.ID));
			if (destroyAttachedGameObject) {
				ref GameObjectComponent gameObjectComponent = ref entity.GetComponent<GameObjectComponent>();
				Destroy(gameObjectComponent.Self);
			}
			Default_World.RemoveEntity(entity);
			poolSize--;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Obsolete("Use it if you want straight control of your pool")]
	public void FreeObject(IEntity entity, bool gameObjectActiveSelf, Predicate<IEntity> freeObjectPredicate) {
		DestroyOutOfRangeObjects(freeObjectPredicate);
		FreeObject(entity, gameObjectActiveSelf);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void FreeObject(IEntity entity, bool gameObjectActiveSelf) {
		ref GameObjectComponent gameObjectComponent = ref entity.GetComponent<GameObjectComponent>();
		gameObjectComponent.InUsage = false;
		gameObjectComponent.Self.SetActive(gameObjectActiveSelf);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Obsolete("Use it if you want straight control of your pool")]
	public void FreePool(bool gameObjectActiveSelf, Predicate<IEntity> freeObjectPredicate) {
		DestroyOutOfRangeObjects(freeObjectPredicate);
		FreePool(gameObjectActiveSelf);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void FreePool(bool gameObjectActiveSelf) {
		for (int i = 0; i < poolSize; i++) {
			FreeObject(pool[i], gameObjectActiveSelf);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void PrePool() => PrePool(prePoolCount, defaultPrefab, unsortedPoolParent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void PrePool(int count) => PrePool(count, defaultPrefab, unsortedPoolParent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void PrePool(int count, Transform parent) => PrePool(count, defaultPrefab, parent);

	public void PrePool(int count, GameObject prefab, Transform parent) {
		int normalizedCount = Math.Max(Math.Min(count, maxPoolSize - poolSize), 0);
		for (int i = 0; i < normalizedCount; i++) {
			ref GameObjectComponent gameObjectComponent = ref CreateStandardEntity(prefab, Vector_Zero, Quaternion_Identity, parent, false, false);
			pool.Add(gameObjectComponent.SelfEntity);
		}
		poolSize = poolSize + normalizedCount;
	}

	public void ClearPool(bool onlyNotInUsage, bool destroyGameObjects) {
		if (!onlyNotInUsage && !destroyGameObjects) {
			pool.Clear();
			poolSize = 0;
		} else {
			for (int i = 0; i < poolSize; i++) {
				IEntity entity = pool[i];
				if (onlyNotInUsage) {
					ref GameObjectComponent gameObjectComponent = ref entity.GetComponent<GameObjectComponent>();
					if (!gameObjectComponent.InUsage) {
						pool.RemoveAt(i);
						poolSize--;
						if (destroyGameObjects) {
							Destroy(gameObjectComponent.Self);
						}
						Default_World.RemoveEntity(entity);
					}
				} else {
					pool.RemoveAt(i);
					poolSize--;
					if (destroyGameObjects) {
						ref GameObjectComponent gameObjectComponent = ref entity.GetComponent<GameObjectComponent>();
						Destroy(gameObjectComponent.Self);
					}
					Default_World.RemoveEntity(entity);
				}
			}
		}
	}

	private void DestroyOutOfRangeObjects(Predicate<IEntity> freeObjectPredicate) {
		int index = pool.FindIndex(freeObjectPredicate);
		while (index != -1 && poolSize > maxPoolSize) {
			IEntity entity = pool[index];
			GameObject go = entity.GetComponent<GameObjectComponent>().Self;
			pool.RemoveAt(index);
			Default_World.RemoveEntity(entity);
			Destroy(go);
			poolSize--;
			index = pool.FindIndex(freeObjectPredicate);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private ref GameObjectComponent CreateStandardEntity(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, bool inUsage, bool gameObjectActiveSelf) {
		IEntity entity = Default_World.CreateEntity();
		ref GameObjectComponent gameObjectComponent = ref entity.AddComponent<GameObjectComponent>();
		gameObjectComponent.Self = Instantiate(prefab, position, rotation, parent);
		gameObjectComponent.SelfTransform = gameObjectComponent.Self.transform;
		gameObjectComponent.Self.SetActive(gameObjectActiveSelf);
		gameObjectComponent.InUsage = inUsage;

		gameObjectComponent.SelfEntity = entity;
		gameObjectComponent.Self.GetComponent<IEntityProvider>().Entity = entity;
		return ref gameObjectComponent;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int FindPoolIDByEntityID(int entityID) {
		for (int i = 0; i < poolSize; i++) {
			if (pool[i].ID == entityID)
				return i;
		}

		return -1;
	}

}
