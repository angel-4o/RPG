using UnityEngine;

namespace Game.CollectableWeapon
{
	public class CollectableWeaponView : MonoBehaviour
	{
		private int _id;
		private CollectableWeaponController _controller;

		public int Id => _id;

		public void Initialize(int id, CollectableWeaponController controller)
		{
			_id = id;
			_controller = controller;
		}
	}
}
