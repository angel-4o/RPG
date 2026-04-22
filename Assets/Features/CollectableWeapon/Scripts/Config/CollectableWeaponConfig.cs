using Core.ScriptableObjectSingleton;
using UnityEngine;

namespace Game.CollectableWeapon
{
	[CreateAssetMenu(fileName = "CollectableWeaponConfig", menuName = "Game/CollectableWeaponConfig")]
	public class CollectableWeaponConfig : ScriptableObjectSingleton<CollectableWeaponConfig>
	{
		[SerializeField]
		[Tooltip("The collectable weapon prefab to instantiate")]
		private CollectableWeaponView collectableWeaponPrefab;

		[SerializeField]
		[Tooltip("Seconds to wait before the first collectable spawns")]
		private float initialSpawnDelay = 5f;

		[SerializeField]
		[Tooltip("Seconds between collectable spawns")]
		private float spawnInterval = 8f;

		[SerializeField]
		[Tooltip("Radius in units around the hero where collectables spawn")]
		private float spawnRadius = 6f;

		[SerializeField]
		[Tooltip("Maximum number of collectables alive at once")]
		private int maxCollectables = 5;

		[SerializeField]
		[Tooltip("Radius in units the hero must be within to collect the pickup")]
		private float pickupRadius = 1f;

		public CollectableWeaponView CollectableWeaponPrefab => collectableWeaponPrefab;
		public float InitialSpawnDelay => initialSpawnDelay;
		public float SpawnInterval => spawnInterval;
		public float SpawnRadius => spawnRadius;
		public int MaxCollectables => maxCollectables;
		public float PickupRadius => pickupRadius;
	}
}
