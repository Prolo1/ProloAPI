using System;
using System.Xml;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
//using System.Threading.Tasks;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Studio;
using KKAPI.Utilities;
using KKAPI.Maker.UI;
using KKABMX.Core;
using ExtensibleSaveFormat;
using MessagePack.Resolvers;
using MessagePack.Unity;
using MessagePack;
using Studio;
//using HarmonyLib;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UniRx;
using Character_Morpher;
using UGUI_AssistLibrary;







#if HONEY_API

using AIChara;
using static AIChara.ChaFileDefine;
#else
using static ChaFileDefine;
#endif

//using static Character_Morpher.CharaMorpher_Core;
//using static Character_Morpher.CharaMorpher_Controller;
//using static Character_Morpher.CharaMorpher_GUI;
//using static Character_Morpher.Morph_Util;
using static BepInEx.Logging.LogLevel;
//using UGUI_AssistLibrary;

namespace ProloAPI
{
	public class PointerEnter : MonoBehaviour, IPointerEnterHandler
	{
		public void OnPointerEnter(PointerEventData eventData)
		{
		}
	}
	public class PointerExit : MonoBehaviour, IPointerExitHandler
	{
		public void OnPointerExit(PointerEventData eventData)
		{
		}
	}

	public class DependencyInfo<T> where T : BaseUnityPlugin
	{


		public DependencyInfo(Version minTargetVer = null, Version maxTargetVer = null)
			: this(typeof(T), minTargetVer, maxTargetVer) { }

		public DependencyInfo(Type type, Version minTargetVer = null, Version maxTargetVer = null)
		{
			plugin = (T)GameObject.FindObjectOfType(type);
			Exists = plugin != null;
			MinTargetVersion = minTargetVer ?? new Version();
			MaxTargetVersion = maxTargetVer ?? new Version();
			IsInTargetVersionRange = Exists &&
				((CurrentVersion = plugin?.Info.Metadata.Version
				?? new Version()) >= MinTargetVersion);

			if(maxTargetVer != null && maxTargetVer >= MinTargetVersion)
				IsInTargetVersionRange &= Exists && (CurrentVersion <= MaxTargetVersion);
		}

		/// <summary>
		/// plugin reference
		/// </summary>
		public readonly T plugin = null;
		/// <summary>
		/// does the mod exist
		/// </summary>
		public bool Exists { get; } = false;
		/// <summary>
		/// Current version matches or exceeds the min target mod version. 
		/// if a max is set it will also make sure the mod is within range.
		/// </summary>
		public bool IsInTargetVersionRange { get; } = false;
		/// <summary>
		/// min version this mod expects
		/// </summary>
		public Version MinTargetVersion { get; } = null;
		/// <summary>
		/// max version this mod expects
		/// </summary>
		public Version MaxTargetVersion { get; } = null;
		/// <summary>
		/// version that is actually downloaded in the game
		/// </summary>
		public Version CurrentVersion { get; } = null;

		public void PrintExistsMsg()
		{

		}

		public override string ToString()
		{
			return
				$"Plugin Name: {plugin?.Info.Metadata.Name ?? "Null"}\n" +
				$"Current version: {CurrentVersion?.ToString() ?? "Null"}\n" +
				$"Min Target Version: {MinTargetVersion}\n" +
				$"Max Target Version: {MaxTargetVersion}\n";
		}
	}

	public class WeakReference<T> : WeakReference
	{

		public event Action OnTargetCollected;

		public new T Target { get => (T)base.Target; set => base.Target = value; }

		public WeakReference(T target) : this(target, false)
		{
		}

		public WeakReference(T target, bool trackResurrection) : base(target, trackResurrection)
		{
			bool last = true;
			this.ObserveEveryValueChanged(v => v.IsAlive).Subscribe(v =>
			{
				if(v != last)
				{
					last = v;
					OnTargetCollected?.Invoke();
				}
			});
		}

		public override bool Equals(object obj)
		{
			return obj is WeakReference<T> reference &&
				System.Collections.Generic.EqualityComparer<object>.Default.Equals(Target, reference.Target);
		}

		public override int GetHashCode()
		{
			return 106246568 + Target?.GetHashCode() ?? 0;
		}
	}

	public class WeakKeyDictionary<Tkey, Tval> : Dictionary<WeakReference<Tkey>, Tval>
	{
		public new IEnumerable<Tkey> Keys { get => base.Keys.Select(k => k.Target); }

		private WeakReference<Tkey> _search = new WeakReference<Tkey>(default);

