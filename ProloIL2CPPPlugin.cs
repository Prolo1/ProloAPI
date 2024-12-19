using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

using BepInEx;
using BepInEx.Logging;

#if IL2CPP
using BepInEx.Unity.IL2CPP;

using Character_Morpher_IL2CPP;

//using Il2CppSystem.Linq;



#endif

using UnityEngine;
using UnityEngine.Events;

namespace ProloAPI
{
#if IL2CPP

	public interface IProloPluginManager 
	{
		public BepInPlugin Metadata { get; }
		public ProloBaseUnityPluginIL2CPP Instance { get; }
		public ManualLogSource Log { get; }
		public BasePlugin Plugin { get; }
		  
	}
	public abstract class ProloPluginManager<T1> : BasePlugin, IProloPluginManager where T1 : ProloUnityPluginIL2CPP<T1>
	{
		public ProloPluginManager()
		{
			m_metadata = MetadataHelper.GetMetadata(this);
			m_instance = AddComponent<ProloUnityPluginIL2CPP<T1>>();
			m_instance.manager = this;
			m_plugin = this;
		}

		BepInPlugin m_metadata;
		public BepInPlugin Metadata => m_metadata;

		ProloBaseUnityPluginIL2CPP m_instance;
		public ProloBaseUnityPluginIL2CPP Instance => m_instance;

		BasePlugin m_plugin;
		public BasePlugin Plugin => m_plugin;
	}

	public abstract class ProloBaseUnityPluginIL2CPP : MonoBehaviour
	{

		public struct ProloInfo
		{
			public string ModName;
			public string GUID;//never change this
			public string Version;

			public override string ToString() => $"{ModName} : {GUID} : {Version}";
		}

		protected ProloBaseUnityPluginIL2CPP()
		{
			ProInfo = new ProloInfo { ModName = manager?.Metadata.Name ?? "", GUID = manager?.Metadata.GUID ?? "", Version = manager?.Metadata.Version.ToString() ?? "" };
			Instance = (manager?.Instance ?? this);
			Instances.Add(Instance);

		}

		~ProloBaseUnityPluginIL2CPP() => Instances.Remove(Instance);



		internal IProloPluginManager manager;
		public ProloInfo ProInfo { get; }
		public ProloBaseUnityPluginIL2CPP Instance { get; }
		public ManualLogSource Logger { get => manager.Log; }

		public static HashSet<ProloBaseUnityPluginIL2CPP> Instances { get; } = new HashSet<ProloBaseUnityPluginIL2CPP>();


	}

	public abstract class ProloUnityPluginIL2CPP<T1> : ProloBaseUnityPluginIL2CPP where T1 : ProloUnityPluginIL2CPP<T1>
	{
		public ProloUnityPluginIL2CPP()
		{
			ForeGrounder.SetCurrentForground();
			Instance = Instance ?? (T1)base.Instance ?? (T1)this;
			Logger = base.Logger;
		}

		public static new T1 Instance { get; private set; }
		public static new ManualLogSource Logger { get; private set; }

	}
#endif

}
