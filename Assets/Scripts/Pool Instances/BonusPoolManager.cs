using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BonusPoolManager : EntityPool
{

	public static BonusPoolManager Instance;

	protected override void Awake() {
		base.Awake();
		if (Instance == null) {
			Instance = this;
		}

		Statics.Pools[(int)Statics.KnownPools.BONUS] = Instance;
	}

}
