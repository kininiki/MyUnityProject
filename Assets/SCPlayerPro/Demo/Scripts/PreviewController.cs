using Sttplay.MediaPlayer;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class PreviewController : MonoBehaviour
{
    public string url;
	public Transform parent;
    public GameObject prefab;
	public ScrollRect scrollRect;
	// Start is called before the first frame update
	void Start()
    {
        //url = SCMGR.GetUrlFromSCSCAssets("");

		SCMGR.InitSCPlugins();
		StartCoroutine(LoopRefresh());
	}

	private IEnumerator PreviewImage()
	{
		int childCount = parent.transform.childCount;
		for (int i = childCount - 1; i >= 0; i--)
		{
			Transform child = parent.transform.GetChild(i);
			Destroy(child.gameObject);
		}

		System.IO.DirectoryInfo dfo = new System.IO.DirectoryInfo(url);
        var infos = dfo.GetFiles();
		for (int i = 0; i < infos.Length; i++)
		{
            var semPtr = ISCNative.BeginPreviewContext(infos[i].FullName, true);
			Debug.Log(infos[i].FullName);
			List<Texture2D> texs = new List<Texture2D>();
			long ms = 0;
			ISCNative.SetPreviewTargetTimestamp(ms);
			while (true)
			{
				PreviewSem previewSem = Marshal.PtrToStructure<PreviewSem>(semPtr);
				if(ISCNative.SCSemaphore_TryWait(previewSem.interruptSem) != 0)
				{
					yield return new WaitForEndOfFrame();
					continue;
				}
				previewSem = Marshal.PtrToStructure<PreviewSem>(semPtr);
				CaptureOpenResult ret = (CaptureOpenResult)previewSem.openState;
				if (ret != CaptureOpenResult.SUCCESS)
					break;
				if (previewSem.data == System.IntPtr.Zero)
					break;
				Texture2D tex = new Texture2D(previewSem.width, previewSem.height, TextureFormat.RGBA32, false, false);
				tex.LoadRawTextureData(previewSem.data, previewSem.width * previewSem.height * 4);
				tex.Apply();
				texs.Add(tex);
				if (previewSem.brightness <= 0.015)
				{
					ms += 1000;
					ISCNative.SetPreviewTargetTimestamp(ms);
					continue;
				}
				break;
			}
			ISCNative.EndPreviewContext();
			if(texs.Count > 0)
			{
				var piece = Instantiate(prefab, parent).GetComponent<PreviewPiece>();
				piece.SetTexture(infos[i].Name, texs[texs.Count - 1]);
				scrollRect.verticalNormalizedPosition = 0;
			}
			SCMGR.GCCollect();
		}
		Debug.Log("Get preview image completed");
	}

	private IEnumerator LoopRefresh()
	{
		while(true)
		{
			yield return PreviewImage();
			yield return new WaitForSeconds(5);
		}
	}
}
