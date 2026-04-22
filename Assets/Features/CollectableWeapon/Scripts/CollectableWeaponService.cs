using System;
using Core.ServicesManager;
using Cysharp.Threading.Tasks;
using Game.GamePlay.Entities;

namespace Game.CollectableWeapon
{
	public class CollectableWeaponService : IService
	{
		public Type[] GetDependencies() => new[] { typeof(EntitiesService) };

		public CollectableWeaponController CollectableWeaponController { get; private set; }

		public async UniTask<bool> Initialize()
		{
			EntitiesService entitiesService = ServicesLocator.Instance.GetService<EntitiesService>();

			CollectableWeaponController = new CollectableWeaponController();
			return await CollectableWeaponController.Initialize(entitiesService.HeroController);
		}

		public UniTask Reset() => CollectableWeaponController?.Reset() ?? UniTask.CompletedTask;
	}
}
