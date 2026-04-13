using System.Threading;
using Cinemachine;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Enemies;
using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
	public class CombatFeedbackView : MonoBehaviour
	{
		[Header("Camera Shake")]
		[SerializeField] private CinemachineImpulseSource impulseSource;
		[SerializeField] private float attackBaseForce = 0.3f;
		[SerializeField] private float attackMaxForce = 1f;
		[SerializeField] private float hitForce = 0.5f;
		[SerializeField] private float dieForce = 1.5f;
		[SerializeField] private float enemyDieForce = 0.2f;

		[Header("Hit Flash")]
		[SerializeField] private Image hitFlashImage;
		[SerializeField] private float flashMaxAlpha = 0.4f;
		[SerializeField] private float flashDuration = 0.25f;

		private HeroController _heroController;
		private EnemiesController _enemiesController;
		private CancellationTokenSource _flashCts;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;

			if (hitFlashImage != null)
				hitFlashImage.color = new Color(1f, 0f, 0f, 0f);
		}

		private void OnServicesInitialized()
		{
			EntitiesService entitiesService = ServicesLocator.Instance.GetService<EntitiesService>();
			_heroController = entitiesService.HeroController;
			_enemiesController = entitiesService.EnemiesController;

			_heroController.OnAttacked += OnHeroAttacked;
			_heroController.OnHit += OnHeroHit;
			_heroController.OnDied += OnHeroDied;
			_enemiesController.OnEnemyDied += OnEnemyDied;
		}

		private void OnDestroy()
		{
			_flashCts?.Cancel();
			_flashCts?.Dispose();

			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_heroController != null)
			{
				_heroController.OnAttacked -= OnHeroAttacked;
				_heroController.OnHit -= OnHeroHit;
				_heroController.OnDied -= OnHeroDied;
			}
			if (_enemiesController != null)
				_enemiesController.OnEnemyDied -= OnEnemyDied;
		}

		private void OnHeroAttacked()
		{
			float force = Mathf.Lerp(attackBaseForce, attackMaxForce, _heroController.CurrentChargeRatio);
			impulseSource.GenerateImpulse(force);
		}

		private void OnHeroHit()
		{
			impulseSource.GenerateImpulse(hitForce);
			TriggerHitFlash();
		}

		private void OnHeroDied() => impulseSource.GenerateImpulse(dieForce);
		private void OnEnemyDied() => impulseSource.GenerateImpulse(enemyDieForce);

		private void TriggerHitFlash()
		{
			if (hitFlashImage == null) return;

			_flashCts?.Cancel();
			_flashCts?.Dispose();
			_flashCts = new CancellationTokenSource();
			HitFlashAsync(_flashCts.Token).Forget();
		}

		private async UniTaskVoid HitFlashAsync(CancellationToken cancellationToken)
		{
			hitFlashImage.color = new Color(1f, 0f, 0f, flashMaxAlpha);

			float elapsed = 0f;
			while (elapsed < flashDuration)
			{
				elapsed += Time.deltaTime;
				float alpha = Mathf.Lerp(flashMaxAlpha, 0f, elapsed / flashDuration);
				hitFlashImage.color = new Color(1f, 0f, 0f, alpha);
				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}

			hitFlashImage.color = new Color(1f, 0f, 0f, 0f);
		}
	}
}