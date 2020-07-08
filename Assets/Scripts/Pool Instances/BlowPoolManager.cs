using System.Collections;
using System.Collections.Generic;
using Morpeh;
using UnityEngine;
using static Statics;

public class BlowPoolManager : PoolManager
{

	private const string BIG_BLOW_TRIGGER = "Big Blow";
	private const string SMALL_BLOW_TRIGGER = "Small Blow";

	public static BlowPoolManager Instance;

	private Transform selfTransform;

	private void Awake() {
		if (Instance == null) {
			Instance = this;
		}

		Pools[(int)KnownPools.BLOW] = Instance;
		selfTransform = transform;
	}

	public void RequestBlow(bool isSmall, Vector3 atPosition) {
		ref GameObjectComponent gameObjectComponent = ref EnsureObject(atPosition, selfTransform);
		ref AnimatorComponent animatorComponent = ref NotHasGet<AnimatorComponent>(gameObjectComponent.SelfEntity);
		if (animatorComponent.SelfAnimator == null) {
			animatorComponent.SelfAnimator = gameObjectComponent.Self.GetComponent<Animator>();
		}
		StartCoroutine("AnimationRoutine", (gameObjectComponent.SelfEntity, animatorComponent.SelfAnimator, isSmall));
	}

	private IEnumerator AnimationRoutine((IEntity entity, Animator animator, bool isSmall) data) {
		data.animator.SetTrigger(data.isSmall ? SMALL_BLOW_TRIGGER : BIG_BLOW_TRIGGER);
		yield return new WaitForSeconds(data.animator.GetCurrentAnimatorStateInfo(0).length);
		base.FreeObject(data.entity, false);
		yield break;
	}

}
