using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
	public Color[] colorID;

	void Start ()
	{
		Application.targetFrameRate = 60;
	}

	void OnValidate ()
	{
		if (colorID.Length > 0)
		{
			foreach (var tile in FindObjectsOfType<Tile>())
			{
				tile.SetColor(colorID[tile.colorID]);
			}
		}
		var tiles = FindObjectsOfType<Tile>();
		foreach (var tile in tiles)
			tile.UpdatePropertyBlock();
	}
}
