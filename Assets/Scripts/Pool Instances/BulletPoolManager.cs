﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletPoolManager : EntityPool
{

	public static BulletPoolManager Instance;

	protected override void Awake() {
		base.Awake();
		if (Instance == null) {
			Instance = this;
		}

		Statics.Pools[(int)Statics.KnownPools.BULLET] = Instance;
	}

}
