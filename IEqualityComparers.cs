using System;
using System.Collections.Generic;
using System.Text;

using BepInEx.Configuration;

namespace ProloAPI
{

	public class StringComparer : IEqualityComparer<string>
	{
		public bool Equals(string x, string y) => x == y;

		public int GetHashCode(string obj) => obj.GetHashCode();

	}

	public class ConfigDefinitionComparer : IEqualityComparer<ConfigDefinition>
	{
		//this is probably done somewhere but here for future reference 
		public bool Equals(ConfigDefinition x, ConfigDefinition y)
		{
			return x.Key == y.Key;
		}

		public int GetHashCode(ConfigDefinition obj) => obj.GetHashCode();
	}

	public class EqualityComparer<T> : IEqualityComparer<T>
	{
		public EqualityComparer(Func<T, T, bool> equals = null, Func<T, int> getHashCode = null)
		{
			this.equals = equals;
			this.getHashCode = getHashCode;
		}

		public bool Equals(T x, T y)
		{
			return equals?.Invoke(x, y) ?? x.Equals(y);
		}

		public int GetHashCode(T obj)
		{
			return getHashCode?.Invoke(obj) ?? obj.GetHashCode();
		}

		private readonly Func<T, T, bool> equals;

		private readonly Func<T, int> getHashCode;
	}

}
