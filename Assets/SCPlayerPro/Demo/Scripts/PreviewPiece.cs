using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreviewPiece : MonoBehaviour
{
    public RawImage rimg;
	public Text text;
	public string url;
    public void SetTexture(string url, Texture2D texture)
	{
		rimg.texture = texture;
		text.text = url;
		this.url = url;
	}
}
