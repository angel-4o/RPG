using UnityEngine;

namespace Core
{
	public class AutoDestroyComponent : MonoBehaviour
	{
		[SerializeField] private float lifetime = 3f;

		private void Start()
		{
			Destroy(gameObject, lifetime);
		}
	}
}