		#region Constructors
		public WeakKeyDictionary()
		{
		}

		public WeakKeyDictionary(int capacity) : base(capacity)
		{
		}

		public WeakKeyDictionary(IEqualityComparer<WeakReference<Tkey>> comparer) : base(comparer)
		{
		}

		public WeakKeyDictionary(IDictionary<WeakReference<Tkey>, Tval> dictionary) : base(dictionary)
		{
		}

		public WeakKeyDictionary(int capacity, IEqualityComparer<WeakReference<Tkey>> comparer) : base(capacity, comparer)
		{
		}

		public WeakKeyDictionary(IDictionary<WeakReference<Tkey>, Tval> dictionary, IEqualityComparer<WeakReference<Tkey>> comparer) : base(dictionary, comparer)
		{
		}

		protected WeakKeyDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
		#endregion

		public Tval this[Tkey key]
		{
			get
			{
				_search.Target = key;
				return base.TryGetValue(_search, out var val) ? val : default;
			}
			set
			{
				var tmp = new WeakReference<Tkey>(key);
				tmp.OnTargetCollected += () => { this.Remove(tmp); };
				base[tmp] = value;
			}
		}

		public void Add(Tkey key, Tval val)
		{
			var tmp = new WeakReference<Tkey>(key);
			tmp.OnTargetCollected += () => { this.Remove(tmp); };
			base.Add(tmp, val);
		}

		public void Remove(Tkey key)
		{
			_search.Target = key;
			base.Remove(_search);
		}

		public bool ContainsKey(Tkey key)
		{
			_search.Target = key;
			return base.ContainsKey(_search);
		}
	}


	/// <summary>
	/// utility to bring process to foreground (used for the file select)
	/// </summary>
	public class ForeGrounder
	{
		static IntPtr ptr = IntPtr.Zero;

		/// <summary>
		/// set window to go back to
		/// </summary>
		public static void SetCurrentForground()
		{
			ptr = GetActiveWindow();

			//	Morph_Util.Logger.LogDebug($"Process ptr 1 set to: {ptr}");
		}

		/// <summary>
		/// reverts back to last window specified by SetCurrentForeground
		/// </summary>
		public static void RevertForground()
		{
			//	Morph_Util.Logger.LogDebug($"process ptr: {ptr}");

			if(ptr != IntPtr.Zero)
				SwitchToThisWindow(ptr, true);
		}



