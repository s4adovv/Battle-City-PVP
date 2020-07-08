using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankPoolManager : EntityPool
{

	public static TankPoolManager Instance;

	protected override void Awake() {
		base.Awake();
		if (Instance == null) {
			Instance = this;
		}

		Statics.Pools[(int)Statics.KnownPools.TANK] = Instance;
	}

}
