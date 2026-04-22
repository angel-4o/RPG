using UnityEngine;

namespace Game.CollectableWeapon
{
	public struct CollectableWeaponState
	{
		public int Id { get; }
		public Vector3 Position { get; }

		public CollectableWeaponState(int id, Vector3 position)
		{
			Id = id;
			Position = position;
		}

		public CollectableWeaponState With(Vector3? position = null)
		{
			return new CollectableWeaponState(Id, position ?? Position);
		}
	}
}
