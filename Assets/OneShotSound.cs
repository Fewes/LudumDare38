using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneShotSound : MonoBehaviour
{
	[SerializeField] AudioClip[] sounds;

	void OnEnable ()
	{
		if (sounds.Length > 0 && Time.time > 1) // Silly fix for pooled objects being enabled at the start of the game
			AudioSource.PlayClipAtPoint(sounds[Random.Range(0, sounds.Length)], transform.position);
	}
}
