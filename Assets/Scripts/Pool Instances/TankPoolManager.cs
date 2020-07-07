using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankPoolManager : PoolManager
{

	public static TankPoolManager Instance;

	private void Awake() {
		if (Instance == null) {
			Instance = this;
		}

		Statics.Pools[(int)Statics.KnownPools.TANK] = Instance;
	}

}
