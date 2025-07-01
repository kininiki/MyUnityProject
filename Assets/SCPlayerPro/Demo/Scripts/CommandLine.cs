using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandLine : MonoBehaviour
{
	private void Awake()
	{
		bool setting = false;
		bool fullscreen = false;
		int w = 1280, h = 720;
		string[] args = System.Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i] == "--fullscreen")
			{
				setting = true;
				fullscreen = true;
			}
			else if (args[i].StartsWith("--vp:"))
			{
				setting = true;
				string[] s = args[i].Split(":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
				if (s.Length >= 2)
				{
					string[] v = s[1].Split("x".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
					if (v.Length >= 2)
					{
						int.TryParse(v[0], out w);
						int.TryParse(v[1], out h);
					}
				}
			}
		}
		if(setting)
			Screen.SetResolution(w, h, fullscreen);
	}
}
