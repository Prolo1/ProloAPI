using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

using MessagePack.Resolvers;

using MessagePack.Unity;

using UnityEngine;
using MessagePack;
#if IL2CPP
using System.Runtime.Serialization.Json;
#endif

namespace ProloAPI
{
    /// <summary>
    /// saves controls from current data. 
    /// Note: make a new one if variables change
    /// </summary>  
    public abstract class BaseSaveLoadManager : IDisposable
    {
        public int Version { get => -1; }
        public string[] DataKeys { get => new string[] { }; }
        public enum LoadDataType : int { }
        public IFormatterResolver FormatterResolver { get; }

        public BaseSaveLoadManager()
        {
#if IL2CPP

			FormatterResolver =	CompositeResolver.Create(
				UnityResolver.Instance.Cast<IFormatterResolver>(),
				StandardResolver.Instance.Cast<IFormatterResolver>(),
				BuiltinResolver.Instance.Cast<IFormatterResolver>(),
				//default resolver
				ContractlessStandardResolver.Instance.Cast<IFormatterResolver>());

#else
            CompositeResolver.Register(
                UnityResolver.Instance,
                StandardResolver.Instance,
                BuiltinResolver.Instance,
                //default resolver
                ContractlessStandardResolver.Instance
                );
            FormatterResolver = CompositeResolver.Instance;
#endif
            Managers.Add(this);
        }

        ~BaseSaveLoadManager()
        {
            Dispose();
        }

        public static List<BaseSaveLoadManager> Managers { get; } = new List<BaseSaveLoadManager>();

        // Convert an object to a byte array
        public static byte[] ObjectToByteArray<T>(T obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using(var ms = new MemoryStream())
            {
#if IL2CPP
				DataContractJsonSerializer data = new DataContractJsonSerializer(typeof(T));
				data.WriteObject(ms, obj);
				return ms.ToArray();
#else
                bf.Serialize(ms, obj);
                return ms.ToArray();
#endif
            }
        }

        public static T ByteArrayToObject<T>(byte[] arr)
        {
            BinaryFormatter bf = new BinaryFormatter();
            T obj;
            using(var ms = new MemoryStream())
            {
                ms.Write(arr, 0, arr.Length);

#if IL2CPP
				DataContractJsonSerializer data = new DataContractJsonSerializer(typeof(T));
				obj = (T)data.ReadObject(ms);
#else
                obj = (T)bf.Deserialize(ms);
#endif

                return obj;
            }
        }

        public virtual object Load(object ctrler, object data = null) => throw new NotImplementedException();

        public virtual object Save(object ctrler, object data = null) => throw new NotImplementedException();

        protected virtual object UpdateVersionFromPrev(object ctrler, object data) => throw new NotImplementedException();

        public void Dispose()
        {
            Managers.Remove(this);
        }
    }

    public abstract class SaveLoadManager<TCtrler, TData> : BaseSaveLoadManager where TData : class
    {
        public new int Version => base.Version;
        public new string[] DataKeys => base.DataKeys;

        protected virtual TData UpdateVersionFromPrev(TCtrler ctrler, TData data) => (TData)base.UpdateVersionFromPrev(ctrler, data);

        public virtual TData Load(TCtrler ctrler, TData data = null) => (TData)base.Load(ctrler, data);

        public virtual TData Save(TCtrler ctrler, TData data = null) => (TData)base.Save(ctrler, data);

    }

}
