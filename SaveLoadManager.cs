using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using MessagePack.Resolvers;

using MessagePack.Unity;

using UnityEngine;

namespace ProloAPI
{
	/// <summary>
	/// saves controls from current data. 
	/// Note: make a new one if variables change
	/// </summary>  
	public abstract class BaseSaveLoadManager
	{
		public int Version { get => -1; }
		public string[] DataKeys { get => new string[] { }; }
		public enum LoadDataType : int { }

		public BaseSaveLoadManager()
		{
			CompositeResolver.Register(
				UnityResolver.Instance,
				StandardResolver.Instance,
				BuiltinResolver.Instance,
				//default resolver
				ContractlessStandardResolver.Instance
				);

			Managers.Add(this);
		}

		~BaseSaveLoadManager()
		{
			Managers.Remove(this);
		}

		public static List<BaseSaveLoadManager> Managers { get; } = new List<BaseSaveLoadManager>();

		// Convert an object to a byte array
		public static byte[] ObjectToByteArray(object obj)
		{
			BinaryFormatter bf = new BinaryFormatter();
			using(var ms = new MemoryStream())
			{
				bf.Serialize(ms, obj);
				return ms.ToArray();
			}
		}

		public static T1 ByteArrayToObject<T1>(byte[] arr)
		{
			BinaryFormatter bf = new BinaryFormatter();
			using(var ms = new MemoryStream())
			{
				ms.Write(arr, 0, arr.Length);
				T1 obj = (T1)bf.Deserialize(ms);
				return obj;
			}
		}

		public virtual object Load(object ctrler, object data = null) => throw new NotImplementedException();

		public virtual object Save(object ctrler, object data = null) => throw new NotImplementedException();

		protected virtual object UpdateVersionFromPrev(object ctrler, object data) => throw new NotImplementedException();
	}

	public abstract class SaveLoadManager<TCtrler, TData> : BaseSaveLoadManager where TData : class
	{
		public new int Version => base.Version;
		public new string[] DataKeys => base.DataKeys;


		protected virtual TData UpdateVersionFromPrev(TCtrler ctrler, TData data) => (TData)base.UpdateVersionFromPrev(ctrler, data);


		/// <summary>
		/// DO NOT OVERRIDE THIS FUNCTION. USE <see cref="Load(TCtrler, TData)"/> 
		/// </summary> 
		/// <param name="ctrler"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public override object Load(object ctrler, object data = null) => Load((TCtrler)ctrler,(TData)data);
		
		/// <summary>
		/// DO NOT OVERRIDE THIS FUNCTION. USE <see cref="Save(TCtrler, TData)"/> 
		/// </summary> 
		/// <param name="ctrler"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		public override object Save(object ctrler, object data = null) => Save((TCtrler)ctrler, (TData)data);


		public virtual TData Load(TCtrler ctrler, TData data = null) => (TData)base.Load(ctrler, data);

		public virtual TData Save(TCtrler ctrler, TData data = null) => (TData)base.Save(ctrler, data);

	}


	public partial class CurrentSaveLoadManager { }
}
