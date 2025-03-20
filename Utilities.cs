using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;

#if HONEY_API
using AIChara;
#endif

#if !IL2CPP
using KKAPI.Chara;
#else
using Character;
using ILLGames.Extensions;
using Il2CppInterop;
#endif


namespace ProloAPI
{
    using Extensions;


    using UnityEngine.Events;

    namespace Utilities
    {
        using System.ComponentModel;
        using System.Runtime.InteropServices;
        using System.Threading;









#if !IL2CPP


        using KKAPI;

        using KKAPI.Utilities;

        using Manager;
#endif

        using static PGeneral;

        public class PGeneral
        {
            public static bool Debug { get; set; } = false;
            private static BaseSaveLoadManager _saveLoad = null;

            internal static readonly ManualLogSource ProloLogger = BepInEx.Logging.Logger.CreateLogSource("Prolo Logger");

#if !IL2CPP
            /// <summary>
            /// force the extra data of a character coordinate to load again
            /// </summary>
            /// <param name="coord"></param>
            /// <param name="control"></param>
            public static void ForceInvokeCoordBeingLoaded(ChaFileCoordinate coord, ChaControl control)
            {
                var ctrlers = CharacterApi.GetBehaviours(control);
                //save coordinate states
                var states = ctrlers?.ToDictionary((x) => x, (y) => y.ControllerRegistration.MaintainCoordinateState);

                foreach(var ctrl in ctrlers)
                    ctrl.ControllerRegistration.MaintainCoordinateState = false;

                typeof(CharacterApi).GetMethod("OnCoordinateBeingLoaded",
                            BindingFlags.Static | BindingFlags.NonPublic,
                            types: new Type[] { typeof(ChaControl), typeof(ChaFileCoordinate) },
                            binder: null, modifiers: null)
                            .Invoke(null, new object[]
                            {control, coord});

                //restore coordinate states
                foreach(var ctrl in ctrlers)
                    ctrl.ControllerRegistration.MaintainCoordinateState = states[ctrl];

            }

            /// <summary>
            /// Force the extra data of a character card to load again
            /// </summary>
            /// <param name="control"></param>
            public static void ForceInvokeOnReload(ChaControl control)
            {
                var ctrlers = CharacterApi.GetBehaviours(control);
                //save coordinate states
                var states = ctrlers?.ToDictionary((x) => x, (y) => y.ControllerRegistration.MaintainState);

                foreach(var ctrl in ctrlers)
                    ctrl.ControllerRegistration.MaintainState = false;

                typeof(CharacterApi).GetMethod("ReloadChara",
                            BindingFlags.Static | BindingFlags.NonPublic,
                            types: new Type[] { typeof(ChaControl) },
                            binder: null, modifiers: null)
                            .Invoke(null, new object[]
                            {control});

                //restore coordinate states
                foreach(var ctrl in ctrlers)
                    ctrl.ControllerRegistration.MaintainState = states[ctrl];

            }
#endif
            public static Tmng GetSaveLoadManager<Tmng>() where Tmng : BaseSaveLoadManager
            {
                if(_saveLoad == null || !(_saveLoad is Tmng))
                    _saveLoad = (Tmng)Activator.CreateInstance(typeof(Tmng));

                return (Tmng)_saveLoad;
            }

            /// <summary>
            /// Gets the first instance of specified plugin.
            /// </summary>
            /// <typeparam name="Tinst"></typeparam>
            /// <returns></returns>
#if IL2CPP
			internal static Tinst GetInstance<Tinst>() where Tinst : ProloBaseUnityPluginIL2CPP
				=> (Tinst)ProloBaseUnityPluginIL2CPP.Instances.First((p) => p is Tinst);
#else
            internal static Tinst GetInstance<Tinst>() where Tinst : ProloBaseUnityPlugin
                => (Tinst)ProloBaseUnityPlugin.Instances.First((p) => p is Tinst);
#endif

