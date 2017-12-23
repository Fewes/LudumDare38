using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldText : MonoBehaviour
{
	public float		lifeTime = 1f;
	public float		riseSpeed = 0.5f;
	public Gradient		colorOverTime;

	private TextMesh	text;
	private float		timer;

	void Start ()
	{
		CheckComponents();
	}

	void CheckComponents ()
	{
		if (!text) text = GetComponent<TextMesh>();
	}
	
	void Update ()
	{
		timer += Time.deltaTime;

		if (timer >= lifeTime)
			Destroy(gameObject);

		transform.position += Vector3.up * riseSpeed * Time.deltaTime;

		text.color = colorOverTime.Evaluate(timer / lifeTime);
	}

	public void SetText (string str)
	{
		CheckComponents();

		text.text = str;
	}

	public void SetColor (Gradient gradient)
	{
		CheckComponents();

		colorOverTime = gradient;
	}
}
