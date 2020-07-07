using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BonusPoolManager : PoolManager
{

	public static BonusPoolManager Instance;

	private void Awake() {
		if (Instance == null) {
			Instance = this;
		}

		Statics.Pools[(int)Statics.KnownPools.BONUS] = Instance;
		PrePool();
	}

}
