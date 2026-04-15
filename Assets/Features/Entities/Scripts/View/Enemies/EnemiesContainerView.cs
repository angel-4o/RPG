using System.Collections.Generic;
using Game.GamePlay.Entities;
using Core.ServicesManager;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public class EnemiesContainerView : MonoBehaviour
	{
		private EnemiesController _enemiesController;
		private Dictionary<int, EnemyView> _enemyViews;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_enemiesController = ServicesLocator.Instance.GetService<EntitiesService>().EnemiesController;
			_enemyViews = new Dictionary<int, EnemyView>();

			_enemiesController.OnEnemySpawned += OnEnemySpawned;
			_enemiesController.OnEnemyRemoved += OnEnemyRemoved;
			_enemiesController.OnEnemyPositionChanged += OnEnemyPositionChanged;
		}

		private void OnEnemySpawned(EnemyState enemyState)
		{
			EnemyView enemyView = Instantiate(enemyState.Config.Prefab, transform);
			enemyView.transform.position = enemyState.Position;
			enemyView.Initialize(enemyState.Id, _enemiesController, enemyState);
			_enemyViews[enemyState.Id] = enemyView;
		}

		private void OnEnemyRemoved(int enemyId)
		{
			if (_enemyViews.Remove(enemyId, out EnemyView enemyView))
			{
				enemyView.PlayDeath();
			}
		}

		private void OnEnemyPositionChanged(EnemyState enemyState)
		{
			if (_enemyViews.TryGetValue(enemyState.Id, out EnemyView enemyView))
			{
				float speed = enemyState.Phase == LungePhase.Lunging
					? enemyState.Config.LungeSpeed
					: enemyState.Config.Speed;
				enemyView.SetTargetPosition(enemyState.Position, speed);
			}
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_enemiesController != null)
			{
				_enemiesController.OnEnemySpawned -= OnEnemySpawned;
				_enemiesController.OnEnemyRemoved -= OnEnemyRemoved;
				_enemiesController.OnEnemyPositionChanged -= OnEnemyPositionChanged;
			}
		}
	}
}