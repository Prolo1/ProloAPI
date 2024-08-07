using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BepInEx.Logging;
using BepInEx;
using UnityEngine;
using KKAPI.Chara;
using BepInEx.Configuration;


namespace ProloAPI
{
	using Extentions;
	namespace Utilities
	{
		using static Util_General;

		public class Util_General
		{
			private static BaseSaveLoadManager _saveLoad = null;

			internal static ProloBaseUnityPlugin Instance = null;
			internal static IConfiguration cfg = null;
			internal static ManualLogSource Logger = null;

			public static SaveLoadManager<Tctrl, Tdata> GetSaveLoadManager<Tctrl, Tdata>() where Tdata : class
			{
				if(_saveLoad == null || !(_saveLoad is SaveLoadManager<Tctrl, Tdata>))
					_saveLoad = new SaveLoadManager<Tctrl, Tdata>();

				return (SaveLoadManager<Tctrl, Tdata>)_saveLoad;
			}

			public static Tinst GetInstance<Tinst>() where Tinst : ProloBaseUnityPlugin
			 => ProloBaseUnityPlugin.Instances as Tinst;

			public static Tcfg GetConfig<Tinst, Tcfg>() where Tinst : ProloBaseUnityPlugin where Tcfg : IConfiguration => (Tcfg)GetInstance<Tinst>().cfg;

			public static void SetApiInst<T1, T2>(ProloUnityPlugin<T1, T2> inst) where T1 : ProloBaseUnityPlugin where T2 : IConfiguration
			{
				Instance = inst;
				Logger = inst.Logger;
				cfg = inst.cfg;
			}


			private static Texture2D _greyTex = null;
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


			/// <summary>
			/// Returns a list of the registered handler specified. returns empty list otherwise 
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <returns></returns>
			public static IEnumerable<T> GetFuncCtrlOfType<T>()
			{
				foreach(var hnd in CharacterApi.RegisteredHandlers
					.Where(reg => reg.ControllerType == typeof(T)))
					return hnd.Instances.Cast<T>();

				return new T[] { };
			}


		}

		public class Util_GUI
		{
			public static Action<ConfigEntryBase> ButtonDrawer(string name = null, string tip = null, Action onClick = null, bool vertical = true)
			{
				return new Action<ConfigEntryBase>((cfgEntry) =>
				{
					if(vertical)
						GUILayout.BeginVertical();
					else
						GUILayout.BeginHorizontal();

					GUILayout.Space(5);

					if(GUILayout.Button(new GUIContent { text = name ?? cfgEntry.Definition.Key, tooltip = tip ?? cfgEntry.Description.Description }, GUILayout.ExpandWidth(true)) && onClick != null)
						onClick();

					GUILayout.Space(5);

					if(vertical)
						GUILayout.EndVertical();
					else
						GUILayout.EndHorizontal();

				});
			}

			public static Action<ConfigEntryBase> DropdownDrawer(string name = null, string tip = null, string[] items = null, int initIndex = 0, Func<string[], string[]> listUpdate = null, Func<int, int> onSelect = null, bool vertical = true)
			{
				int selectedItem = initIndex;
				bool selectingItem = false;
				Vector2 scrollview = Vector2.zero;

				return new Action<ConfigEntryBase>((cfgEntry) =>
				{
					if(vertical)
						GUILayout.BeginVertical();
					else
						GUILayout.BeginHorizontal();

					items = listUpdate != null ? listUpdate(items) : items;

					if((Math.Max(-1, Math.Min(items.Length - 1, selectedItem))) < 0)
						selectedItem = Math.Max(0, Math.Min
						(items.Length - 1, selectedItem));

					if(selectedItem < 0) return;


					try
					{
						GUILayout.Space(3);
						bool btn;
						int maxWidth = 350, maxHeight = 200;
						if(items.Length > 0)
							if((btn = GUILayout.Button(new GUIContent { text = name ?? $"{cfgEntry.Definition.Key} {items[selectedItem]}", tooltip = tip ?? cfgEntry.Description.Description },
								 GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MaxWidth(maxWidth))) || selectingItem)
							{
								selectingItem = !(btn && selectingItem);//if dropdown btn was pressed

								scrollview = GUILayout.BeginScrollView(scrollview, false, false,
									GUILayout.ExpandWidth(true),
									GUILayout.ExpandHeight(true), GUILayout.Height(150), GUILayout.MaxHeight(maxHeight), GUILayout.MaxWidth(maxWidth));

								var select = GUILayout.SelectionGrid(selectedItem, items, 1, GUILayout.ExpandWidth(true));
								if(select != selectedItem) { selectingItem = false; select = onSelect != null ? onSelect(select) : select; }
								selectedItem = select;

								GUILayout.EndScrollView();
							}

						GUILayout.Space(5);
					}
					catch(Exception e)
					{
						Logger.LogError(e);
					}

					if(vertical)
						GUILayout.EndVertical();
					else
						GUILayout.EndHorizontal();
				});
			}

