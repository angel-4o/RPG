using Core.ServicesManager;
using Game.GamePlay.Entities;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public class HitParticlesView : MonoBehaviour
	{
		[SerializeField] private ParticleSystem hitParticlePrefab;

		private EnemiesController _enemiesController;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_enemiesController = ServicesLocator.Instance.GetService<EntitiesService>().EnemiesController;
			_enemiesController.OnEnemyHit += OnEnemyHit;
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_enemiesController != null)
				_enemiesController.OnEnemyHit -= OnEnemyHit;
		}

		private void OnEnemyHit(int enemyId, Vector3 position)
		{
			if (hitParticlePrefab == null) return;

			ParticleSystem ps = Instantiate(hitParticlePrefab, position, Quaternion.identity);
			ps.Play();
		}
	}
}