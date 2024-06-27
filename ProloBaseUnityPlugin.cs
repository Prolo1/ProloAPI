

using BepInEx;
using BepInEx.Logging;

namespace ProloAPI
{
	public class ProloBaseUnityPlugin : BaseUnityPlugin
	{

		public class ProloInfoBase {
			public string ModName;
			public string GUID;//never change this
			public string Version;

			public ManualLogSource Logger;

			public IConfiguration cfg;

			internal ProloBaseUnityPlugin instance { get;   set; }
		
		}
		public class ProloInfo<T>:ProloInfoBase where T : ProloBaseUnityPlugin
		{

			public T Instance { get => (T)instance; set => instance = value; }
		}

		public   static ProloInfoBase PInfoBase { get; protected set; }
	}

	public class ProloUnityPlugin<T> : ProloBaseUnityPlugin where T : ProloBaseUnityPlugin
	{

		public static ProloInfo<T> PInfo { get=> (ProloInfo<T>)PInfoBase; protected set=>PInfoBase=value; }

	}

}