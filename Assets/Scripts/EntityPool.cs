using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Morpeh;
using UnityEngine;
using static Statics;

public unsafe class EntityPool : MonoBehaviour, IPool<IEntity>
{

	private const int DATA_USAGE_SHIFT = 6;
	private const int DATA_USAGE_INCREASER = 8;
	private const int ULONG_TYPE_BIT_SIZE = sizeof(ulong) * 8; // 8 bits

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
	private List<IEntity> pool;

	private ulong* inUsageData;
	private int inUsageDataSize;

	protected virtual void Awake() {
		pool = new List<IEntity>(maxPoolSize);
		inUsageDataSize = DATA_USAGE_INCREASER;
		inUsageData = (ulong*)Marshal.AllocHGlobal(inUsageDataSize * sizeof(ulong));
		PrePool(prePoolCount, defaultPrefab, unsortedPoolParent);
	}

	private void OnDestroy() {
		Marshal.FreeHGlobal((IntPtr)inUsageData);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public IEntity EnsureObject() => EnsureObject(defaultPrefab, Vector_Zero, Quaternion_Identity, unsortedPoolParent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public IEntity EnsureObject(GameObject prefab) => EnsureObject(prefab, Vector_Zero, Quaternion_Identity, unsortedPoolParent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public IEntity EnsureObject(Vector3 position) => EnsureObject(defaultPrefab, position, Quaternion_Identity, unsortedPoolParent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public IEntity EnsureObject(Quaternion rotation) => EnsureObject(defaultPrefab, Vector_Zero, rotation, unsortedPoolParent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public IEntity EnsureObject(Transform parent) => EnsureObject(defaultPrefab, Vector_Zero, Quaternion_Identity, parent);

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public IEntity EnsureObject(Vector3 position, Quaternion rotation) => EnsureObject(defaultPrefab, position, rotation, unsortedPoolParent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public IEntity EnsureObject(Vector3 position, Transform parent) => EnsureObject(defaultPrefab, position, Quaternion_Identity, parent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public IEntity EnsureObject(Quaternion rotation, Transform parent) => EnsureObject(defaultPrefab, Vector_Zero, rotation, parent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public IEntity EnsureObject(GameObject prefab, Vector3 position) => EnsureObject(prefab, position, Quaternion_Identity, unsortedPoolParent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public IEntity EnsureObject(GameObject prefab, Quaternion rotation) => EnsureObject(prefab, Vector_Zero, rotation, unsortedPoolParent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public IEntity EnsureObject(GameObject prefab, Transform parent) => EnsureObject(prefab, Vector_Zero, Quaternion_Identity, parent);


	[MethodImpl(MethodImplOptions.AggressiveInlining)] public IEntity EnsureObject(Vector3 position, Quaternion rotation, Transform parent) => EnsureObject(defaultPrefab, position, rotation, parent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public IEntity EnsureObject(GameObject prefab, Vector3 position, Quaternion rotation) => EnsureObject(prefab, position, rotation, unsortedPoolParent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public IEntity EnsureObject(GameObject prefab, Vector3 position, Transform parent) => EnsureObject(prefab, position, Quaternion_Identity, parent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public IEntity EnsureObject(GameObject prefab, Quaternion rotation, Transform parent) => EnsureObject(prefab, Vector_Zero, rotation, parent);

	/// <summary>
	/// Get IEntity object with specified GameObject prefab.
	/// </summary>
	/// <param name="prefab">GameObject to instantiate.</param>
	/// <param name="position">Start position.</param>
	/// <param name="rotation">Start rotation.</param>
	/// <param name="parent">The parent to attach an object to.</param>
	/// <param name="freeObjectPredicate">A Predicate rule for finding free objects in the pool.</param>
	public IEntity EnsureObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent) {
		if (maxPoolSize == 0)
			return CreateStandardEntity(prefab, position, rotation, parent, true);

		if (poolSize > maxPoolSize) {
			DestroyOutOfRangeObjects();
		}
		int index = FindFreeObjectInPool();

		if (index != -1) {
			IEntity entity = pool[index];
			ref GameObjectComponent gameObjectComponent = ref entity.GetComponent<GameObjectComponent>();
			gameObjectComponent.SelfTransform.position = position;
			gameObjectComponent.SelfTransform.rotation = rotation;
			gameObjectComponent.SelfTransform.parent = parent;
			gameObjectComponent.Self.SetActive(true);
			SetUsage(index, true);

			return entity;
		} else {
			IEntity entity = CreateStandardEntity(prefab, position, rotation, parent, true);
			pool.Add(entity);
			SetUsage(poolSize, true);
			poolSize++;

			return entity;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void DestroyObject(IEntity entity) => DestroyObject(entity, true);

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
			SetUsage(FindPoolIDByEntityID(entity.ID), false);
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
	public void FreeObject(IEntity entity, bool gameObjectActiveSelf) {
		ref GameObjectComponent gameObjectComponent = ref entity.GetComponent<GameObjectComponent>();
		gameObjectComponent.Self.SetActive(gameObjectActiveSelf);
		SetUsage(FindPoolIDByEntityID(entity.ID), false);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void FreePool(bool gameObjectActiveSelf) {
		for (int i = 0; i < poolSize; i++) {
			ref GameObjectComponent gameObjectComponent = ref pool[i].GetComponent<GameObjectComponent>();
			gameObjectComponent.Self.SetActive(gameObjectActiveSelf);
			SetUsage(i, false);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void PrePool(int count) => PrePool(count, defaultPrefab, unsortedPoolParent);
	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void PrePool(int count, Transform parent) => PrePool(count, defaultPrefab, parent);

	public void PrePool(int count, GameObject prefab, Transform parent) {
		int normalizedCount = Math.Max(Math.Min(count, maxPoolSize - poolSize), 0);
		for (int i = 0; i < normalizedCount; i++) {
			pool.Add(CreateStandardEntity(prefab, Vector_Zero, Quaternion_Identity, parent, false));
			SetUsage(poolSize++, false);
		}
	}

	public void ClearPool(bool onlyNotInUsage, bool destroyGameObjects) {
		if (!onlyNotInUsage && !destroyGameObjects) {
			pool.Clear();
			poolSize = 0;
		} else {
			for (int i = 0; i < poolSize; i++) {
				IEntity entity = pool[i];
				if (onlyNotInUsage) {
					if (!GetUsage(i)) {
						pool.RemoveAt(i);
						poolSize--;
						if (destroyGameObjects) {
							ref GameObjectComponent gameObjectComponent = ref entity.GetComponent<GameObjectComponent>();
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

	public void DestroyOutOfRangeObjects() {
		int index = FindFreeObjectInPool();
		while (index != -1 && poolSize > maxPoolSize) {
			IEntity entity = pool[index];
			GameObject go = entity.GetComponent<GameObjectComponent>().Self;
			pool.RemoveAt(index);
			Default_World.RemoveEntity(entity);
			Destroy(go);
			poolSize--;
			index = FindFreeObjectInPool();
		}
	}

	public void DestroyOutOfRangeObjects(Predicate<IEntity> freeObjectPredicate) {
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
	private IEntity CreateStandardEntity(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, bool gameObjectActiveSelf) {
		IEntity entity = Default_World.CreateEntity();
		ref GameObjectComponent gameObjectComponent = ref entity.AddComponent<GameObjectComponent>();
		gameObjectComponent.Self = Instantiate(prefab, position, rotation, parent);
		gameObjectComponent.SelfTransform = gameObjectComponent.Self.transform;
		gameObjectComponent.Self.SetActive(gameObjectActiveSelf);

		gameObjectComponent.SelfEntity = entity;
		gameObjectComponent.Self.GetComponent<IEntityProvider>().Entity = entity;
		return entity;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int FindPoolIDByEntityID(int entityID) {
		for (int i = 0; i < poolSize; i++) {
			if (pool[i].ID == entityID)
				return i;
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetUsage(int index, bool inUsage) {
		if (index >= (inUsageDataSize << DATA_USAGE_SHIFT)) {
			inUsageDataSize += DATA_USAGE_INCREASER;
			inUsageData = (ulong*)Marshal.ReAllocHGlobal((IntPtr)inUsageData, (IntPtr)(inUsageDataSize * sizeof(ulong)));
		}

		int subIndex = index % ULONG_TYPE_BIT_SIZE;
		index /= ULONG_TYPE_BIT_SIZE;

		if (inUsage) {
			inUsageData[index] &= ~(0b1ul << subIndex);
		} else {
			inUsageData[index] |= 0b1ul << subIndex;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool GetUsage(int index) {
		int subIndex = index % ULONG_TYPE_BIT_SIZE;
		index /= ULONG_TYPE_BIT_SIZE;

		return (inUsageData[index] & (0b1ul << subIndex)) == 0;
	}

	private int FindFreeObjectInPool() {
		int tempIndex = 0;
		for (int i = 0; i < inUsageDataSize; i++) {
			ulong tempDataBlock = inUsageData[i];
			if (tempDataBlock != ulong.MaxValue) {
				if ((tempDataBlock & (0xff_ff_ff_ff_00_00_00_00)) != 0) {
					if ((tempDataBlock & (0xff_ff_00_00_00_00_00_00)) != 0) {
						if ((tempDataBlock & (0xff_00_00_00_00_00_00_00)) != 0) {
							if ((tempDataBlock & (0xf0_00_00_00_00_00_00_00)) != 0) {
								if ((tempDataBlock & (0xc0_00_00_00_00_00_00_00)) != 0) {
									if ((tempDataBlock & (0x80_00_00_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 63;
									} else if ((tempDataBlock & (0x40_00_00_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 62;
									}
								} else if ((tempDataBlock & (0x30_00_00_00_00_00_00_00)) != 0) {
									if ((tempDataBlock & (0x20_00_00_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 61;
									} else if ((tempDataBlock & (0x10_00_00_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 60;
									}
								}
							} else if ((tempDataBlock & (0x0f_00_00_00_00_00_00_00)) != 0) {
								if ((tempDataBlock & (0x0c_00_00_00_00_00_00_00)) != 0) {
									if ((tempDataBlock & (0x08_00_00_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 59;
									} else if ((tempDataBlock & (0x04_00_00_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 58;
									}
								} else if ((tempDataBlock & (0x03_00_00_00_00_00_00_00)) != 0) {
									if ((tempDataBlock & (0x02_00_00_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 57;
									} else if ((tempDataBlock & (0x01_00_00_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 56;
									}
								}
							}
						} else if ((tempDataBlock & (0x00_ff_00_00_00_00_00_00)) != 0) {
							if ((tempDataBlock & (0x00_f0_00_00_00_00_00_00)) != 0) {
								if ((tempDataBlock & (0x00_c0_00_00_00_00_00_00)) != 0) {
									if ((tempDataBlock & (0x00_80_00_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 55;
									} else if ((tempDataBlock & (0x00_40_00_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 54;
									}
								} else if ((tempDataBlock & (0x00_30_00_00_00_00_00_00)) != 0) {
									if ((tempDataBlock & (0x00_20_00_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 53;
									} else if ((tempDataBlock & (0x00_10_00_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 52;
									}
								}
							} else if ((tempDataBlock & (0x00_0f_00_00_00_00_00_00)) != 0) {
								if ((tempDataBlock & (0x00_0c_00_00_00_00_00_00)) != 0) {
									if ((tempDataBlock & (0x00_08_00_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 51;
									} else if ((tempDataBlock & (0x00_04_00_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 50;
									}
								} else if ((tempDataBlock & (0x00_03_00_00_00_00_00_00)) != 0) {
									if ((tempDataBlock & (0x00_02_00_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 49;
									} else if ((tempDataBlock & (0x00_01_00_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 48;
									}
								}
							}
						}
					} else if ((tempDataBlock & (0x00_00_ff_ff_00_00_00_00)) != 0) {
						if ((tempDataBlock & (0x00_00_ff_00_00_00_00_00)) != 0) {
							if ((tempDataBlock & (0x00_00_f0_00_00_00_00_00)) != 0) {
								if ((tempDataBlock & (0x00_00_c0_00_00_00_00_00)) != 0) {
									if ((tempDataBlock & (0x00_00_80_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 47;
									} else if ((tempDataBlock & (0x00_00_40_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 46;
									}
								} else if ((tempDataBlock & (0x00_00_30_00_00_00_00_00)) != 0) {
									if ((tempDataBlock & (0x00_00_20_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 45;
									} else if ((tempDataBlock & (0x00_00_10_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 44;
									}
								}
							} else if ((tempDataBlock & (0x00_00_0f_00_00_00_00_00)) != 0) {
								if ((tempDataBlock & (0x00_00_0c_00_00_00_00_00)) != 0) {
									if ((tempDataBlock & (0x00_00_08_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 43;
									} else if ((tempDataBlock & (0x00_00_04_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 42;
									}
								} else if ((tempDataBlock & (0x00_00_03_00_00_00_00_00)) != 0) {
									if ((tempDataBlock & (0x00_00_02_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 41;
									} else if ((tempDataBlock & (0x00_00_01_00_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 40;
									}
								}
							}
						} else if ((tempDataBlock & (0x00_00_00_ff_00_00_00_00)) != 0) {
							if ((tempDataBlock & (0x00_00_00_f0_00_00_00_00)) != 0) {
								if ((tempDataBlock & (0x00_00_00_c0_00_00_00_00)) != 0) {
									if ((tempDataBlock & (0x00_00_00_80_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 39;
									} else if ((tempDataBlock & (0x00_00_00_40_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 38;
									}
								} else if ((tempDataBlock & (0x00_00_00_30_00_00_00_00)) != 0) {
									if ((tempDataBlock & (0x00_00_00_20_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 37;
									} else if ((tempDataBlock & (0x00_00_00_10_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 36;
									}
								}
							} else if ((tempDataBlock & (0x00_00_00_0f_00_00_00_00)) != 0) {
								if ((tempDataBlock & (0x00_00_00_0c_00_00_00_00)) != 0) {
									if ((tempDataBlock & (0x00_00_00_08_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 35;
									} else if ((tempDataBlock & (0x00_00_00_04_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 34;
									}
								} else if ((tempDataBlock & (0x00_00_00_03_00_00_00_00)) != 0) {
									if ((tempDataBlock & (0x00_00_00_02_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 33;
									} else if ((tempDataBlock & (0x00_00_00_01_00_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 32;
									}
								}
							}
						}
					}
				} else if ((tempDataBlock & (0x00_00_00_00_ff_ff_ff_ff)) != 0) {
					if ((tempDataBlock & (0x00_00_00_00_ff_ff_00_00)) != 0) {
						if ((tempDataBlock & (0x00_00_00_00_ff_00_00_00)) != 0) {
							if ((tempDataBlock & (0x00_00_00_00_f0_00_00_00)) != 0) {
								if ((tempDataBlock & (0x00_00_00_00_c0_00_00_00)) != 0) {
									if ((tempDataBlock & (0x00_00_00_00_80_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 31;
									} else if ((tempDataBlock & (0x00_00_00_00_40_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 30;
									}
								} else if ((tempDataBlock & (0x00_00_00_00_30_00_00_00)) != 0) {
									if ((tempDataBlock & (0x00_00_00_00_20_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 29;
									} else if ((tempDataBlock & (0x00_00_00_00_10_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 28;
									}
								}
							} else if ((tempDataBlock & (0x00_00_00_00_0f_00_00_00)) != 0) {
								if ((tempDataBlock & (0x00_00_00_00_0c_00_00_00)) != 0) {
									if ((tempDataBlock & (0x00_00_00_00_08_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 27;
									} else if ((tempDataBlock & (0x00_00_00_00_04_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 26;
									}
								} else if ((tempDataBlock & (0x00_00_00_00_03_00_00_00)) != 0) {
									if ((tempDataBlock & (0x00_00_00_00_02_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 25;
									} else if ((tempDataBlock & (0x00_00_00_00_01_00_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 24;
									}
								}
							}
						} else if ((tempDataBlock & (0x00_00_00_00_00_ff_00_00)) != 0) {
							if ((tempDataBlock & (0x00_00_00_00_00_f0_00_00)) != 0) {
								if ((tempDataBlock & (0x00_00_00_00_00_c0_00_00)) != 0) {
									if ((tempDataBlock & (0x00_00_00_00_00_80_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 23;
									} else if ((tempDataBlock & (0x00_00_00_00_00_40_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 22;
									}
								} else if ((tempDataBlock & (0x00_00_00_00_00_30_00_00)) != 0) {
									if ((tempDataBlock & (0x00_00_00_00_00_20_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 21;
									} else if ((tempDataBlock & (0x00_00_00_00_00_10_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 20;
									}
								}
							} else if ((tempDataBlock & (0x00_00_00_00_00_0f_00_00)) != 0) {
								if ((tempDataBlock & (0x00_00_00_00_00_0c_00_00)) != 0) {
									if ((tempDataBlock & (0x00_00_00_00_00_08_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 19;
									} else if ((tempDataBlock & (0x00_00_00_00_00_04_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 18;
									}
								} else if ((tempDataBlock & (0x00_00_00_00_00_03_00_00)) != 0) {
									if ((tempDataBlock & (0x00_00_00_00_00_02_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 17;
									} else if ((tempDataBlock & (0x00_00_00_00_00_01_00_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 16;
									}
								}
							}
						}
					} else if ((tempDataBlock & (0x00_00_00_00_00_00_ff_ff)) != 0) {
						if ((tempDataBlock & (0x00_00_00_00_00_00_ff_00)) != 0) {
							if ((tempDataBlock & (0x00_00_00_00_00_00_f0_00)) != 0) {
								if ((tempDataBlock & (0x00_00_00_00_00_00_c0_00)) != 0) {
									if ((tempDataBlock & (0x00_00_00_00_00_00_80_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 15;
									} else if ((tempDataBlock & (0x00_00_00_00_00_00_40_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 14;
									}
								} else if ((tempDataBlock & (0x00_00_00_00_00_00_30_00)) != 0) {
									if ((tempDataBlock & (0x00_00_00_00_00_00_20_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 13;
									} else if ((tempDataBlock & (0x00_00_00_00_00_00_10_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 12;
									}
								}
							} else if ((tempDataBlock & (0x00_00_00_00_00_00_0f_00)) != 0) {
								if ((tempDataBlock & (0x00_00_00_00_00_00_0c_00)) != 0) {
									if ((tempDataBlock & (0x00_00_00_00_00_00_08_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 11;
									} else if ((tempDataBlock & (0x00_00_00_00_00_00_04_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 10;
									}
								} else if ((tempDataBlock & (0x00_00_00_00_00_00_03_00)) != 0) {
									if ((tempDataBlock & (0x00_00_00_00_00_00_02_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 9;
									} else if ((tempDataBlock & (0x00_00_00_00_00_00_01_00)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 8;
									}
								}
							}
						} else if ((tempDataBlock & (0x00_00_00_00_00_00_00_ff)) != 0) {
							if ((tempDataBlock & (0x00_00_00_00_00_00_00_f0)) != 0) {
								if ((tempDataBlock & (0x00_00_00_00_00_00_00_c0)) != 0) {
									if ((tempDataBlock & (0x00_00_00_00_00_00_00_80)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 7;
									} else if ((tempDataBlock & (0x00_00_00_00_00_00_00_40)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 6;
									}
								} else if ((tempDataBlock & (0x00_00_00_00_00_00_00_30)) != 0) {
									if ((tempDataBlock & (0x00_00_00_00_00_00_00_20)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 5;
									} else if ((tempDataBlock & (0x00_00_00_00_00_00_00_10)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 4;
									}
								}
							} else if ((tempDataBlock & (0x00_00_00_00_00_00_00_0f)) != 0) {
								if ((tempDataBlock & (0x00_00_00_00_00_00_00_0c)) != 0) {
									if ((tempDataBlock & (0x00_00_00_00_00_00_00_08)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 3;
									} else if ((tempDataBlock & (0x00_00_00_00_00_00_00_04)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 2;
									}
								} else if ((tempDataBlock & (0x00_00_00_00_00_00_00_03)) != 0) {
									if ((tempDataBlock & (0x00_00_00_00_00_00_00_02)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE) + 1;
									} else if ((tempDataBlock & (0x00_00_00_00_00_00_00_01)) != 0) {
										tempIndex = (i * ULONG_TYPE_BIT_SIZE);
									}
								}
							}
						}
					}
				}
			}
		}

		return tempIndex >= poolSize ? -1 : tempIndex;
	}

}
