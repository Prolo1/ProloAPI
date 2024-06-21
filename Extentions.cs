using System;
using System.Collections.Generic;
using System.Text;

namespace ProloAPI
{

	namespace Extentions
	{
		public static class Util
		{
			internal static ManualLogSource Logger { get; private set; }

			static Texture2D _greyTex = null;
			public static Texture2D greyTex
			{
				get
				{
					if(_greyTex != null) return _greyTex;

					_greyTex = new Texture2D(1, 1);
					var pixels = _greyTex.GetPixels();
					for(int a = 0; a < pixels.Length; ++a)
						pixels[a] = Color.black;
					_greyTex.SetPixels(pixels);
					_greyTex.Apply();

					return _greyTex;
				}
			}
			public static void Init(ManualLogSource logger) { Logger = logger; }

			public static class General
			{
			}
			public static class Game
			{
			}
			public static class GUI
			{
			}
		}
	}
}
