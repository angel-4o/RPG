using System;
using System.Collections.Generic;
using System.Threading;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public class EnemiesController
	{
		private HeroController _heroController;

		// Events
		public event Action<EnemyState> OnEnemySpawned;
		public event Action<int> OnEnemyRemoved;
		public event Action<EnemyState> OnEnemyPositionChanged;
		public event Action<int, Vector3> OnEnemyHit;
		public event Action<int> OnEnemyAttacked;
		public event Action<EnemyState> OnEnemyHealthChanged;
		public event Action OnEnemyDied;
		public event Action<EnemyState> OnEnemyPhaseChanged;

		// State
		private Dictionary<int, EnemyState> _enemies;
		private CancellationTokenSource _cancellationTokenSource;
		private int _nextEnemyId;
		private readonly List<int> _enemyUpdateBuffer = new List<int>();

		public IReadOnlyDictionary<int, EnemyState> Enemies => _enemies;

		public UniTask<bool> Initialize(HeroController heroController)
		{
			_heroController = heroController;

			_enemies = new Dictionary<int, EnemyState>();
			_nextEnemyId = 0;
			_cancellationTokenSource = new CancellationTokenSource();

			SpawnLoop(_cancellationTokenSource.Token).Forget();
			UpdateLoop(_cancellationTokenSource.Token).Forget();

			return UniTask.FromResult(true);
		}

		public UniTask Reset()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();
			_enemies.Clear();

			return UniTask.CompletedTask;
		}

		public void ClearAllEnemies()
		{
			List<int> enemyIds = new List<int>(_enemies.Keys);
			foreach (int enemyId in enemyIds)
				RemoveEnemy(enemyId);
		}

		public void RemoveEnemy(int enemyId)
		{
			if (_enemies.Remove(enemyId))
				OnEnemyRemoved?.Invoke(enemyId);
		}

		public void AttackEnemy(EnemyState enemyState, int damage)
		{
			if (!_enemies.ContainsKey(enemyState.Id)) return;

			OnEnemyHit?.Invoke(enemyState.Id, enemyState.Position);
			int newHealth = enemyState.Health - damage;

			Debug.Log($"Attacked enemy id°{enemyState.Id}. Health : {enemyState.Health} -> {newHealth}");

			if (newHealth <= 0)
			{
				Debug.Log($"Enemy id°{enemyState.Id} is dead. Removing it.");
				OnEnemyDied?.Invoke();
				RemoveEnemy(enemyState.Id);
			}
			else
			{
				EnemyState updated = enemyState.With(health: newHealth);
				_enemies[enemyState.Id] = updated;
				OnEnemyHealthChanged?.Invoke(updated);
			}
		}

		private async UniTaskVoid SpawnLoop(CancellationToken cancellationToken)
		{
			await UniTask.Delay(TimeSpan.FromSeconds(3), cancellationToken: cancellationToken);

			while (!cancellationToken.IsCancellationRequested)
			{
				if (!_heroController.CurrentState.IsDead && _enemies.Count < EnemiesConfig.Instance.MaxEnemies)
					SpawnEnemy();

				await UniTask.Delay(TimeSpan.FromSeconds(EnemiesConfig.Instance.SpawnInterval), cancellationToken: cancellationToken);
			}
		}

		private void SpawnEnemy()
		{
			if (EnemiesConfig.Instance.Enemies.Count == 0) return;

			Vector3 playerPosition = _heroController.CurrentState.Position;
			Vector3 spawnPosition = GetRandomPositionAroundPlayer(playerPosition);

			int enemyId = _nextEnemyId++;
			EnemyConfig enemyConfig = EnemiesConfig.Instance.Enemies[0];
			EnemyState newEnemy = new EnemyState(enemyId, spawnPosition, enemyConfig.InitialHealth, enemyConfig, lastAttackTime: Time.time);

			_enemies[enemyId] = newEnemy;
			OnEnemySpawned?.Invoke(newEnemy);
		}

		private Vector3 GetRandomPositionAroundPlayer(Vector3 playerPosition)
		{
			float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
			float x = playerPosition.x + EnemiesConfig.Instance.SpawnRadius * Mathf.Cos(angle);
			float z = playerPosition.z + EnemiesConfig.Instance.SpawnRadius * Mathf.Sin(angle);

			return new Vector3(x, playerPosition.y, z);
		}

		private async UniTaskVoid UpdateLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				if (_heroController.CurrentState.IsDead)
				{
					await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
					continue;
				}

				_enemyUpdateBuffer.Clear();
				_enemyUpdateBuffer.AddRange(_enemies.Keys);

				for (int i = 0; i < _enemyUpdateBuffer.Count; i++)
				{
					if (!_enemies.TryGetValue(_enemyUpdateBuffer[i], out EnemyState enemy)) continue;
					UpdateEnemy(enemy);
				}

				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}
		}

		private void UpdateEnemy(EnemyState enemy)
		{
			Vector3 heroPosition = _heroController.CurrentState.Position;

			switch (enemy.Phase)
			{
				case LungePhase.Chasing:
					UpdateChasing(enemy, heroPosition);
					break;
				case LungePhase.Lunging:
					UpdateLunging(enemy, heroPosition);
					break;
				case LungePhase.Recovering:
					UpdateRecovering(enemy);
					break;
			}
		}

		private void UpdateChasing(EnemyState enemy, Vector3 heroPosition)
		{
			float distanceToHero = Vector3.Distance(enemy.Position, heroPosition);

			bool lungeCooldownElapsed = Time.time - enemy.LastAttackTime >= enemy.Config.LungeCooldown;
			if (distanceToHero <= enemy.Config.LungeRange && lungeCooldownElapsed)
			{
				EnemyState updated = enemy.With(phase: LungePhase.Lunging, lungeActionStartTime: Time.time, lungeTarget: heroPosition);
				_enemies[enemy.Id] = updated;
				OnEnemyPhaseChanged?.Invoke(updated);
				return;
			}

			if (distanceToHero <= enemy.Config.AttackRange)
			{
				if (Time.time - enemy.LastAttackTime >= enemy.Config.AttackCooldown)
				{
					_heroController.TakeHit(enemy.Config.AttackDamage);
					OnEnemyAttacked?.Invoke(enemy.Id);
					_enemies[enemy.Id] = enemy.With(lastAttackTime: Time.time);
				}
				return;
			}

			Vector3 desiredDirection = (heroPosition - enemy.Position).normalized;
			Vector3 smoothedDirection = Vector3.Slerp(enemy.MoveDirection, desiredDirection, enemy.Config.DirectionSmoothFactor * Time.deltaTime).normalized;
			Vector3 newPosition = enemy.Position + smoothedDirection * (enemy.Config.Speed * Time.deltaTime);

			EnemyState moved = enemy.With(position: newPosition, moveDirection: smoothedDirection);
			_enemies[enemy.Id] = moved;
			OnEnemyPositionChanged?.Invoke(moved);
		}

		private void UpdateLunging(EnemyState enemy, Vector3 heroPosition)
		{
			if (Vector3.Distance(enemy.Position, heroPosition) <= enemy.Config.AttackRange)
			{
				_heroController.TakeHit(enemy.Config.AttackDamage);
				OnEnemyAttacked?.Invoke(enemy.Id);

				EnemyState recovered = enemy.With(lastAttackTime: Time.time, phase: LungePhase.Recovering, lungeActionStartTime: Time.time);
				_enemies[enemy.Id] = recovered;
				OnEnemyPositionChanged?.Invoke(recovered);
				OnEnemyPhaseChanged?.Invoke(recovered);
				return;
			}

			Vector3 toTarget = enemy.LungeTarget - enemy.Position;
			float distToTarget = toTarget.magnitude;
			float step = enemy.Config.LungeSpeed * Time.deltaTime;

			if (distToTarget <= step)
			{
				EnemyState recovered = enemy.With(position: enemy.LungeTarget, lastAttackTime: Time.time, phase: LungePhase.Recovering, lungeActionStartTime: Time.time);
				_enemies[enemy.Id] = recovered;
				OnEnemyPositionChanged?.Invoke(recovered);
				OnEnemyPhaseChanged?.Invoke(recovered);
				return;
			}

			Vector3 direction = toTarget / distToTarget;
			EnemyState moved = enemy.With(position: enemy.Position + direction * step, moveDirection: direction);
			_enemies[enemy.Id] = moved;
			OnEnemyPositionChanged?.Invoke(moved);
		}

		private void UpdateRecovering(EnemyState enemy)
		{
			if (Time.time - enemy.LungeActionStartTime >= enemy.Config.RecoveryDuration)
			{
				EnemyState updated = enemy.With(phase: LungePhase.Chasing, lungeActionStartTime: 0f, lungeTarget: default);
				_enemies[enemy.Id] = updated;
				OnEnemyPhaseChanged?.Invoke(updated);
			}
		}
	}
}