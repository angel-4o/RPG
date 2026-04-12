using UnityEngine;

namespace Game.GamePlay.Heroes
{
	public struct HeroState
	{
		public Vector3 Position { get; }
		public int Health { get; }
		public float LastAttackTime { get; }
		public Vector3 FacingDirection { get; }

		public bool IsDead => Health <= 0;

		public HeroState(Vector3 position, int health, float lastAttackTime, Vector3 facingDirection = default)
		{
			Position = position;
			Health = health;
			LastAttackTime = lastAttackTime;
			FacingDirection = facingDirection == Vector3.zero ? Vector3.forward : facingDirection;
		}
	}
}