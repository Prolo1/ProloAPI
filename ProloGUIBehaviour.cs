using UnityEngine.Events;
using UnityEngine;

public class ProloGUIBehaviour<T> : Singleton<T>
	where T : MonoBehaviour
{
	public UnityEvent guiEvent { get; } = new UnityEvent();

	void OnGUI()
	{

		guiEvent?.Invoke();

	}
}