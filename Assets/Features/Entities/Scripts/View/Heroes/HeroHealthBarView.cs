using Core.ServicesManager;
using Game.GamePlay.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay.Heroes
{
	public class HeroHealthBarView : MonoBehaviour
	{
		[SerializeField] private Image healthFillImage;

		private HeroController _heroController;
		private Camera _camera;

		private void Start()
		{
			_camera = Camera.main;
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
			_heroController.OnStateChanged += OnHeroStateChanged;
			_heroController.OnDied += OnHeroDied;
			OnHeroStateChanged(_heroController.CurrentState);
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_heroController != null)
			{
				_heroController.OnStateChanged -= OnHeroStateChanged;
				_heroController.OnDied -= OnHeroDied;
			}
		}

		private void LateUpdate()
		{
			if (_camera == null) return;
			transform.rotation = _camera.transform.rotation;
		}

		private void OnHeroStateChanged(HeroState state)
		{
			if (healthFillImage == null) return;
			healthFillImage.fillAmount = (float)state.Health / HeroConfig.Instance.InitialHealth;
		}

		private void OnHeroDied()
		{
			gameObject.SetActive(false);
		}
	}
}