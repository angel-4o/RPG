using UnityEngine;

namespace Game.GamePlay.Enemies
{
	public struct EnemyState
	{
		public int Id { get; }
		public Vector3 Position { get; }
		public int Health { get; }
		public EnemyConfig Config { get; }
		public float LastAttackTime { get; }
		public float AttackWindupStartTime { get; }
		public Vector3 MoveDirection { get; }

		public EnemyState(int id, Vector3 position, int health, EnemyConfig config, float lastAttackTime = 0f, float attackWindupStartTime = -1f, Vector3 moveDirection = default)
		{
			Id = id;
			Position = position;
			Health = health;
			Config = config;
			LastAttackTime = lastAttackTime;
			AttackWindupStartTime = attackWindupStartTime;
			MoveDirection = moveDirection == Vector3.zero ? Vector3.forward : moveDirection;
		}
	}
}