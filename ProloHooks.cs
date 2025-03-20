using System;
using System.IO;
using System.Collections.Generic;
using System.Text;


using HarmonyLib;
using UnityEngine.Events;
using ProloAPI.Extensions;



#if IL2CPP
using Character;

#endif
namespace ProloAPI
{
	using static Utilities.PGeneral;

	internal static class ProloHooks
	{

#if IL2CPP

		private static Harmony harmony = null;
		internal static void InitHooks(this IProloPluginManager plugin)
		{
			if(harmony == null)
				harmony = Harmony.CreateAndPatchAll(typeof(ProloHooks), "prolo.api");
		}

		public class HumanCreatedEvent : UnityEvent<Human> { }
		public class HumanLoadedEvent : UnityEvent<HumanData> { }

		public static HumanCreatedEvent characterCreatedEvent { get; } = new HumanCreatedEvent();
		public static HumanLoadedEvent characterLoadedEvent { get; } = new HumanLoadedEvent();

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Human), nameof(Human.CreateCustom),
			new Type[] { typeof(HumanData), typeof(bool) })]
		public static void OnCharCreate(Human __result)
		=> characterCreatedEvent.Invoke(__result);

		[HarmonyPostfix]
		[HarmonyPatch(typeof(HumanData), nameof(HumanData.LoadFile),
			new Type[] { typeof(Il2CppSystem.IO.BinaryReader), typeof(HumanData.LoadFileInfo.Flags) })]
		public static void OnCharacterReload(HumanData __instance)
		=>
			characterLoadedEvent.Invoke(__instance);

		public static event Action OnGameStart;

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Manager.Game), nameof(Manager.Scene.Initialize))]
		public static void ONGameStart()
		{
			OnGameStart?.Invoke(); 
		}

		private static void ProloHooks_OnGameStart()
		{
			throw new NotImplementedException();
		}

#endif

	}

}
