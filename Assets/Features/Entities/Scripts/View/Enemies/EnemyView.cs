using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public class EnemyView : MonoBehaviour
	{
		private static readonly int MoveHash = Animator.StringToHash("Move");
		private static readonly int TakeDamageHash = Animator.StringToHash("TakeDamage");
		private static readonly int AttackHash = Animator.StringToHash("Attack");
		private static readonly int DieHash = Animator.StringToHash("Die");
		private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

		[SerializeField] private Animator animator;
		[SerializeField] private float rotationSpeed = 10f;
		[SerializeField] private float deathAnimationDuration = 1f;
		[SerializeField] private ParticleSystem deathParticlesPrefab;

		private HeroController _heroController;
		private EnemiesController _enemiesController;
		private EnemyHealthBarView _healthBarView;
		private int _id;
		private bool _isMoving;
		private Vector3 _targetPosition;
		private float _moveSpeed;
		private Renderer _renderer;
		private Color _originalColor;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;
		}

		private void OnServicesInitialized()
		{
			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
		}

		public void Initialize(int id, EnemiesController enemiesController, EnemyState initialState)
		{
			_id = id;
			_enemiesController = enemiesController;
			_targetPosition = transform.position;

			_renderer = GetComponentInChildren<Renderer>();
			if (_renderer != null)
				_originalColor = _renderer.material.GetColor(BaseColorId);

			_enemiesController.OnEnemyPositionChanged += OnEnemyPositionChanged;
			_enemiesController.OnEnemyAttacked += OnEnemyAttacked;
			_enemiesController.OnEnemyPhaseChanged += OnEnemyPhaseChanged;

			_healthBarView = GetComponentInChildren<EnemyHealthBarView>();
			_healthBarView?.Initialize(initialState, enemiesController);
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_enemiesController != null)
			{
				_enemiesController.OnEnemyPositionChanged -= OnEnemyPositionChanged;
				_enemiesController.OnEnemyHit -= OnEnemyHit;
				_enemiesController.OnEnemyAttacked -= OnEnemyAttacked;
				_enemiesController.OnEnemyPhaseChanged -= OnEnemyPhaseChanged;
			}
		}

		public void SetTargetPosition(Vector3 position, float speed)
		{
			_targetPosition = position;
			_moveSpeed = speed;
		}

		private void LateUpdate()
		{
			if (_heroController == null || _heroController.CurrentState.IsDead) return;

			transform.position = Vector3.MoveTowards(transform.position, _targetPosition, _moveSpeed * Time.deltaTime);

			Vector3 heroPosition = _heroController.CurrentState.Position;
			Vector3 direction = (heroPosition - transform.position).normalized;

			if (direction.sqrMagnitude > 0.01f)
			{
				Quaternion targetRotation = Quaternion.LookRotation(direction);
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
			}
		}

		private void OnEnemyPositionChanged(EnemyState state)
		{
			if (state.Id != _id || _isMoving) return;

			_isMoving = true;
			if (animator != null) animator.SetTrigger(MoveHash);
		}

		private void OnEnemyHit(int enemyId, Vector3 position)
		{
			if (enemyId != _id) return;

			_isMoving = false;
			if (animator != null) animator.SetTrigger(TakeDamageHash);
		}

		private void OnEnemyAttacked(int enemyId)
		{
			if (enemyId != _id) return;

			_isMoving = false;
			if (animator != null) animator.SetTrigger(AttackHash);
		}

		private void OnEnemyPhaseChanged(EnemyState state)
		{
			if (state.Id != _id || _renderer == null) return;

			Color color = state.Phase == LungePhase.Lunging ? Color.red : _originalColor;
			_renderer.material.SetColor(BaseColorId, color);
		}

		public void PlayDeath()
		{
			if (animator != null) animator.SetTrigger(DieHash);
			PlayDeathAsync().Forget();
		}

		private async UniTaskVoid PlayDeathAsync()
		{
			await UniTask.Delay(System.TimeSpan.FromSeconds(deathAnimationDuration));
			if (this == null) return;

			if (deathParticlesPrefab != null)
				Instantiate(deathParticlesPrefab, transform.position, Quaternion.identity);
			Destroy(gameObject);
		}
	}
}