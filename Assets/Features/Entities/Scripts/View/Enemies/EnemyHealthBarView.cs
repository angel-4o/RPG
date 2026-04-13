using UnityEngine;
using UnityEngine.UI;

namespace Game.GamePlay.Enemies
{
	public class EnemyHealthBarView : MonoBehaviour
	{
		[SerializeField] private Image healthFillImage;

		private EnemiesController _enemiesController;
		private Camera _camera;
		private int _id;
		private int _maxHealth;

		private void Start()
		{
			_camera = Camera.main;
		}

		public void Initialize(EnemyState initialState, EnemiesController enemiesController)
		{
			_id = initialState.Id;
			_maxHealth = initialState.Config.InitialHealth;
			_enemiesController = enemiesController;

			_enemiesController.OnEnemyHealthChanged += OnEnemyHealthChanged;
			_enemiesController.OnEnemyRemoved += OnEnemyRemoved;

			UpdateFill(initialState.Health);
		}

		private void OnDestroy()
		{
			if (_enemiesController != null)
			{
				_enemiesController.OnEnemyHealthChanged -= OnEnemyHealthChanged;
				_enemiesController.OnEnemyRemoved -= OnEnemyRemoved;
			}
		}

		private void LateUpdate()
		{
			if (_camera == null) return;
			transform.rotation = _camera.transform.rotation;
		}

		private void OnEnemyHealthChanged(EnemyState state)
		{
			if (state.Id != _id) return;
			UpdateFill(state.Health);
		}

		private void OnEnemyRemoved(int enemyId)
		{
			if (enemyId != _id) return;
			gameObject.SetActive(false);
		}

		private void UpdateFill(int health)
		{
			if (healthFillImage == null) return;
			healthFillImage.fillAmount = (float)health / _maxHealth;
		}
	}
}