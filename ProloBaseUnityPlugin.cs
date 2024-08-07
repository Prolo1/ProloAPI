

using System.Collections.Generic;

using BepInEx;
using BepInEx.Logging;

using ProloAPI.Extentions;

namespace ProloAPI
{
	public abstract class ProloBaseUnityPlugin : BaseUnityPlugin
	{

		public class ProloInfo
		{
			public string ModName;
			public string GUID;//never change this
			public string Version;
		}

		public ProloInfo info { get; protected set; }

		public static ICollection<ILogSource> Loggers { get => BepInEx.Logging.Logger.Sources; }
		public static ICollection<ProloBaseUnityPlugin> Instances { get; } = new List<ProloBaseUnityPlugin>();
		public IConfiguration cfg { get; protected set; }





	}

	public abstract class ProloUnityPlugin<T1, T2> : ProloBaseUnityPlugin where T1 : ProloBaseUnityPlugin where T2 : IConfiguration
	{
		public static T1 Instance { get => (T1)Instances.FirstOrNull(t => t is T1); protected set { Instances.Remove(Instance); Instances.Add(value); } }
		public new T2 cfg { get => (T2)base.cfg; protected set { base.cfg = value; } }
		
		private ManualLogSource _logger = null;
		public new ManualLogSource Logger { get => _logger = _logger ?? 
				(ManualLogSource)Loggers.FirstOrNull(t => t.SourceName == MetadataHelper.GetMetadata(Instance).Name); }


	}

}