

using System;
using System.Collections.Generic;

using BepInEx;
using BepInEx.Logging;

using ProloAPI.Extentions;

using UnityEngine.Events;

using static Illusion.Utils;

namespace ProloAPI
{
	public abstract class ProloBaseUnityPlugin : BaseUnityPlugin
	{

		protected ProloBaseUnityPlugin()
		{
			ProInfo = new ProloInfo { ModName = Info.Metadata.Name, GUID = Info.Metadata.GUID, Version = Info.Metadata.Version.ToString() };
			Instance = (ProloBaseUnityPlugin)Info.Instance ?? this;
			Instances.Remove(Instance);
			Instances.Add(Instance);
		}

		~ProloBaseUnityPlugin() => Instances.Remove(Instance);

		public struct ProloInfo
		{
			public string ModName;
			public string GUID;//never change this
			public string Version;

			public override string ToString() => $"{ModName} : {GUID} : {Version}";
		}

		public ProloInfo ProInfo { get; }
		public ProloBaseUnityPlugin Instance { get; }
		public new ManualLogSource Logger { get => base.Logger; }
		//private IConfiguration _cfg = null;
		//public IConfiguration cfg { get => _cfg; set => _cfg = value; }

		public static ICollection<ProloBaseUnityPlugin> Instances { get; } = new List<ProloBaseUnityPlugin>();

	}

	public abstract class ProloUnityPlugin<T1, T2> : ProloBaseUnityPlugin where T1 : ProloUnityPlugin<T1, T2> where T2 : IConfiguration
	{
		protected ProloUnityPlugin()
		{
			//	Debug.Log("Logging does work so that is good");
			ForeGrounder.SetCurrentForground();
			Instance = Instance ?? (T1)base.Instance ?? (T1)this;
			Logger = base.Logger;

			//instUpdate.RemoveAllListeners();
			//instUpdate.AddListener(() => Utilities.Util_General.SetApiInst(this));
			//instUpdate.Invoke();
		}

		//private UnityEvent instUpdate = new UnityEvent();
		public static new T1 Instance { get; private set; }
		public static new ManualLogSource Logger { get; private set; }
		//public new T2 cfg
		//{
		//	get => (T2)base.cfg;
		//	set
		//	{
		//		base.cfg = value;
		//		Instance.instUpdate.Invoke();
		//	}
		//}



	}

}