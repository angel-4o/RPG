using Core.ScriptableObjectSingleton;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
	[CreateAssetMenu(fileName = "HeroConfig", menuName = "Game/HeroConfig")]
	public class HeroConfig : ScriptableObjectSingleton<HeroConfig>
	{
		[SerializeField]
		[Tooltip("The hero prefab to instantiate")]
		private HeroView heroPrefab;

		[SerializeField]
		[Tooltip("Movement speed in units per second")]
		private float moveSpeed = 5f;

		[SerializeField]
		[Tooltip("Initial health of the hero")]
		private int initialHealth = 100;

		[SerializeField]
		[Tooltip("Attack arc angle in degrees (e.g. 120 = 60° on each side)")]
		private float attackArcAngle = 120f;

		[SerializeField]
		[Tooltip("Minimum charge duration in seconds to trigger an attack")]
		private float minChargeDuration = 0.5f;

		[SerializeField]
		[Tooltip("Charge duration in seconds at which damage is capped")]
		private float maxChargeDuration = 2f;

		[SerializeField]
		[Tooltip("Damage multiplier applied at maximum charge (e.g. 3 = 3x base damage)")]
		private float maxChargeDamageMultiplier = 3f;

		[SerializeField]
		[Tooltip("Weapon scale multiplier at maximum charge (e.g. 2 = 2x normal size)")]
		private float maxChargeWeaponScale = 2f;

		[SerializeField]
		[Tooltip("Range multiplier at maximum charge (e.g. 2 = 2x base weapon range)")]
		private float maxChargeRangeMultiplier = 2f;

		public HeroView HeroPrefab => heroPrefab;
		public float MoveSpeed => moveSpeed;
		public int InitialHealth => initialHealth;
		public float AttackArcAngle => attackArcAngle;
		public float MinChargeDuration => minChargeDuration;
		public float MaxChargeDuration => maxChargeDuration;
		public float MaxChargeDamageMultiplier => maxChargeDamageMultiplier;
		public float MaxChargeWeaponScale => maxChargeWeaponScale;
		public float MaxChargeRangeMultiplier => maxChargeRangeMultiplier;
	}
}
