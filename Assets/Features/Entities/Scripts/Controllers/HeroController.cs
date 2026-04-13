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
		private int _pendingAttackDamage;
		private bool _wasJoystickActive;
		private float _chargeStartTime;
		private float _attackChargeRatio;

		// Public State
		public HeroState CurrentState => _currentState;
		public float CurrentChargeRatio
		{
			get
			{
				if (_pendingAttackTargets != null)
					return _attackChargeRatio;
				return _wasJoystickActive
					? Mathf.Clamp01(Mathf.InverseLerp(
						HeroConfig.Instance.MinChargeDuration,
						HeroConfig.Instance.MaxChargeDuration,
						Time.time - _chargeStartTime))
					: 0f;
			}
		}

		// Events
		public event Action<HeroState> OnStateChanged;
		public event Action OnAttacked;
		public event Action OnHit;
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
			OnHit?.Invoke();

			if (_currentState.IsDead)
			{
				Debug.Log("Hero is dead!");
				OnDied?.Invoke();
			}
		}

		public void Restart()
		{
			_wasJoystickActive = false;
			_chargeStartTime = 0f;
			_attackChargeRatio = 0f;
			_pendingAttackTargets = null;
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
					bool isJoystickActive = _joystickInputService.CurrentState.IsActive;

					if (isJoystickActive)
					{
						if (!_wasJoystickActive)
							_chargeStartTime = Time.time;

						UpdatePosition();
					}
					else if (_wasJoystickActive)
					{
						TryChargedAttack();
					}

					_wasJoystickActive = isJoystickActive;
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

		private void TryChargedAttack()
		{
			if (_weaponsService.CurrentWeapon == null) return;

			float chargeDuration = Time.time - _chargeStartTime;
			if (chargeDuration < HeroConfig.Instance.MinChargeDuration) return;

			float chargeRatio = Mathf.Clamp01(Mathf.InverseLerp(
				HeroConfig.Instance.MinChargeDuration,
				HeroConfig.Instance.MaxChargeDuration,
				chargeDuration
			));
			float scaledRange = _weaponsService.CurrentWeapon.Range *
				Mathf.Lerp(1f, HeroConfig.Instance.MaxChargeRangeMultiplier, chargeRatio);

			if (!TryFindClosestEnemy(out EnemyState closestEnemy, scaledRange)) return;

			_pendingAttackDamage = Mathf.RoundToInt(
				_weaponsService.CurrentWeapon.Damage *
				Mathf.Lerp(1f, HeroConfig.Instance.MaxChargeDamageMultiplier, chargeRatio)
			);

			Vector3 facingDirection = (closestEnemy.Position - _currentState.Position).normalized;
			_currentState = new HeroState(_currentState.Position, _currentState.Health, Time.time, facingDirection);
			OnStateChanged?.Invoke(_currentState);

			_attackChargeRatio = chargeRatio;
			_pendingAttackTargets = GetEnemiesInArc(facingDirection, scaledRange);
			OnAttacked?.Invoke();
		}

		public void ExecuteAttackDamage()
		{
			if (_pendingAttackTargets == null) return;

			foreach (EnemyState enemy in _pendingAttackTargets)
				_enemiesController.AttackEnemy(enemy, _pendingAttackDamage);

			_pendingAttackTargets = null;
		}

		private bool TryFindClosestEnemy(out EnemyState closestEnemy, float range)
		{
			closestEnemy = default;
			float closestDistance = range;
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

		private List<EnemyState> GetEnemiesInArc(Vector3 facingDirection, float range)
		{
			List<EnemyState> result = new List<EnemyState>();
			float halfArc = HeroConfig.Instance.AttackArcAngle / 2f;

			foreach (EnemyState enemy in _enemiesController.Enemies.Values)
			{
				float distance = Vector3.Distance(_currentState.Position, enemy.Position);
				if (distance > range) continue;

				Vector3 dirToEnemy = (enemy.Position - _currentState.Position).normalized;
				if (Vector3.Angle(facingDirection, dirToEnemy) <= halfArc)
					result.Add(enemy);
			}

			return result;
		}
	}
}