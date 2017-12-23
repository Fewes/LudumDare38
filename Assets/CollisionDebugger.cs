using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDebugger : MonoBehaviour
{
	public bool		visualizePhysics;
	// Use this for initialization
	void Start ()
	{
		foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
		{
			renderer.enabled = false;
		}
	}
	
	void OnValidate ()
	{
		foreach (var renderer in GetComponentsInChildren<SpriteRenderer>())
		{
			renderer.enabled = visualizePhysics;
		}
	}
}
