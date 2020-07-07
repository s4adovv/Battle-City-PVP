using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Statics;

public class AudioManager : MonoBehaviour
{

   public static AudioManager Instance;

	[SerializeField] private AudioClip[] sounds;
	private AudioSource audioSource;

	private void Awake() {
		if (Instance == null) {
			Instance = this;
		}

		audioSource = this.GetComponent<AudioSource>();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void RequestSound(GameSounds sound) => audioSource.PlayOneShot(sounds[(int)sound]);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void RequestInfiniteSound(GameSounds sound) {
		audioSource.Stop();
		audioSource.clip = sounds[(int)sound];
		audioSource.Play();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)] public void StopSounds() => audioSource.Stop();

}