		[DllImport("user32.dll")]
		static extern IntPtr GetActiveWindow();
		[DllImport("user32.dll")]
		static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);
	}


	namespace Extentions
	{
		using static Utilities.Util_General;

		public static class Ext_General
		{


			/// <summary>
			/// Adds a value to the end of a list and returns it
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <param name="list"></param>
			/// <param name="val"></param>
			/// <returns></returns>
			public static T AddNReturn<T>(this ICollection<T> list, T val)
			{
				list.Add(val);
				return list.Last();
			}
			public static T FirstOrNull<T>(this IEnumerable<T> enu) where T : class
			{
				//I love loopholes 🤣
				try
				{ return enu.First(); }
				catch { return null; }
			}
			public static T FirstOrNull<T>(this IEnumerable<T> enu, Func<T, bool> predicate) where T : class
			{
				//I love loopholes 🤣
				try
				{ return enu.First(predicate); }
				catch { return null; }
			}
			public static T LastOrNull<T>(this IEnumerable<T> enu) where T : class
			{
				try
				{ return enu.Last(); }
				catch { return null; }
			}     //I love loopholes 🤣
			public static T LastOrNull<T>(this IEnumerable<T> enu, Func<T, bool> predicate) where T : class
			{
				try
				{ return enu.Last(predicate); }
				catch { return null; }
			}   //I love loopholes 🤣

			public static bool InRange<T>(this IEnumerable<T> list, int index) => index >= 0 && index < list.Count();
			public static bool InRange<T>(this T src, T min, T max) where T : IComparable
				=> src.CompareTo(max) <= 0 && src.CompareTo(min) >= 0;

			public static List<KeyValuePair<ConfigDefinition, string>> GetUnorderedOrphanedEntries(this ConfigFile file, string sec = "")
			{
				Dictionary<ConfigDefinition, string> OrphanedEntries = new Dictionary<ConfigDefinition, string>();
				List<KeyValuePair<ConfigDefinition, string>> orderedOrphanedEntries = new List<KeyValuePair<ConfigDefinition, string>>();
				string section = string.Empty;
				string[] array = File.ReadAllLines(file.ConfigFilePath);
				for(int i = 0; i < array.Length; i++)
				{
					string text = array[i].Trim();
					if(text.StartsWith("#"))
					{
						continue;
					}

					if(text.StartsWith("[") && text.EndsWith("]"))
					{
						section = text.Substring(1, text.Length - 2);
						continue;
					}

					string[] array2 = text.Split(new char[1] { '=' }, 2);
					if(sec == section || sec.IsNullOrEmpty())
						if(array2.Length == 2)
						{
							string key = array2[0].Trim();
							string text2 = array2[1].Trim();
							ConfigDefinition key2 = new ConfigDefinition(section, key);
							if(!((IDictionary<ConfigDefinition, ConfigEntryBase>)file).TryGetValue(key2, out _))
							{
								OrphanedEntries[key2] = text2;
								orderedOrphanedEntries.Add(new KeyValuePair<ConfigDefinition, string>(key2, text2));
							}
						}

				}

				return orderedOrphanedEntries;
			}

			/// <summary>
			/// makes sure a path fallows the format "this/is/a/path" and not "this//is\\a/path" or similar
			/// </summary>
			/// <param name="dir"></param>
			/// <param name="oldslash"></param>
			/// <param name="newslash"></param>
			/// <returns></returns>
			public static string MakeDirPath(this string dir, string oldslash = "\\", string newslash = "/")
			{

				dir = (dir ?? "").Trim().Replace(oldslash, newslash).Replace(newslash + newslash, newslash);

				if((dir.LastIndexOf('.') < dir.LastIndexOf(newslash))
					&& dir.Substring(dir.Length - newslash.Length) != newslash)
					dir += newslash;

				return dir;
			}

			/// <summary>
			/// Defaults the ConfigEntry on game launch using default value specified
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <param name="v1"></param>
			/// <param name="v2"></param>
			public static ConfigEntry<T> ConfigDefaulter<T>(this ConfigEntry<T> v1, T v2)
			{
				if(v1 == null || !CharaMorpher_Core.cfg.resetOnLaunch.Value) return v1;

				v1.Value = v2;
				v1.SettingChanged += (m, n) => { if(v2 != null) v2 = v1.Value; };
				return v1;
			}

			/// <summary>
			/// Defaults the ConfigEntry on game launch using default value in ConfigEntry
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <param name="v1"></param>
			/// <param name="v2"></param>
			public static ConfigEntry<T> ConfigDefaulter<T>(this ConfigEntry<T> v1) => v1?.ConfigDefaulter((T)v1.DefaultValue);

			/// <summary>
			/// Crates Image Texture based on path
			/// </summary>
			/// <param name="path">directory path to image (i.e. C:/path/to/image.png)</param>
			/// <param name="data">raw image data that will be read instead of path if not null or empty</param>
			/// <returns>An Texture2D created from path if passed, else a black texture</returns>
			public static Texture2D CreateTexture(this string path, byte[] data = null) =>
				(!data.IsNullOrEmpty() || !File.Exists(path)) ?
				data?.LoadTexture(TextureFormat.RGBA32) ?? Texture2D.blackTexture.ToTexture2D() :
				File.ReadAllBytes(path)?.LoadTexture(TextureFormat.RGBA32) ??
				Texture2D.blackTexture.ToTexture2D();

			/// <summary>
			/// 
			/// </summary>
			/// <param name="gui"></param>
			/// <param name="act"></param>
			/// <returns></returns>
			public static T OnGUIExists<T>(this T gui, UnityAction<T> act) where T : BaseGuiEntry
			{
				if(gui == null) return null;

#if false
			if(!gui.Exists)
			{
				var ob = gui.ObserveEveryValueChanged(p => p.Exists, FrameCountType.EndOfFrame);
				var sub = ob.Subscribe(val =>
				{
					if(!val) return;
					act(gui);
				});
			}
			else
			{
				act(gui);
			}
#else
				GetInstance<ProloBaseUnityPlugin>().StartCoroutine(func(gui, act));
				IEnumerator func(T gui1, UnityAction<T> act1)
				{
					if(!gui1.Exists)
						while(!gui1.Exists)
							yield return new WaitForEndOfFrame();//the thing needs to exist first

					act1(gui);

					yield break;
				}
#endif

				return gui;
			}


			public static Graphic GetTextComponentInChildren(this GameObject obj)
			{
				return (Graphic)obj?.GetComponentInChildren<TMP_Text>() ??
				 obj?.GetComponentInChildren<Text>();
			}
			public static Graphic GetTextComponentInChildren(this UIBehaviour obj)
			{
				return obj.gameObject.GetTextComponentInChildren();
			}

			/// <summary>
			/// gets the text of the first Text or TMP_Text component in a game object or it's children.
			///  If no component return null. 
			/// </summary>
			/// <param name="obj"></param>
			/// <returns></returns>
			public static string GetTextFromTextComponent(this GameObject obj)
				=>
				obj?.GetComponentInChildren<TMP_Text>()?.text ??
				obj?.GetComponentInChildren<Text>()?.text ?? null;

			/// <summary>
			/// gets the text of the first Text or TMP_Text component in a game object or it's children.
			///  If no component return null. 
			/// </summary>
			/// <param name="obj"></param>
			/// <returns></returns>
			public static string GetTextFromTextComponent(this UIBehaviour obj)
				=> obj.gameObject.GetTextFromTextComponent();

			/// <summary>
			/// sets the text of the first Text or TMP_Text component in a game object or it's children.
			///  If no component does nothing. 
			/// </summary>
			/// <param name="obj"></param>
			/// <returns></returns>
			public static void SetTextFromTextComponent(this GameObject obj, string txt) =>
			((Component)obj?.GetComponentInChildren<TMP_Text>() ??
				obj?.GetComponentInChildren<Text>())?
				.SetTextFromTextComponent(txt);

			/// <summary>
			/// sets the text of the first Text or TMP_Text component in a game object or it's children.
			///  If no component does nothing. 
			/// </summary>
			/// <param name="obj"></param>
			/// <returns></returns>
			public static void SetTextFromTextComponent(this Component obj, string txt)
			{
				Component comp;
				if(comp = obj?.GetComponentInChildren<TMP_Text>())
					((TMP_Text)comp).text = (txt);
				else if(comp = obj?.GetComponentInChildren<Text>())
					((Text)comp).text = (txt);
			}


			public static GameObject ScaleToParent2D(this GameObject obj, float pwidth = 1, float pheight = 1, bool changewidth = true, bool changeheight = true)
			{
				RectTransform rectTrans = null;

				rectTrans = obj?.GetComponent<RectTransform>();

				if(rectTrans == null) return obj;

				//var rectTrans = par.GetComponent<RectTransform>();
				rectTrans.anchorMin = new Vector2(
					changewidth ? 0 + (1 - pwidth) : rectTrans.anchorMin.x,
					changeheight ? 0 + (1 - pheight) : rectTrans.anchorMin.y);
				rectTrans.anchorMax = new Vector2(
					changewidth ? 1 - (1 - pwidth) : rectTrans.anchorMax.x,
					changeheight ? 1 - (1 - pheight) : rectTrans.anchorMax.y);

				rectTrans.localPosition = Vector3.zero;//The location of this line matters

				rectTrans.offsetMin = new Vector2(
					changewidth ? 0 : rectTrans.offsetMin.x,
					changeheight ? 0 : rectTrans.offsetMin.y);
				rectTrans.offsetMax = new Vector2(
					changewidth ? 0 : rectTrans.offsetMax.x,
					changeheight ? 0 : rectTrans.offsetMax.y);
				//rectTrans.pivot = new Vector2(0.5f, 0.5f);

				return obj;
			}
			public static T ScaleToParent2D<T>(this T comp, float pwidth = 1, float pheight = 1, bool width = true, bool height = true) where T : Component
			{
				comp?.gameObject.ScaleToParent2D(pwidth: pwidth, pheight: pheight, changewidth: width, changeheight: height);
				return comp;
			}


			public static T GetComponentInParent<T>(this GameObject obj) where T : Component
			{
				Transform search = obj.transform;
				T ans = null;
				while(search && ans == null)
				{
					ans = search.GetComponent<T>();
					search = search.parent;
				}
				return ans;
			}
			public static T GetComponentInParent<T>(this Component obj) where T : Component =>
				GetComponentInParent<T>(obj.gameObject);

			public static T GetOrAddComponent<T>(this Component obj) where T : Component => obj.gameObject.GetOrAddComponent<T>();
			public static T GetOrAddComponent<T>(this GameObject obj) where T : Component => obj.GetComponent<T>() ?? obj.AddComponent<T>();

			public static IEnumerable<T> GetComponentsInChildren<T>(this GameObject obj, int depth) where T : Component =>
			 obj.GetComponentsInChildren<T>().Where((v1) =>
			(((Component)(object)v1).transform.HierarchyLevelIndex() - obj.transform.HierarchyLevelIndex()) < (depth > 0 ? depth + 1 : int.MaxValue));
			public static IEnumerable<T> GetComponentsInChildren<T>(this Component obj, int depth) where T : Component =>
				obj.gameObject.GetComponentsInChildren<T>(depth);

			public static int HierarchyLevelIndex(this Transform obj) => obj.parent ? obj.parent.HierarchyLevelIndex() + 1 : 0;
			public static int HierarchyLevelIndex(this GameObject obj) => obj.transform.HierarchyLevelIndex();

		}

		public static class Ext_Game
		{
			public static PluginData SaveExtData<Tctrl>(this Tctrl ctrl, PluginData data = default, UnityAction pre = null, UnityAction post = null) => ctrl.SaveExtData<Tctrl, PluginData>(data, pre, post);
			public static Tdata SaveExtData<Tctrl, Tdata>(this Tctrl ctrl, Tdata data = default, UnityAction pre = null, UnityAction post = null) where Tdata : class
			{
				pre();
				var tmp = GetSaveLoadManager<Tctrl, Tdata>().Save(ctrl, data);
				post();
				return tmp;
			}
			public static PluginData LoadExtData<Tctrl>(this Tctrl ctrl, PluginData data = default, UnityAction pre = null, UnityAction post = null) => ctrl.LoadExtData<Tctrl, PluginData>(data, pre, post);
			public static Tdata LoadExtData<Tctrl, Tdata>(this Tctrl ctrl, Tdata data = default, UnityAction pre = null, UnityAction post = null) where Tdata : class
			{
				pre();
				var tmp = GetSaveLoadManager<Tctrl, Tdata>().Load(ctrl, data);
				post();
				return tmp;
			}

			public static PluginData Copy(this PluginData source)
			{
				return new PluginData
				{
					version = source.version,
					data = source.data.ToDictionary((p) => p.Key, (p) => p.Value),
				};
			}

		}

		public static class Ext_GUI
		{


			public static T AddToCustomGUILayout<T>(this T gui, bool topUI = false, float pWidth = -1, float viewpercent = -1, bool newVertLine = true) where T : BaseGuiEntry
			{
#if true //TODO: fix new UI loading in KK
				gui?.OnGUIExists(g =>
				{

					GetInstance<ProloBaseUnityPlugin>().StartCoroutine(g.AddToCustomGUILayoutCO(topUI, pWidth, viewpercent, newVertLine));


					//await g.AddToCustomGUILayoutCO(topUI, pWidth, viewpercent, newVertLine);
				});
#endif
				return gui;
			}

			static IEnumerator AddToCustomGUILayoutCO<T>(this T gui, bool topUI = false, float pWidth = -1, float viewpercent = -1, bool newVertLine = true, GameObject ctrlObj = null) where T : BaseGuiEntry
			{
				if(cfg.debug.Value)  ProloBaseUnityPlugin.PInfoBase.Logger.LogDebug("moving object");

				ctrlObj = ctrlObj ?? gui.ControlObject;

				//await Func();
				//async Task Func()
				//{
				//	UnityEngine.Debug.Log("We Made it here");
				//	Logger.LogInfo("Looking for scrollrect?");
				//	while(ctrlObj?.GetComponentInParent<ScrollRect>() == null)
				//	{
				//		await Task.Delay(1000);
				//		ctrlObj = ctrlObj ?? gui.ControlObject;
				//	}
				//	UnityEngine.Debug.Log("We Made it here Too");
				//	Logger.LogInfo("scrollrect found!!!");
				//}

				if(!ctrlObj)
					yield return new WaitWhile(() => ctrlObj.GetComponentInParent<ScrollRect>() == null);

#if KK
				var rctrl = ctrlObj.GetComponentInParent<UI_RaycastCtrl>();
				rctrl.Reset();
#endif


				//	newVertLine = horizontal ? newVertLine : true;
#if HONEY_API
				if(gui is MakerText)
				{
					var piv = (Vector2)ctrlObj?
						.GetComponentInChildren<Text>()?
						.rectTransform.pivot;
					piv.x = -.5f;
					piv.y = 1f;
				}
#endif


				var scrollRect = ctrlObj.GetComponentInParent<ScrollRect>();
				var par = scrollRect.transform;


				if(cfg.debug.Value)  ProloBaseUnityPlugin.PInfoBase.Logger.LogDebug("Parent: " + par);

				int countcheck = 0;

				//setup VerticalLayoutGroup
				if(cfg.debug.Value)  ProloBaseUnityPlugin.PInfoBase.Logger.LogDebug("Check: " + ++countcheck);
				var vlg = scrollRect.gameObject.GetOrAddComponent<VerticalLayoutGroup>();

#if HONEY_API
				vlg.childAlignment = TextAnchor.UpperLeft;
#else
				vlg.childAlignment = TextAnchor.UpperCenter;
#endif
				var pad = 10;//(int)cfg.unknownTest.Value;//10
				vlg.padding = new RectOffset(pad, pad + 5, 0, 0);
				vlg.childControlWidth = true;
				vlg.childControlHeight = true;
				vlg.childForceExpandWidth = true;
				vlg.childForceExpandHeight = false;

				//This fixes the KOI_API rendering issue & enables scrolling over viewport (not elements tho)
				//Also a sizing issue in Honey_API
				if(cfg.debug.Value)  ProloBaseUnityPlugin.PInfoBase.Logger.LogDebug("Check: " + ++countcheck);
#if KOI_API
				scrollRect.GetComponent<Image>().sprite = scrollRect.content.GetComponent<Image>()?.sprite;
				scrollRect.GetComponent<Image>().color = (Color)scrollRect.content.GetComponent<Image>()?.color;


				scrollRect.GetComponent<Image>().enabled = true;
				scrollRect.GetComponent<Image>().raycastTarget = true;
				var img = scrollRect.content.GetComponent<Image>();
				if(!img)
					img = scrollRect.viewport.GetComponent<Image>();
				img.enabled = false;
#elif HONEY_API
				//		scrollRect.GetComponent<RectTransform>().sizeDelta =
				//		  scrollRect.transform.parent.GetComponentInChildren<Image>().rectTransform.sizeDelta;
#endif

				//Setup LayoutElements 
				if(cfg.debug.Value)  ProloBaseUnityPlugin.PInfoBase.Logger.LogDebug("Check: " + ++countcheck);
				scrollRect.verticalScrollbar.GetOrAddComponent<LayoutElement>().ignoreLayout = true;
				scrollRect.content.GetOrAddComponent<LayoutElement>().ignoreLayout = true;

				var viewLE = scrollRect.viewport.GetOrAddComponent<LayoutElement>();
#if !KK
				viewLE.layoutPriority = 1;
#endif
				viewLE.minWidth = -1;
				viewLE.flexibleWidth = -1;
				gui.ResizeCustomUIViewport(viewpercent);


				Transform layoutObj = null;
				//Create  LayoutElement
				//if(horizontal)

				//Create Layout Element GameObject
				if(cfg.debug.Value)  ProloBaseUnityPlugin.PInfoBase.Logger.LogDebug("Check: " + ++countcheck);

				GameObject CreateGameObject(string name, Transform parent = null)
				{
					var tmp = new GameObject(name);
					tmp.transform.parent = parent;
					return tmp;
				}


				act1();
				void act1()
				{
					par = newVertLine ?
						CreateGameObject("LayoutElement", par)?.transform :
						par.GetComponentsInChildren<HorizontalLayoutGroup>(2)
						.LastOrNull((elem) => elem.GetComponent<HorizontalLayoutGroup>())?.transform.parent ??
						CreateGameObject("LayoutElement", par)?.transform;

					//await Task.Yield();

					layoutObj = par = par.GetOrAddComponent<RectTransform>().transform;//May need this line (I totally do)
				}



				//calculate base GameObject sizing
				if(cfg.debug.Value)  ProloBaseUnityPlugin.PInfoBase.Logger.LogDebug("Check: " + ++countcheck);
				var ele = par.GetOrAddComponent<LayoutElement>();
				ele.minWidth = -1;
				ele.minHeight = -1;
				ele.preferredHeight = System.Math.Max(ele?.preferredHeight ?? -1, ctrlObj.GetOrAddComponent<LayoutElement>()?.minHeight ?? ele?.preferredHeight ?? -1);
				ele.preferredWidth =
#if HONEY_API
				scrollRect.GetComponent<RectTransform>().rect.width;
#else
				//viewLE.minWidth;
				0;
#endif

				var lgtmp = par.gameObject.GetComponentInParent<VerticalLayoutGroup>();
				lgtmp.CalculateLayoutInputHorizontal();
				lgtmp.CalculateLayoutInputVertical();


				//Create and Set Horizontal Layout Settings
				if(cfg.debug.Value)  ProloBaseUnityPlugin.PInfoBase.Logger.LogDebug("Check: " + ++countcheck);
				act2();
				void act2()
				{

					par = par.GetComponentsInChildren<HorizontalLayoutGroup>(2)?
						.FirstOrNull((elem) => elem.gameObject.GetComponent<HorizontalLayoutGroup>())?.transform ??
						GameObject.Instantiate<GameObject>(new GameObject("HorizontalLayoutGroup"), par)?.transform;
					par = par.gameObject.GetOrAddComponent<RectTransform>().transform;//May need this line (I totally do)

					//	await Task.Yield();

					var layout = par.GetOrAddComponent<HorizontalLayoutGroup>();

					//	await Task.Yield();

					layout.childControlWidth = true;
					layout.childControlHeight = true;
					layout.childForceExpandWidth = true;
					layout.childForceExpandHeight = true;
					layout.childAlignment = TextAnchor.MiddleCenter;

					par?.ScaleToParent2D();
				};


				//Add layout elements to control object children
				if(cfg.debug.Value)  ProloBaseUnityPlugin.PInfoBase.Logger.LogDebug("Check: " + ++countcheck);
				for(int a = 0; a < ctrlObj.transform.childCount; ++a)
				{
					ele = ctrlObj.transform.GetChild(a).GetOrAddComponent<LayoutElement>();
					ele.preferredHeight = ele.GetComponent<RectTransform>().rect.height;
				}

				//remove extra LayoutElements
				if(cfg.debug.Value)  ProloBaseUnityPlugin.PInfoBase.Logger.LogDebug("Check: " + ++countcheck);
				var rList = ctrlObj.GetComponents<LayoutElement>();
				for(int a = 1; a < rList.Length; ++a)
					GameObject.DestroyImmediate(rList[a]);



				//change child layout elements
				if(cfg.debug.Value)  ProloBaseUnityPlugin.PInfoBase.Logger.LogDebug("Check: " + ++countcheck);
				foreach(var val in ctrlObj.GetComponentsInChildren<LayoutElement>(0))
					if(val.gameObject != ctrlObj)
						val.flexibleWidth = val.minWidth = val.preferredWidth = -1;


				//edit layout groups
				if(cfg.debug.Value)  ProloBaseUnityPlugin.PInfoBase.Logger.LogDebug("Check: " + ++countcheck);
				foreach(var val in ctrlObj.GetComponentsInChildren<HorizontalLayoutGroup>(0))
				//	if(val.gameObject != ctrlObj)
				{
					val.childControlWidth = true;
					val.childForceExpandWidth = true;

				}

				//Set this object's Layout settings
				if(cfg.debug.Value)  ProloBaseUnityPlugin.PInfoBase.Logger.LogDebug("Check: " + ++countcheck);
				if(cfg.debug.Value)  ProloBaseUnityPlugin.PInfoBase.Logger.LogDebug("setting as first/last");
				ctrlObj.transform.SetParent(par, false);
				ctrlObj.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
				var apos = ctrlObj.GetComponent<RectTransform>().anchoredPosition; apos.x = 0;
				if(topUI)
				{
					if(layoutObj?.GetSiblingIndex() != scrollRect.viewport.transform.GetSiblingIndex() - 1)
						layoutObj?.SetSiblingIndex
							(scrollRect.viewport.transform.GetSiblingIndex());
				}
				else
					layoutObj?.SetAsLastSibling();

				//if(ctrlObj.GetComponent<LayoutElement>())
				//	GameObject.Destroy(ctrlObj.GetComponent<LayoutElement>());
				var thisLE = ctrlObj.GetOrAddComponent<LayoutElement>();
#if !KK
				thisLE.layoutPriority = 5;
#endif
				thisLE.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
				bool check = thisLE.transform.childCount > 1 &&
					!thisLE.GetComponent<HorizontalOrVerticalLayoutGroup>();
				if(check)
				{
					var tmp = GameObject.Instantiate(new GameObject(), thisLE.transform);
					var hlog = tmp.AddComponent<HorizontalLayoutGroup>();
					hlog.childAlignment = TextAnchor.MiddleLeft;
					hlog.childControlHeight = true;
					hlog.childControlWidth = false;
					hlog.childForceExpandHeight = false;
					hlog.childForceExpandWidth = true;

					for(int a = 0; a < thisLE.transform.childCount; ++a)
						if(thisLE.transform.GetChild(a) != tmp.transform)
							thisLE.transform.GetChild(a--).SetParent(tmp.transform);

				}
				if(thisLE.transform.childCount == 1)
					thisLE.transform.GetChild(0).ScaleToParent2D();


				thisLE.flexibleWidth = -1;
				thisLE.flexibleHeight = -1;
				thisLE.minWidth = -1;
				//thisLE.minHeight = -1;

				thisLE.preferredWidth =
#if HONEY_API
					  pWidth > 0 ? scrollRect.rectTransform.rect.width * pWidth : -1;
#else
			//	 pWidth > 0 ? scrollRect.rectTransform.rect.width * pWidth : -1;
			0;
#endif
				//thisLE.preferredHeight = ctrlObj.GetComponent<RectTransform>().rect.height;


				//Reorder Scrollbar
				if(!topUI)
				{
					scrollRect.verticalScrollbar?.transform.SetAsLastSibling();
					scrollRect.horizontalScrollbar?.transform.SetAsLastSibling();
				}

				vlg.SetLayoutVertical();
				LayoutRebuilder.MarkLayoutForRebuild(scrollRect.GetComponent<RectTransform>());
				yield break;
			}


			public static void OnUIEnter<T>(this T gui, UnityAction enterAct) where T : UIBehaviour
				=> gui.gameObject.OnUIEnter(enterAct);
			public static void OnUIEnter(this GameObject gui, UnityAction enterAct)
			{
				var tmp = gui.GetOrAddComponent<UIAL_EventTrigger>();
				tmp.triggers.AddNReturn(new UIAL_EventTrigger.Entry { eventID = EventTriggerType.PointerEnter })
					.callback.AddListener(_ => enterAct());

			}

			public static void OnUIExit<T>(this T gui, UnityAction endAct) where T : UIBehaviour
				=> gui.gameObject.OnUIExit(endAct);
			public static void OnUIExit(this GameObject gui, UnityAction endAct)
			{
				var tmp = gui.GetOrAddComponent<UIAL_EventTrigger>();
				tmp.triggers.AddNReturn(new UIAL_EventTrigger.Entry { eventID = EventTriggerType.PointerExit })
					.callback.AddListener(_ => endAct());


			}


			public static void OnUIStay<T>(this T gui, UnityAction stayAct, UnityAction enterAct = null, UnityAction endAct = null) where T : UIBehaviour
				=> gui.gameObject.OnUIStay(stayAct, enterAct, endAct);
			public static void OnUIStay(this GameObject gui, UnityAction stayAct, UnityAction enterAct = null, UnityAction endAct = null)
			{

				Coroutine co = null;
				IEnumerator Func(UnityAction acti)
				{
					yield return new WaitWhile(() =>
					{
						acti?.Invoke();
						return true;
					});
				}
				gui.OnUIEnter(() => { enterAct?.Invoke(); co = GetInstance<ProloBaseUnityPlugin>().StartCoroutine(Func(stayAct)); });
				gui.OnUIExit(() => { if(co != null) endAct?.Invoke(); GetInstance<ProloBaseUnityPlugin>().StopCoroutine(co); });
			}

			static Coroutine resizeco;
			public static void ResizeCustomUIViewport<T>(this T template, float viewPercent = -1) where T : BaseGuiEntry
			{
				if(viewPercent >= 0 && cfg.makerViewportUISpace.Value != viewPercent)
					cfg.makerViewportUISpace.Value = viewPercent;
				viewPercent = cfg.makerViewportUISpace.Value;

				if(template != null)
					template.OnGUIExists((gui) =>
					{


						IEnumerator func()
						{

							var ctrlObj = gui?.ControlObject;
							if(ctrlObj == null) yield break;

							yield return new WaitUntil(() =>
							ctrlObj.GetComponentInParent<ScrollRect>() != null);

							var scrollRect = ctrlObj.GetComponentInParent<ScrollRect>();

							var viewLE = scrollRect.viewport.GetOrAddComponent<LayoutElement>();
							float vHeight = Mathf.Abs(scrollRect.rectTransform.rect.height);
							viewLE.minHeight = vHeight * viewPercent;

							LayoutRebuilder.MarkLayoutForRebuild(scrollRect.rectTransform);
						}

						if(resizeco != null) GetInstance<ProloBaseUnityPlugin>().StopCoroutine(resizeco);
						resizeco = GetInstance<ProloBaseUnityPlugin>().StartCoroutine(func());
					});

			}


		}
	}
}
