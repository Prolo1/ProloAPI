using UnityEngine.Events;
using UnityEngine;

#if IL2CPP
using ILLGames.Unity.Component;
#endif
public class ProloGUIBehaviour<T> : Singleton<T>
	where T : MonoBehaviour
{
	public UnityEvent guiEvent { get; } = new UnityEvent();

	void OnGUI() => guiEvent?.Invoke();


}