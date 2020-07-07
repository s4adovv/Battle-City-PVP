using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletPoolManager : PoolManager
{

	public static BulletPoolManager Instance;

	private void Awake() {
		if (Instance == null) {
			Instance = this;
		}

		Statics.Pools[(int)Statics.KnownPools.BULLET] = Instance;
		PrePool();
	}

}
