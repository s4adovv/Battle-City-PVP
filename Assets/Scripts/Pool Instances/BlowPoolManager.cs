using System.Collections;
using System.Collections.Generic;
using Morpeh;
using UnityEngine;
using static Statics;

public class BlowPoolManager : EntityPool
{

	private const string BIG_BLOW_TRIGGER = "Big Blow";
	private const string SMALL_BLOW_TRIGGER = "Small Blow";

	public static BlowPoolManager Instance;

	private Transform selfTransform;

	protected override void Awake() {
		base.Awake();
		if (Instance == null) {
			Instance = this;
		}

		Pools[(int)KnownPools.BLOW] = Instance;
		selfTransform = transform;
	}

	public void RequestBlow(bool isSmall, Vector3 atPosition) {
		IEntity blowEntity = Get(atPosition, selfTransform);
		ref GameObjectComponent blowGameObjectComponent = ref blowEntity.GetComponent<GameObjectComponent>();
		ref AnimatorComponent blowAnimatorComponent = ref NotHasGet<AnimatorComponent>(blowEntity);
		if (blowAnimatorComponent.SelfAnimator == null) {
			blowAnimatorComponent.SelfAnimator = blowGameObjectComponent.Self.GetComponent<Animator>();
		}
		StartCoroutine("AnimationRoutine", (blowEntity, blowAnimatorComponent.SelfAnimator, isSmall));
	}

	private IEnumerator AnimationRoutine((IEntity entity, Animator animator, bool isSmall) data) {
		data.animator.SetTrigger(data.isSmall ? SMALL_BLOW_TRIGGER : BIG_BLOW_TRIGGER);
		yield return new WaitForSeconds(data.animator.GetCurrentAnimatorStateInfo(0).length);
		base.FreeObject(data.entity, false);
		yield break;
	}

}
