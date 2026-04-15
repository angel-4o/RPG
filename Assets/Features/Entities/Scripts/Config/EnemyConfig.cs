using UnityEngine;

namespace Game.GamePlay.Enemies
{
	[CreateAssetMenu(fileName = "EnemyX", menuName = "Content/Enemy")]
	public class EnemyConfig : ScriptableObject
	{
		[SerializeField] private string id;
		[SerializeField] private int initialHealth;
		[SerializeField] private float speed;
		[SerializeField] private float directionSmoothFactor = 15f;
		[SerializeField] private float attackWindupDuration = 1f;
		[SerializeField] private float attackCooldown;
		[SerializeField] private int attackDamage;
		[SerializeField] private float attackRange;
		[SerializeField] private EnemyView prefab;

		[Header("Lunge")]
		[SerializeField] private float lungeRange = 5f;
		[SerializeField] private float lungeWindupDuration = 0.5f;
		[SerializeField] private float lungeSpeed = 8f;
		[SerializeField] private float recoveryDuration = 2f;
		[SerializeField] private float lungeCooldown = 5f;

		public string Id => id;
		public int InitialHealth => initialHealth;
		public float Speed => speed;
		public float DirectionSmoothFactor => directionSmoothFactor;
		public float AttackWindupDuration => attackWindupDuration;
		public float AttackCooldown => attackCooldown;
		public int AttackDamage => attackDamage;
		public float AttackRange => attackRange;
		public EnemyView Prefab => prefab;
		public float LungeRange => lungeRange;
		public float LungeWindupDuration => lungeWindupDuration;
		public float LungeSpeed => lungeSpeed;
		public float RecoveryDuration => recoveryDuration;
		public float LungeCooldown => lungeCooldown;
	}
}
