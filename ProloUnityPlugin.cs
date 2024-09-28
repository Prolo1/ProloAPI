

using System;
using System.Collections.Generic;

using BepInEx;
using BepInEx.Logging;

using ProloAPI.Extensions;

using UnityEngine.Events;

using static Illusion.Utils;

namespace ProloAPI
{
	public abstract class ProloBaseUnityPlugin : BaseUnityPlugin
	{
		public struct ProloInfo
		{
			public string ModName;
			public string GUID;//never change this
			public string Version;

			public override string ToString() => $"{ModName} : {GUID} : {Version}";
		}

		protected ProloBaseUnityPlugin()
		{
			ProInfo = new ProloInfo { ModName = Info.Metadata.Name, GUID = Info.Metadata.GUID, Version = Info.Metadata.Version.ToString() };
			Instance = (ProloBaseUnityPlugin)Info.Instance ?? this;
			Instances.Remove(Instance);
			Instances.Add(Instance);
		}

		~ProloBaseUnityPlugin() => Instances.Remove(Instance);

		public ProloInfo ProInfo { get; }
		public ProloBaseUnityPlugin Instance { get; }
		public new ManualLogSource Logger { get => base.Logger; }

		public static ICollection<ProloBaseUnityPlugin> Instances { get; } = new List<ProloBaseUnityPlugin>();

	}

	public abstract class ProloUnityPlugin<T1> : ProloBaseUnityPlugin where T1 : ProloUnityPlugin<T1>
	{
		protected ProloUnityPlugin()
		{
			ForeGrounder.SetCurrentForground();
			Instance = Instance ?? (T1)base.Instance ?? (T1)this;
			Logger = base.Logger;
		}

		public static new T1 Instance { get; private set; }
		public static new ManualLogSource Logger { get; private set; }

	}

}