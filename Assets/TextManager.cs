using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextManager : MonoBehaviour
{
	static TextManager			manager;

	[SerializeField] GameObject	worldTextPrefab;
	[SerializeField] Gradient[]	colors;

	void Start ()
	{
		// Singleton
		if (manager && manager != this)
			Destroy(manager);
		manager = this;
	}
	
	void Update ()
	{
		
	}

	void AddWorldTextP (string str, Vector3 pos, int gradientID = 0)
	{
		var worldTextGo = Instantiate(worldTextPrefab);
		worldTextGo.transform.position = pos;
		var worldText = worldTextGo.GetComponent<WorldText>();
		if (worldText)
		{
			worldText.SetText(str);
			worldText.SetColor(colors[gradientID]);
		}
		else
		{
			Destroy(worldTextGo);
		}
	}

	public static void AddWorldText (string str, Vector3 pos, int gradientID = 0)
	{
		manager.AddWorldTextP(str, pos, gradientID);
	}
}
