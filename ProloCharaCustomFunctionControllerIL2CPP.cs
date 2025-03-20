using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

#if IL2CPP
using Character;
#endif

namespace ProloAPI
{
#if IL2CPP
	public abstract class ProloCharaCustomFunctionControllerIL2CPP : MonoBehaviour
	{
		public static List<ProloCharaCustomFunctionControllerIL2CPP> list { get; } = new List<ProloCharaCustomFunctionControllerIL2CPP>();

		public Human Human { get; private set; } = null;
		public HumanData HumanData { get; private set; } = null;
		public HumanComponent HumanComponent { get; private set; } = null;

		private Action<HumanData> reload = null;
		protected virtual void Awake()
		{
			list.Add(this);
			HumanComponent = GetComponent<HumanComponent>();
			Human = HumanComponent?.Human;
			HumanData = Human?.data;

			ProloHooks.characterLoadedEvent.AddListener(reload = new Action<HumanData>(h => { if(h == HumanData && HumanComponent.isActiveAndEnabled) OnReload(); }));
		}

		protected abstract void OnReload();

		protected virtual void OnDestroy()
		{
			list.Remove(this);
			ProloHooks.characterLoadedEvent.RemoveListener(reload);
		}
	}
#endif
}
