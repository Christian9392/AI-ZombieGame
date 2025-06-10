using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class DrawGUI : MonoBehaviour
{
	public Sprite HeartSprite;
	public Sprite FlySprite;

	private int _iconSize = 20;
	private int _iconSeparation = 10;
	
	private Texture2D _heartTex;
	private Texture2D _flyTex;
	private Frog _frog;

	void Start()
	{
		_heartTex = SpriteToTexture(HeartSprite);
		_flyTex = SpriteToTexture(FlySprite);
		_frog = GameObject.Find("Frog").GetComponent<Frog>();
	}

	void OnGUI()
	{
		if (_frog == null || _heartTex == null || _flyTex == null)
		return;

		int _maxFiles = 9;

		GUI.Box(new Rect(10, 10, 30 * _maxFiles + 10, 60), "");

        // At the moment, the GUI is hardcoded to 3 health.
        // The counts are wrong, and don't change as the frog takes damage.
        for (int i = 0; i < _frog.Health; i++)
		{
			GUI.DrawTexture(new Rect(20 + (_iconSize + _iconSeparation) * i, 20, _iconSize, _iconSize), _heartTex, ScaleMode.ScaleToFit, true, 0.0f);
		}
		for (int i = 0; i < Mathf.Min(_frog.flies, 9); i++)
		{
			GUI.DrawTexture(new Rect(20 + (_iconSize + _iconSeparation) * i, 45, _iconSize, _iconSize), _flyTex, ScaleMode.ScaleToFit, true, 0.0f);
		}

		
	}

	// Helper function to convert sprites to textures.
	// Follows the code from http://answers.unity3d.com/questions/651984/convert-sprite-image-to-texture.html
	private Texture2D SpriteToTexture(Sprite sprite)
	{
		if (sprite.rect.width != sprite.texture.width)
		{
			Texture2D texture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
			Color[] pixels = sprite.texture.GetPixels((int)sprite.textureRect.x, (int)sprite.textureRect.y, (int)sprite.textureRect.width, (int)sprite.textureRect.height);
			texture.SetPixels(pixels);
			texture.Apply();

			return texture;
		}
		else
		{
			return sprite.texture;
		}
	}
}
