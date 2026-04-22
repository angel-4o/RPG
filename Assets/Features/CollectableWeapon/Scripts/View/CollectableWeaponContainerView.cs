using System.Collections.Generic;
using Core.ServicesManager;
using UnityEngine;

namespace Game.CollectableWeapon
{
	public class CollectableWeaponContainerView : MonoBehaviour
	{
		[SerializeField] private CollectableWeaponView prefab;

		private CollectableWeaponController _controller;
		private Dictionary<int, CollectableWeaponView> _views;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_controller = ServicesLocator.Instance.GetService<CollectableWeaponService>().CollectableWeaponController;
			_views = new Dictionary<int, CollectableWeaponView>();

			_controller.OnSpawned += OnSpawned;
			_controller.OnRemoved += OnRemoved;
		}

		private void OnSpawned(CollectableWeaponState state)
		{
			if (prefab == null) return;

			CollectableWeaponView view = Instantiate(prefab, transform);
			view.transform.position = state.Position;
			view.Initialize(state.Id, _controller);
			_views[state.Id] = view;
		}

		private void OnRemoved(int id)
		{
			if (_views.Remove(id, out CollectableWeaponView view) && view != null)
				Destroy(view.gameObject);
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_controller != null)
			{
				_controller.OnSpawned -= OnSpawned;
				_controller.OnRemoved -= OnRemoved;
			}
		}
	}
}
