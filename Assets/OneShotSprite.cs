using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneShotSprite : MonoBehaviour
{
	[HideInInspector] public Animator			animator;
	[HideInInspector] public SpriteRenderer		spriteRenderer;
	[HideInInspector] public Vector3			velocity;

	void Start ()
	{
		CheckComponents();
	}

	void CheckComponents ()
	{
		if (!animator) animator = GetComponent<Animator>();
		if (!spriteRenderer) spriteRenderer = GetComponent<SpriteRenderer>();
	}
	
	void OnEnable ()
	{
		CheckComponents();

		velocity = Vector3.zero;

		animator.SetTrigger("Play");
	}

	public void AnimationFinished ()
	{
		gameObject.SetActive(false);
	}

	void Update ()
	{
		transform.position += velocity * Time.deltaTime;
	}
}