            private static Texture2D _greyTex = null;
            public static Texture2D greyTex
            {
                get
                {
                    if(_greyTex != null) return _greyTex;
                    return _greyTex = ColourTexture(Color.black);
                }
            }

            /// <summary>
            /// Creates a 1x1 texture that uses only one colour
            /// </summary>
            /// <param name="colour"></param>
            /// <returns></returns>
            public static Texture2D ColourTexture(Color colour)
            {
                Texture2D tex = new Texture2D(1, 1);
                var pixels = tex.GetPixels();
                for(int a = 0; a < pixels.Length; ++a)
                    pixels[a] = colour;
                tex.SetPixels(pixels);
                tex.Apply();

                return tex;
            }

#if !IL2CPP
            /// <summary>
            /// Returns a list of the regestered handeler specified. returns empty list otherwise 
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            public static IEnumerable<T> GetAllChaFuncCtrlOfType<T>() where T : CharaCustomFunctionController
            {
                foreach(var hnd in CharacterApi.RegisteredHandlers)
                    if(hnd.ControllerType == typeof(T))
                        return hnd.Instances.Cast<T>();

                return new T[] { };
            }
#elif IL2CPP
			/// <summary>
			/// Returns a list of the regestered handeler specified. returns empty list otherwise 
			/// </summary>
			/// <typeparam name="T"></typeparam>
			/// <returns></returns>
			public static IEnumerable<T> GetAllChaFuncCtrlOfType<T>() where T : ProloCharaCustomFunctionControllerIL2CPP
			{

				var list = ProloCharaCustomFunctionControllerIL2CPP.list;
				return list?.OfType<T>() ?? new T[] { };

			}
#endif
            public static MemoryStream ResourceGrabber(string name, Assembly ass = null, string[] res = null, MemoryStream mem = null)
            {
                /**This stuff will be used later*/
                //Logger.LogDebug($"\nResources:\n[{string.Join(", ", resources)}]");
                ass = ass ?? Assembly.GetExecutingAssembly();
                res = res ?? ass.GetManifestResourceNames();
                var data = ass.GetManifestResourceStream(res.FirstOrDefault((txt) => (txt.ToLower()).Contains(name)) ?? " ");
                mem = mem ?? new MemoryStream();
#if KK
                mem.SetLength(0);//Clear Buffer 
                mem.Write(data.ReadAllBytes(), 0, (int)data.Length);//write Buffer
#else
				mem.SetLength(0);//Clear Buffer 
				data?.CopyTo(mem);
#endif
                return mem;
            }

            public static GameObject CreateGameObject(string name, Transform parent = null)
            {
                var tmp = new GameObject(name);
                tmp.transform.parent = parent;
                return tmp;
            }
#if IL2CPP
			public static GameObject CreateGameObject(string name, params Il2CppSystem.Type[] components)
			{
				var tmp = new GameObject(name, components);
				return tmp;
			}

			public static GameObject CreateGameObject(string name, Transform parent, params Il2CppSystem.Type[] components)
			{
				var tmp = new GameObject(name, components);
				tmp.transform.parent = parent;
				return tmp;
			}
#else
            public static GameObject CreateGameObject(string name, params Type[] components)
            {
                var tmp = new GameObject(name, components);
                return tmp;
            }

            public static GameObject CreateGameObject(string name, Transform parent, params Type[] components)
            {
                var tmp = new GameObject(name, components);
                tmp.transform.parent = parent;
                return tmp;
            }
#endif
        }

#if !IL2CPP

        internal class DummyChara<T> where T : CharaCustomFunctionController
        {
            private static ChaControl _extraCharacter = null;


