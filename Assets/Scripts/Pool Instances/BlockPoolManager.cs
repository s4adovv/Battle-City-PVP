using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockPoolManager : EntityPool
{

	public static BlockPoolManager Instance;

	protected override void Awake() {
		base.Awake();
		if (Instance == null) {
			Instance = this;
		}

		Statics.Pools[(int)Statics.KnownPools.BLOCK] = Instance;
	}

}
