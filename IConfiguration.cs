using System;
using System.Collections.Generic;
using System.Text;

using BepInEx.Configuration;

namespace ProloAPI
{
	public interface IConfiguration
	{
		//Main
		ConfigEntry<bool> enable { set; get; }

		//Advanced
		ConfigEntry<bool> resetOnLaunch { set; get; }
		ConfigEntry<bool> debug { set; get; }
		//ConfigEntry<float> makerViewportUISpace { set; get; }
	}
}
