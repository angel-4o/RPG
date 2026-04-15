using Core.ServicesManager;
using DG.Tweening;
using Game.GamePlay.Enemies;
using Game.GamePlay.Entities;
using TMPro;
using UnityEngine;

namespace Game.UI
{
	public class HUDView : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI scoreText;

		private EnemiesController _enemiesController;
		private int _score;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
			UpdateScoreText();
		}

		private void OnServicesInitialized()
		{
			_enemiesController = ServicesLocator.Instance.GetService<EntitiesService>().EnemiesController;
			_enemiesController.OnEnemyDied += OnEnemyDied;
		}

		private void OnEnemyDied()
		{
			_score++;
			UpdateScoreText();

			if (scoreText != null)
			{
				scoreText.transform.DOKill();
				scoreText.transform.DOPunchScale(Vector3.one * 0.4f, 0.3f, 5, 0.5f);
			}
		}

		private void UpdateScoreText()
		{
			if (scoreText != null)
				scoreText.text = _score.ToString();
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_enemiesController != null)
				_enemiesController.OnEnemyDied -= OnEnemyDied;
		}
	}
}