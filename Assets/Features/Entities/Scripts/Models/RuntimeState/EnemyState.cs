using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public enum LungePhase { Chasing, Lunging, Recovering }

	public struct EnemyState
	{
		public int Id { get; }
		public Vector3 Position { get; }
		public int Health { get; }
		public EnemyConfig Config { get; }
		public float LastAttackTime { get; }
		public LungePhase Phase { get; }
		public float LungeActionStartTime { get; }
		public Vector3 LungeTarget { get; }
		public Vector3 MoveDirection { get; }

		public EnemyState(int id, Vector3 position, int health, EnemyConfig config,
			float lastAttackTime = 0f, LungePhase phase = LungePhase.Chasing,
			float lungeActionStartTime = 0f, Vector3 lungeTarget = default, Vector3 moveDirection = default)
		{
			Id = id;
			Position = position;
			Health = health;
			Config = config;
			LastAttackTime = lastAttackTime;
			Phase = phase;
			LungeActionStartTime = lungeActionStartTime;
			LungeTarget = lungeTarget;
			MoveDirection = moveDirection == Vector3.zero ? Vector3.forward : moveDirection;
		}

		public EnemyState With(
			Vector3? position = null,
			int? health = null,
			float? lastAttackTime = null,
			LungePhase? phase = null,
			float? lungeActionStartTime = null,
			Vector3? lungeTarget = null,
			Vector3? moveDirection = null)
		{
			return new EnemyState(
				Id,
				position ?? Position,
				health ?? Health,
				Config,
				lastAttackTime ?? LastAttackTime,
				phase ?? Phase,
				lungeActionStartTime ?? LungeActionStartTime,
				lungeTarget ?? LungeTarget,
				moveDirection ?? MoveDirection
			);
		}
	}
}