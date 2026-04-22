using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Heroes;
using UnityEngine;

namespace Game.CollectableWeapon
{
	public class CollectableWeaponController
	{
		public event Action<CollectableWeaponState> OnSpawned;
		public event Action<int> OnRemoved;
		public event Action<CollectableWeaponState> OnCollected;

		private HeroController _heroController;
		private Dictionary<int, CollectableWeaponState> _collectables;
		private CancellationTokenSource _cancellationTokenSource;
		private int _nextId;
		private readonly List<int> _updateBuffer = new List<int>();

		public IReadOnlyDictionary<int, CollectableWeaponState> Collectables => _collectables;

		public UniTask<bool> Initialize(HeroController heroController)
		{
			_heroController = heroController;

			_collectables = new Dictionary<int, CollectableWeaponState>();
			_nextId = 0;
			_cancellationTokenSource = new CancellationTokenSource();

			SpawnLoop(_cancellationTokenSource.Token).Forget();
			PickupLoop(_cancellationTokenSource.Token).Forget();

			return UniTask.FromResult(true);
		}

		public UniTask Reset()
		{
			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();
			_collectables?.Clear();
			return UniTask.CompletedTask;
		}

		public void Remove(int id)
		{
			if (_collectables.Remove(id))
				OnRemoved?.Invoke(id);
		}

		private void Collect(CollectableWeaponState state)
		{
			OnCollected?.Invoke(state);
			Remove(state.Id);
		}

		private async UniTaskVoid SpawnLoop(CancellationToken cancellationToken)
		{
			await UniTask.Delay(TimeSpan.FromSeconds(CollectableWeaponConfig.Instance.InitialSpawnDelay), cancellationToken: cancellationToken);

			while (!cancellationToken.IsCancellationRequested)
			{
				if (!_heroController.CurrentState.IsDead && _collectables.Count < CollectableWeaponConfig.Instance.MaxCollectables)
					Spawn();

				await UniTask.Delay(TimeSpan.FromSeconds(CollectableWeaponConfig.Instance.SpawnInterval), cancellationToken: cancellationToken);
			}
		}

		private void Spawn()
		{
			Vector3 heroPosition = _heroController.CurrentState.Position;
			Vector3 spawnPosition = GetRandomPositionAroundHero(heroPosition);

			int id = _nextId++;
			CollectableWeaponState newState = new CollectableWeaponState(id, spawnPosition);

			_collectables[id] = newState;
			OnSpawned?.Invoke(newState);
		}

		private Vector3 GetRandomPositionAroundHero(Vector3 heroPosition)
		{
			float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
			float radius = CollectableWeaponConfig.Instance.SpawnRadius;
			float x = heroPosition.x + radius * Mathf.Cos(angle);
			float z = heroPosition.z + radius * Mathf.Sin(angle);
			return new Vector3(x, heroPosition.y, z);
		}

		private async UniTaskVoid PickupLoop(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				if (_heroController.CurrentState.IsDead)
				{
					await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
					continue;
				}

				float pickupRadiusSqr = CollectableWeaponConfig.Instance.PickupRadius * CollectableWeaponConfig.Instance.PickupRadius;
				Vector3 heroPosition = _heroController.CurrentState.Position;

				_updateBuffer.Clear();
				_updateBuffer.AddRange(_collectables.Keys);

				for (int i = 0; i < _updateBuffer.Count; i++)
				{
					if (!_collectables.TryGetValue(_updateBuffer[i], out CollectableWeaponState state)) continue;
					if ((state.Position - heroPosition).sqrMagnitude <= pickupRadiusSqr)
						Collect(state);
				}

				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}
		}
	}
}
