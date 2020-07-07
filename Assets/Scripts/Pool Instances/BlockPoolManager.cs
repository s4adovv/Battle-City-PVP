using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockPoolManager : PoolManager
{

	public static BlockPoolManager Instance;

	private void Awake() {
		if (Instance == null) {
			Instance = this;
		}

		Statics.Pools[(int)Statics.KnownPools.BLOCK] = Instance;
	}

}
