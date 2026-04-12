using System;
using System.Threading;
using Game.GamePlay.Enemies;
using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
	public class GameOverOverlayView : MonoBehaviour
	{
		[SerializeField] private Button restartButton;
		[SerializeField] private float showDelay = 1.5f;
		[SerializeField] private float showDuration = 0.4f;

		private HeroController _heroController;
		private EnemiesController _enemiesController;
		private CancellationTokenSource _cts;

		private void Start()
		{
			gameObject.SetActive(false);
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			EntitiesService entitiesService = ServicesLocator.Instance.GetService<EntitiesService>();
			_heroController = entitiesService.HeroController;
			_enemiesController = entitiesService.EnemiesController;

			_heroController.OnDied += OnHeroDied;

			if (restartButton != null)
				restartButton.onClick.AddListener(OnRestartButtonClicked);
		}

		private void OnHeroDied()
		{
			_cts?.Cancel();
			_cts?.Dispose();
			_cts = new CancellationTokenSource();
			ShowAsync(_cts.Token).Forget();
		}

		private async UniTaskVoid ShowAsync(CancellationToken cancellationToken)
		{
			await UniTask.Delay(TimeSpan.FromSeconds(showDelay), cancellationToken: cancellationToken);

			gameObject.SetActive(true);
			transform.localScale = Vector3.zero;

			float elapsed = 0f;
			while (elapsed < showDuration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / showDuration);
				float scale = Mathf.Sin(t * Mathf.PI * 0.5f);
				transform.localScale = Vector3.one * scale;
				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}

			transform.localScale = Vector3.one;
		}

		private void OnRestartButtonClicked()
		{
			_cts?.Cancel();
			_cts?.Dispose();
			_cts = null;

			gameObject.SetActive(false);
			_enemiesController.ClearAllEnemies();
			_heroController.Restart();
		}

		private void OnDestroy()
		{
			_cts?.Cancel();
			_cts?.Dispose();

			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_heroController != null)
				_heroController.OnDied -= OnHeroDied;
			if (restartButton != null)
				restartButton.onClick.RemoveListener(OnRestartButtonClicked);
		}
	}
}