            /// <summary>
            /// true: creates a new instance if one is not created. false: destroys the current instance
            /// </summary>
            public static bool initialize
            {
                set
                {
                    if(value)
                    {
                        if(_extraCharacter == null)
                        {

                            Transform parent = null;
                            parent = GetAllChaFuncCtrlOfType<T>()?.First()?.transform.parent;
                            //_extraCharacter = new ChaControl();

                            _extraCharacter =

#if HONEY_API
							Character.Instance.CreateChara(1, parent?.gameObject, -10);
#elif KK
                                Character.Instance.CreateFemale(parent?.gameObject, -10, hiPoly: false);
#elif KKS
							Character.CreateFemale(parent?.gameObject, -10, hiPoly: false);
#endif

                            if(!(_extraCharacter?.gameObject)) { _extraCharacter = null; return; }

                            //remove character from internal list
#if KKS
						Character.DeleteChara(_extraCharacter, entryOnly: true);
#else
                            Character.Instance?.DeleteChara(_extraCharacter, entryOnly: true);
#endif

                            //BoneController _bonectrl = null;
                            //if(ABMXDependency.IsInTargetVersionRange)
                            //    _bonectrl = _extraCharacter?.GetComponent<BoneController>();

                            //This is needed so extracharacter is not immediately destroyed
                            var ctrler = _extraCharacter?.GetComponent<T>();
                            if(ctrler)
                            {

                                if(Debug) ProloLogger.LogDebug("Destroying dummy chara controller");

                                ctrler.enabled = false;
                                GameObject.Destroy(ctrler);//change back to Destroy if issues arise
                            }

                            _extraCharacter.gameObject.SetActive(false);
                            if(Debug) ProloLogger.LogDebug("created new Morph character instance");
                        }

                        return;
                    }


                    //if(_bonectrl) _bonectrl.hideFlags = HideFlags.None;
                    //if(_bonectrl) GameObject.Destroy(_bonectrl);
                    if(_extraCharacter) GameObject.Destroy(_extraCharacter?.gameObject);

                    _extraCharacter = null;
                }
                get { return _extraCharacter != null; }
            }

            public static ChaControl extraCharacter { get => _extraCharacter; }

            public static ChaFileControl chaFile { get => extraCharacter?.chaFile; }
        }
#endif

        public class PGUI
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
                        ProloLogger.LogError(e);
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
                        ProloLogger.LogError(e);
                    }

                    EndDirection();

                    return selectedItem;
                });
            }

            static GUIStyle tmpSty = null;
            public static void IMGUITooltipMsg(string tooltip = null, bool enableTip = true)
            {
                tooltip = tooltip ?? GUI.tooltip;

                if(enableTip && !tooltip.IsNullOrEmpty())
                {
                    if(tmpSty == null)
                    {
                        var tex = ColourTexture(new Color(0, 0, 0, .5f));

                        tmpSty = new GUIStyle(GUI.skin.label)
                        {
                            normal = new GUIStyleState
                            {
                                textColor = Color.cyan,
                                background = tex
                            },
                            wordWrap = true,
                            alignment = TextAnchor.MiddleCenter,
                        };
                    }

                    var rect = GUI.tooltipRect;
                    tmpSty.fontSize = 16;
                    var content = GUIContent.Temp(tooltip);
                    var size = rect.size; //new Vector2(winRec.width * .5f, tmpSty.CalcHeight(content, winRec.width * .5f) + 10);
                                          //var size = tmpSty.CalcSize(content);
                    var pos = Event.current.mousePosition;
                    pos -= new Vector2(size.x * .5f, size.y + 10);//Copy vector 

                    //pos.x = ((pos.x + size.x) > winRec.xMax ? winRec.xMax - size.x : pos.x);
                    //pos.y = (pos.y < winRec.yMin ? winRec.yMin : pos.y);

                    var ymp = tooltip == GUI.tooltip ? rect : new Rect(pos, size);
                    if(tooltip != null)
                    {
                        GUI.Label(ymp, tooltip, (GUIStyle)tmpSty);
                        //		Logger.LogInfo($"\nConstraint: {winRec}\nRect info: {ymp}\nTooltip: {tooltip}");
                    }
                }


            }

        }
