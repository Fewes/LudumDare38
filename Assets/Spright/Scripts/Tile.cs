using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Tile : MonoBehaviour
{
	[System.Serializable]
	public enum ShadowChannel
	{
		R,
		G,
		B,
		A
	}

	public int						colorID;
	[Range(0, 1)]
	public float					rimIntensity = 0;
	//public bool						rimLit;
	public bool						castShadows = false;
	public Color					overrideColor = new Color(1, 1, 1, 0);
	[Range(0, 1)]
	public float					shadowLength = 0.5f;
	public ShadowChannel			shadowChannel = ShadowChannel.R;
	public bool						animated = false;
	public bool						debug = false;

	private SpriteRenderer			m_Renderer;
	private MaterialPropertyBlock	m_PropertyBlock;

	private Color					flashColor;
	private float					flashDuration;
	private float					flashTimer;
	private bool					flashReset;

	void Start ()
	{
		CheckRenderer();
	}

	public void Flash (Color color, float duration, float delay = 0)
	{
		StartCoroutine(FlashDelayed(color, duration, delay));
	}

	IEnumerator FlashDelayed (Color color, float duration, float delay = 0)
	{
		yield return new WaitForSeconds(delay);

		flashColor = color;
		flashDuration = duration;
		flashTimer = flashDuration;
		flashReset = false;
	}

	void OnValidate ()
	{
		var tileManager = FindObjectOfType<TileManager>();
		if (tileManager)
			SetColor(tileManager.colorID[colorID]);

		UpdatePropertyBlock();
	}
	
	public void SetColor (Color col)
	{
		CheckRenderer();
		m_Renderer.color = col;
	}

	public void UpdatePropertyBlock ()
	{
		if (!Application.isPlaying)
			CheckRenderer();

		// Get current property block (prevents overwriting properties set by the Sprite Renderer)
		m_Renderer.GetPropertyBlock(m_PropertyBlock);

		m_PropertyBlock.SetVector ("_ColorO", new Vector4(
			overrideColor.r,
			overrideColor.g,
			overrideColor.b,
			overrideColor.a
			));

		m_PropertyBlock.SetFloat ("_RimLit", rimIntensity);
		m_PropertyBlock.SetFloat ("_Shadows", castShadows ? 1.0f : 0.0f);
		m_PropertyBlock.SetFloat ("_ShadowLength", shadowLength);
		m_PropertyBlock.SetVector ("_ShadowChannel", new Vector4(
			shadowChannel == ShadowChannel.R ? 1 : 0,
			shadowChannel == ShadowChannel.G ? 1 : 0,
			shadowChannel == ShadowChannel.B ? 1 : 0,
			shadowChannel == ShadowChannel.A ? 1 : 0
			));
		//m_PropertyBlock.SetColor ("_RendererColor", m_Renderer.color);		// Already set by SpriteRenderer
		//m_PropertyBlock.SetTexture ("_MainTex", m_Renderer.sprite.texture);	// Already set by SpriteRenderer
		m_PropertyBlock.SetVector ("_FlipUV", new Vector4(m_Renderer.flipX ? -1 : 1, m_Renderer.flipY ? -1 : 1, 1, 1));

		// Calculate texel size (XY)
		Vector4 texelSize = new Vector4();
		texelSize.x = 1f / (float)m_Renderer.sprite.texture.width * m_Renderer.sprite.pixelsPerUnit / transform.lossyScale.x;
		texelSize.y = 1f / (float)m_Renderer.sprite.texture.height * m_Renderer.sprite.pixelsPerUnit / transform.lossyScale.y;
		m_PropertyBlock.SetVector ("_TexelSize", texelSize);

		// Set sprite bounds
		Vector2 size = new Vector2(m_Renderer.sprite.texture.width, m_Renderer.sprite.texture.height);
		m_PropertyBlock.SetVector ("_SpriteRect", new Vector4(
			m_Renderer.sprite.rect.xMin / size.x, m_Renderer.sprite.rect.xMax / size.x,
			m_Renderer.sprite.rect.yMin / size.y, m_Renderer.sprite.rect.yMax / size.y));

		// Set the property block
		m_Renderer.SetPropertyBlock (m_PropertyBlock);
	}

	void CheckRenderer ()
	{
		if (!m_Renderer)
			m_Renderer = GetComponent<SpriteRenderer>();
		if (m_PropertyBlock == null)
			m_PropertyBlock = new MaterialPropertyBlock();
	}

	void Update ()
	{
		flashTimer -= Time.deltaTime;

		if (flashTimer > 0)
		{
			overrideColor = Color.Lerp(new Color(flashColor.r, flashColor.g, flashColor.b, 0), flashColor, flashTimer / flashDuration);
		}
		else if (!flashReset)
		{
			overrideColor = new Color(1, 1, 1, 0);
			flashReset = true;
		}
	}

	void LateUpdate ()
	{
		// Update the property block every frame if the sprite is animated, or if the scene is updated in the editor
		if (animated || !Application.isPlaying)
		{
			if (!Application.isPlaying)
				CheckRenderer();
			UpdatePropertyBlock();
		}
	}
}
