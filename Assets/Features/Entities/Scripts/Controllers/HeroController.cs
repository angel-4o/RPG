using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Enemies;
using Game.JoystickInput;
using Game.Weapons;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
	public class HeroController
	{
		// Services
		private EnemiesController _enemiesController;
		private JoystickInputService _joystickInputService;
		private WeaponsService _weaponsService;

		// Internal State
		private CancellationTokenSource _cancellationTokenSource;
		private HeroState _currentState;
		private List<EnemyState> _pendingAttackTargets;

		// Public State
		public HeroState CurrentState => _currentState;

		// Events
		public event Action<HeroState> OnStateChanged;
		public event Action OnAttacked;
		public event Action OnDied;

		public UniTask<bool> Initialize(EnemiesController enemiesController, JoystickInputService joystickInputService, WeaponsService weaponsService)
		{
			_enemiesController = enemiesController;
			_joystickInputService = joystickInputService;
			_weaponsService = weaponsService;

			_currentState = new HeroState(Vector3.zero, HeroConfig.Instance.InitialHealth, 0f, Vector3.forward);
			_cancellationTokenSource = new CancellationTokenSource();

			UpdateLoop(_cancellationTokenSource.Token).Forget();

			return UniTask.FromResult(true);
		}

		public void TakeHit(int damage)
		{
			if (_currentState.IsDead) return;

			int newHealth = Mathf.Max(0, _currentState.Health - damage);
			Debug.Log($"Hero is taking a hit. Health : {_currentState.Health} -> {newHealth}");
			_currentState = new HeroState(_currentState.Position, newHealth, _currentState.LastAttackTime, _currentState.FacingDirection);
			OnStateChanged?.Invoke(_currentState);

			if (_currentState.IsDead)
			{
				Debug.Log("Hero is dead!");
				OnDied?.Invoke();
			}
		}

		public void Restart()
		{
			_currentState = new HeroState(Vector3.zero, HeroConfig.Instance.InitialHealth, 0f, Vector3.forward);
			OnStateChanged?.Invoke(_currentState);
		}

		public UniTask Reset()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();

			return UniTask.CompletedTask;
		}

		private async UniTaskVoid UpdateLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				if (!_currentState.IsDead)
				{
					if (_joystickInputService.CurrentState.IsActive) UpdatePosition();
					else AttackClosestEnemy();
				}
				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}
		}

		private void UpdatePosition()
		{
			Vector2 currentMovementInput = _joystickInputService.CurrentState.IsActive ? _joystickInputService.CurrentState.MovementVector : Vector2.zero;
			if (currentMovementInput.sqrMagnitude <= 0.01f) return;

			Vector3 movement = new Vector3(-currentMovementInput.x, 0f, -currentMovementInput.y);
			Vector3 newPosition = _currentState.Position + movement * (HeroConfig.Instance.MoveSpeed * Time.deltaTime);

			_currentState = new HeroState(newPosition, _currentState.Health, _currentState.LastAttackTime, movement.normalized);
			OnStateChanged?.Invoke(_currentState);
		}

		private void AttackClosestEnemy()
		{
			if (_weaponsService.CurrentWeapon == null) return;
			if (Time.time - _currentState.LastAttackTime < _weaponsService.CurrentWeapon.Cooldown) return;
			if (!TryFindClosestEnemy(out EnemyState closestEnemy)) return;

			Vector3 facingDirection = (closestEnemy.Position - _currentState.Position).normalized;
			_currentState = new HeroState(_currentState.Position, _currentState.Health, Time.time, facingDirection);
			OnStateChanged?.Invoke(_currentState);

			_pendingAttackTargets = GetEnemiesInArc(facingDirection);
			OnAttacked?.Invoke();
		}

		public void ExecuteAttackDamage()
		{
			if (_pendingAttackTargets == null) return;

			foreach (EnemyState enemy in _pendingAttackTargets)
				_enemiesController.AttackEnemy(enemy, _weaponsService.CurrentWeapon.Damage);

			_pendingAttackTargets = null;
		}

		private bool TryFindClosestEnemy(out EnemyState closestEnemy)
		{
			closestEnemy = default;
			if (_weaponsService.CurrentWeapon == null) return false;
			float closestDistance = _weaponsService.CurrentWeapon.Range;
			bool found = false;

			foreach (EnemyState enemy in _enemiesController.Enemies.Values)
			{
				float distance = Vector3.Distance(_currentState.Position, enemy.Position);
				if (distance < closestDistance)
				{
					closestDistance = distance;
					closestEnemy = enemy;
					found = true;
				}
			}

			return found;
		}

		private List<EnemyState> GetEnemiesInArc(Vector3 facingDirection)
		{
			List<EnemyState> result = new List<EnemyState>();
			float halfArc = HeroConfig.Instance.AttackArcAngle / 2f;

			foreach (EnemyState enemy in _enemiesController.Enemies.Values)
			{
				float distance = Vector3.Distance(_currentState.Position, enemy.Position);
				if (distance > _weaponsService.CurrentWeapon.Range) continue;

				Vector3 dirToEnemy = (enemy.Position - _currentState.Position).normalized;
				if (Vector3.Angle(facingDirection, dirToEnemy) <= halfArc)
					result.Add(enemy);
			}

			return result;
		}
	}
}