			public static Func<int> GUILayoutDropdownDrawer(Func<string[], int, GUIContent> content, string[] items = null, int initSelection = 0, float scrollHeight = 150, Func<string[], string[]> listUpdate = null, Func<int, int> modSelected = null, Func<int, int> onSelect = null, bool vertical = true)
			{
				int selectedItem = initSelection;
				//var select = selectedItem;
				bool selectingItem = false;
				Vector2 scrollpos = Vector2.zero;


				return new Func<int>(() =>
				{
					void BeginDirection(bool invert = false, params GUILayoutOption[] opt)
					{
						if(vertical && !invert)
							GUILayout.BeginVertical(opt);
						else
							GUILayout.BeginHorizontal(opt);
					}

					void EndDirection(bool invert = false)
					{
						if(vertical && !invert)
							GUILayout.EndVertical();
						else
							GUILayout.EndHorizontal();
					}


					BeginDirection();

					items = listUpdate?.Invoke(items) ?? items;

					if(!items?.InRange(selectedItem) ?? false)
						selectedItem = Math.Max(0, Math.Min
						(items.Length - 1, selectedItem));

					if(!items?.InRange(selectedItem) ?? true)
					{

						EndDirection();
						return -1;
					}

					try
					{
						GUILayout.Space(3);
						bool btn;
						//int maxWidth = 350, maxHeight = 200;
						if(items.Length > 0)
						{
							var tmpcontent = content?.Invoke(items, selectedItem);
							if(tmpcontent != null)
								tmpcontent.text += selectingItem ? " ▲" : " ▼";//▼▾
							if((btn = GUILayout.Button(tmpcontent ?? new GUIContent(selectingItem ? "▲" : "▼"),
								 GUILayout.ExpandWidth(vertical), GUILayout.ExpandHeight(!vertical))) || selectingItem)
							{
								selectingItem = !(btn && selectingItem);//if dropdown btn was pressed

								var rec = new Rect(GUILayoutUtility.GetLastRect());
								GUILayout.Space(scrollHeight);
								rec.y += rec.height;

								var recContent = new Rect(rec) { height = items.Length * (rec.height) };
								rec.height = GUILayoutUtility.GetLastRect().height;


								scrollpos = GUI.BeginScrollView(rec, scrollpos, recContent, false, false, GUIStyle.none, GUI.skin.verticalScrollbar
									//GUILayout.Height(rec.height),
									//GUILayout.ExpandWidth(true),
									//GUILayout.ExpandHeight(true)
									);

								recContent.x += (rec.width * .15f * .5f);
								recContent.width *= .85f;
								var select = GUI.SelectionGrid(recContent, selectedItem, items, 1
								  //GUILayout.Height(recView.height),
								  //GUILayout.ExpandWidth(true),
								  //GUILayout.ExpandHeight(true)
								  );


								if(select != selectedItem) { selectingItem = false; select = onSelect != null ? onSelect(select) : select; }
								selectedItem = select;

								GUI.EndScrollView();

							}
						}

						selectedItem = modSelected?.Invoke(selectedItem) ?? selectedItem;

						GUILayout.Space(5);
					}
					catch(Exception e)
					{
						Logger.LogError(e);
					}

					EndDirection();

					return selectedItem;
				});
			}
		}
	}

}