#if IL2CPP

		//
		// Summary:
		//     Provides methods for running code on other threads and synchronizing with the
		//     main thread.
		public sealed class ThreadingHelper : MonoBehaviour, ISynchronizeInvoke
		{
			private sealed class InvokeResult : IAsyncResult
			{
				internal bool ExceptionThrown;

				public bool IsCompleted { get; private set; }

				public WaitHandle AsyncWaitHandle { get; }

				public object AsyncState { get; private set; }

				public bool CompletedSynchronously { get; private set; }

				public InvokeResult()
				{
					AsyncWaitHandle = new EventWaitHandle(initialState: false, EventResetMode.ManualReset);
				}

				public void Finish(object result, bool completedSynchronously)
				{
					AsyncState = result;
					CompletedSynchronously = completedSynchronously;
					IsCompleted = true;
					((EventWaitHandle)AsyncWaitHandle).Set();
				}
			}

			private readonly object _invokeLock = new object();

			private Action _invokeList;

			private Thread _mainThread;

			//
			// Summary:
			//     Current instance of the helper.
			public static ThreadingHelper Instance { get; private set; }

			//
			// Summary:
			//     Gives methods for invoking delegates on the main unity thread, both synchronously
			//     and asynchronously. Can be used in many built-in framework types, for example
			//     System.IO.FileSystemWatcher.SynchronizingObject and System.Timers.Timer.SynchronizingObject
			//     to make their events fire on the main unity thread.
			public static ISynchronizeInvoke SynchronizingObject => Instance;

			//
			// Summary:
			//     False if current code is executing on the main unity thread, otherwise True.
			//     Warning: Will return true before the first frame finishes (i.e. inside plugin
			//     Awake and Start methods).
			public bool InvokeRequired
			{
				get
				{
					if(_mainThread != null)
					{
						return _mainThread != Thread.CurrentThread;
					}

					return true;
				}
			}

			internal static void Initialize()
			{
				GameObject gameObject = new GameObject("BepInEx_ThreadingHelper");
				//if(Chainloader.ConfigHideBepInExGOs.Value)
				//{
				//	gameObject.hideFlags = HideFlags.HideAndDontSave;
				//}

				UnityEngine.Object.DontDestroyOnLoad(gameObject);
				Instance = gameObject.AddComponent<ThreadingHelper>();
			}

			//
			// Summary:
			//     Queue the delegate to be invoked on the main unity thread. Use to synchronize
			//     your threads.
			public void StartSyncInvoke(Action action)
			{
				if(action == null)
				{
					throw new ArgumentNullException("action");
				}

				lock(_invokeLock)
				{
					_invokeList = (Action)Delegate.Combine(_invokeList, action);
				}
			}

			private void Update()
			{
				if(_mainThread == null)
				{
					_mainThread = Thread.CurrentThread;
				}

				if(_invokeList == null)
				{
					return;
				}

				Action invokeList;
				lock(_invokeLock)
				{
					invokeList = _invokeList;
					_invokeList = null;
				}

				foreach(Action item in invokeList.GetInvocationList().Cast<Action>())
				{
					try
					{
						item();
					}
					catch(Exception ex)
					{
						LogInvocationException(ex);
					}
				}
			}

			//
			// Summary:
			//     Queue the delegate to be invoked on a background thread. Use this to run slow
			//     tasks without affecting the game. NOTE: Most of Unity API can not be accessed
			//     while running on another thread!
			//
			// Parameters:
			//   action:
			//     Task to be executed on another thread. Can optionally return an Action that will
			//     be executed on the main thread. You can use this action to return results of
			//     your work safely. Return null if this is not needed.
			public void StartAsyncInvoke(Func<Action> action)
			{
				if(!ThreadPool.QueueUserWorkItem(DoWork))
				{
					throw new NotSupportedException("Failed to queue the action on ThreadPool");
				}

				void DoWork(object _)
				{
					try
					{
						Action action2 = action();
						if(action2 != null)
						{
							StartSyncInvoke(action2);
						}
					}
					catch(Exception ex)
					{
						LogInvocationException(ex);
					}
				}
			}

			private static void LogInvocationException(Exception ex)
			{
				ProloLogger.Log(LogLevel.Error, ex);
				if(ex.InnerException != null)
				{
					ProloLogger.Log(LogLevel.Error, "INNER: " + ex.InnerException);
				}
			}

			IAsyncResult ISynchronizeInvoke.BeginInvoke(Delegate method, object[] args)
			{
				InvokeResult result = new InvokeResult();
				if(!InvokeRequired)
				{
					result.Finish(Invoke(), completedSynchronously: true);
				}
				else
				{
					StartSyncInvoke(delegate
					{
						result.Finish(Invoke(), completedSynchronously: false);
					});
				}

				return result;
				object Invoke()
				{
					try
					{
						return method.DynamicInvoke(args);
					}
					catch(Exception result2)
					{
						result.ExceptionThrown = true;
						return result2;
					}
				}
			}

			object ISynchronizeInvoke.EndInvoke(IAsyncResult result)
			{
				InvokeResult invokeResult = (InvokeResult)result;
				invokeResult.AsyncWaitHandle.WaitOne();
				if(invokeResult.ExceptionThrown)
				{
					throw (Exception)invokeResult.AsyncState;
				}

				return invokeResult.AsyncState;
			}

			object ISynchronizeInvoke.Invoke(Delegate method, object[] args)
			{
				IAsyncResult result = ((ISynchronizeInvoke)this).BeginInvoke(method, args);
				return ((ISynchronizeInvoke)this).EndInvoke(result);
			}
		}

		//
		// Summary:
		//     Gives access to the Windows open file dialog. http://www.pinvoke.net/default.aspx/comdlg32/GetOpenFileName.html
		//     http://www.pinvoke.net/default.aspx/Structures/OpenFileName.html http://www.pinvoke.net/default.aspx/Enums/OpenSaveFileDialgueFlags.html
		//     https://social.msdn.microsoft.com/Forums/en-US/2f4dd95e-5c7b-4f48-adfc-44956b350f38/getopenfilename-for-multiple-files?forum=csharpgeneral
		public class OpenFileDialog
		{
			private static class NativeMethods
			{
				[DllImport("comdlg32.dll", CharSet = CharSet.Unicode)]
				public static extern bool GetOpenFileName([In][Out] OpenFileName ofn);

				[DllImport("user32.dll")]
				public static extern IntPtr GetActiveWindow();
			}

			[Flags]
			public enum OpenSaveFileDialgueFlags
			{
				OFN_READONLY = 1,
				OFN_OVERWRITEPROMPT = 2,
				OFN_HIDEREADONLY = 4,
				OFN_NOCHANGEDIR = 8,
				OFN_SHOWHELP = 0x10,
				OFN_ENABLEHOOK = 0x20,
				OFN_ENABLETEMPLATE = 0x40,
				OFN_ENABLETEMPLATEHANDLE = 0x80,
				OFN_NOVALIDATE = 0x100,
				OFN_ALLOWMULTISELECT = 0x200,
				OFN_EXTENSIONDIFFERENT = 0x400,
				OFN_PATHMUSTEXIST = 0x800,
				OFN_FILEMUSTEXIST = 0x1000,
				OFN_CREATEPROMPT = 0x2000,
				OFN_SHAREAWARE = 0x4000,
				OFN_NOREADONLYRETURN = 0x8000,
				OFN_NOTESTFILECREATE = 0x10000,
				OFN_NONETWORKBUTTON = 0x20000,
				//
				// Summary:
				//     Force no long names for 4.x modules
				OFN_NOLONGNAMES = 0x40000,
				//
				// Summary:
				//     New look commdlg
				OFN_EXPLORER = 0x80000,
				OFN_NODEREFERENCELINKS = 0x100000,
				//
				// Summary:
				//     Force long names for 3.x modules
				OFN_LONGNAMES = 0x200000
			}

			[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
			private class OpenFileName
			{
				public int structSize;

				public IntPtr dlgOwner = IntPtr.Zero;

				public IntPtr instance = IntPtr.Zero;

				public string filter;

				public string customFilter;

				public int maxCustFilter;

				public int filterIndex;

				public IntPtr file;

				public int maxFile;

				public string fileTitle;

				public int maxFileTitle;

				public string initialDir;

				public string title;

				public int flags;

				public short fileOffset;

				public short fileExtension;

				public string defExt;

				public IntPtr custData = IntPtr.Zero;

				public IntPtr hook = IntPtr.Zero;

				public string templateName;

				public IntPtr reservedPtr = IntPtr.Zero;

				public int reservedInt;

				public int flagsEx;
			}

			//
			// Summary:
			//     Arguments used for opening a single file
			public const OpenSaveFileDialgueFlags SingleFileFlags = OpenSaveFileDialgueFlags.OFN_NOCHANGEDIR | OpenSaveFileDialgueFlags.OFN_FILEMUSTEXIST | OpenSaveFileDialgueFlags.OFN_EXPLORER | OpenSaveFileDialgueFlags.OFN_LONGNAMES;

			//
			// Summary:
			//     Arguments used for opening multiple files
			public const OpenSaveFileDialgueFlags MultiFileFlags = OpenSaveFileDialgueFlags.OFN_NOCHANGEDIR | OpenSaveFileDialgueFlags.OFN_ALLOWMULTISELECT | OpenSaveFileDialgueFlags.OFN_FILEMUSTEXIST | OpenSaveFileDialgueFlags.OFN_EXPLORER | OpenSaveFileDialgueFlags.OFN_LONGNAMES;

			public static string[] ShowDialog(string title, string initialDir, string filter, string defaultExt, OpenSaveFileDialgueFlags flags, IntPtr owner = default(IntPtr))
			{
				return ShowDialog(title, initialDir, filter, defaultExt, flags, null, owner);
			}

			//
			// Summary:
			//     Show windows file open dialog. Blocks the thread until user closes the dialog.
			//     Returns list of selected files, or null if user cancelled the action.
			//
			// Parameters:
			//   title:
			//     A string to be placed in the title bar of the dialog box. If this member is NULL,
			//     the system uses the default title (that is, Save As or Open)
			//
			//   initialDir:
			//     The initial directory. The algorithm for selecting the initial directory varies
			//     on different platforms.
			//
			//   filter:
			//     A list of filter pairs separated by |. First item is the display name, while
			//     the second is the actual filter (e.g. *.txt) Example:
			//
			//     "Log files (.log)|*.log|All files|*.*"
			//
			//   defaultExt:
			//     The default extension. This extension is appended to the file name if the user
			//     fails to type an extension.
			//
			//   flags:
			//     A set of bit flags you can use to initialize the dialog box. When the dialog
			//     box returns, it sets these flags to indicate the user's input. This member can
			//     be a combination of the CommomDialgueFlags.
			//
			//   owner:
			//     Hwnd pointer of the owner window. IntPtr.Zero to use default parent
			//
			//   defaultFilename:
			//     Filename that is initially entered in the filename box.
			public static string[] ShowDialog(string title, string initialDir, string filter, string defaultExt, OpenSaveFileDialgueFlags flags, string defaultFilename, IntPtr owner = default(IntPtr))
			{
				OpenFileName openFileName = new OpenFileName();
				openFileName.structSize = Marshal.SizeOf(openFileName);
				openFileName.filter = filter.Replace("|", "\0") + "\0";
				openFileName.fileTitle = new string(new char[2048]);
				openFileName.maxFileTitle = openFileName.fileTitle.Length;
				openFileName.initialDir = initialDir;
				openFileName.title = title;
				openFileName.flags = (int)flags;
				if(defaultExt != null)
				{
					openFileName.defExt = defaultExt;
				}

				char[] array = new char[2048];
				defaultFilename?.CopyTo(0, array, 0, defaultFilename.Length);
				string text = new string(array);
				openFileName.file = Marshal.StringToBSTR(text);
				openFileName.maxFile = text.Length;
				if(owner == (IntPtr)0)
				{
					owner = NativeMethods.GetActiveWindow();
				}

				openFileName.dlgOwner = owner;
				bool runInBackground = Application.runInBackground;
				Application.runInBackground = false;
				string currentWorkingDirectory = Environment.CurrentDirectory;
				bool run = true;
				 
			  ThreadingHelper.Instance.StartAsyncInvoke(delegate
				{
					while(run)
					{
						Environment.CurrentDirectory = currentWorkingDirectory;
					}

					return null;
				});

				bool openFileName2 = NativeMethods.GetOpenFileName(openFileName);
				run = false;
				Environment.CurrentDirectory = currentWorkingDirectory;
				Application.runInBackground = runInBackground;
				if(openFileName2)
				{
					List<string> list = new List<string>();
					long num = (long)openFileName.file;
					string text2 = Marshal.PtrToStringAuto(openFileName.file);
					while(!string.IsNullOrEmpty(text2))
					{
						list.Add(text2);
						num += text2.Length * 2 + 2;
						openFileName.file = (IntPtr)num;
						text2 = Marshal.PtrToStringAuto(openFileName.file);
					}

					if(list.Count == 1)
					{
						return list.ToArray();
					}

					string[] array2 = new string[list.Count - 1];
					for(int i = 0; i < array2.Length; i++)
					{
						array2[i] = list[0] + "\\" + list[i + 1];
					}

					return array2;
				}

				return null;
			}

			//
			// Summary:
			//     Show windows file open dialog. Doesn't pause the game.
			//
			// Parameters:
			//   onAccept:
			//     Action that gets called with results of user's selection. Returns list of selected
			//     files, or null if user cancelled the action. WARNING: This runs on another thread!
			//     Game will crash if you attempt to access unity methods. You can use
			//
			//     KoikatuAPI.SynchronizedInvoke
			//
			//     to go back to the main thread.
			//
			//   title:
			//     A string to be placed in the title bar of the dialog box. If this member is NULL,
			//     the system uses the default title (that is, Save As or Open)
			//
			//   initialDir:
			//     The initial directory. The algorithm for selecting the initial directory varies
			//     on different platforms.
			//
			//   filter:
			//     A list of filter pairs separated by |. First item is the display name, while
			//     the second is the actual filter (e.g. *.txt) Example:
			//
			//     "Log files (.log)|*.log|All files|*.*"
			//
			//   defaultExt:
			//     The default extension. This extension is appended to the file name if the user
			//     fails to type an extension.
			//
			//   flags:
			//     A set of bit flags you can use to initialize the dialog box. When the dialog
			//     box returns, it sets these flags to indicate the user's input. This member can
			//     be a combination of the CommomDialgueFlags.
			public static void Show(Action<string[]> onAccept, string title, string initialDir, string filter, string defaultExt, OpenSaveFileDialgueFlags flags = OpenSaveFileDialgueFlags.OFN_NOCHANGEDIR | OpenSaveFileDialgueFlags.OFN_FILEMUSTEXIST | OpenSaveFileDialgueFlags.OFN_EXPLORER | OpenSaveFileDialgueFlags.OFN_LONGNAMES)
			{
				if(onAccept == null)
				{
					throw new ArgumentNullException("onAccept");
				}

				IntPtr handle = NativeMethods.GetActiveWindow();
				new Thread((ThreadStart)delegate
				{
					string[] obj = ShowDialog(title, initialDir, filter, defaultExt, flags, handle);
					onAccept(obj);
				}).Start();
			}
		}
#endif
    }


}
