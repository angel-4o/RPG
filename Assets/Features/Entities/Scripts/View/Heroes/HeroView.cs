using Game.GamePlay.Entities;
using Game.GamePlay.Heroes;
using Game.JoystickInput;
using Game.Weapons;
using Core.ServicesManager;
using UnityEngine;

namespace Game.GamePlay.Heroes
{
	public class HeroView : MonoBehaviour
	{
		private static readonly int SpeedHash = Animator.StringToHash("Speed");
		private static readonly int AttackHash = Animator.StringToHash("Attack");
		private static readonly int DieHash = Animator.StringToHash("Die");

		[SerializeField] private Animator animator;
		[SerializeField] private float rotationSpeed = 10f;
		[SerializeField] private Transform weaponSlot;
		[SerializeField] private ParticleSystem chargeParticle;
		[SerializeField] private float maxChargeParticleScale = 2f;

		private JoystickInputService _joystickInputService;
		private HeroController _heroController;
		private WeaponsService _weaponsService;
		private Vector2 _currentMovementInput;
		private WeaponView _currentWeaponView;
		private AnimationEventComponent _animationEventComponent;

		private void Start()
		{
			ServicesLocator.Instance.OnAllServicesInitialized += OnServicesInitialized;

			_animationEventComponent = GetComponentInChildren<AnimationEventComponent>();
			if (_animationEventComponent != null)
				_animationEventComponent.OnAnimationEvent += OnAttackImpact;
		}

		private void OnServicesInitialized()
		{
			_joystickInputService = ServicesLocator.Instance.GetService<JoystickInputService>();
			_heroController = ServicesLocator.Instance.GetService<EntitiesService>().HeroController;
			_weaponsService = ServicesLocator.Instance.GetService<WeaponsService>();

			_joystickInputService.OnStateChanged += OnJoystickStateChanged;
			_heroController.OnStateChanged += OnHeroStateChanged;
			_heroController.OnAttacked += OnHeroAttacked;
			_heroController.OnDied += OnHeroDied;
			_weaponsService.OnWeaponChanged += OnWeaponChanged;

			OnJoystickStateChanged(_joystickInputService.CurrentState);
			OnHeroStateChanged(_heroController.CurrentState);
			SpawnCurrentWeapon();
		}

		private void OnDestroy()
		{
			ServicesLocator.Instance.OnAllServicesInitialized -= OnServicesInitialized;
			if (_joystickInputService != null)
			{
				_joystickInputService.OnStateChanged -= OnJoystickStateChanged;
			}
			if (_heroController != null)
			{
				_heroController.OnStateChanged -= OnHeroStateChanged;
				_heroController.OnAttacked -= OnHeroAttacked;
				_heroController.OnDied -= OnHeroDied;
			}
			if (_weaponsService != null)
			{
				_weaponsService.OnWeaponChanged -= OnWeaponChanged;
			}
			if (_animationEventComponent != null)
				_animationEventComponent.OnAnimationEvent -= OnAttackImpact;
			if (_currentWeaponView != null)
			{
				Destroy(_currentWeaponView.gameObject);
			}
		}

		private void OnJoystickStateChanged(JoystickState state)
		{
			_currentMovementInput = state.IsActive ? state.MovementVector : Vector2.zero;
			UpdateAnimator();
			UpdateChargeParticle(state.IsActive);
		}

		private void OnHeroStateChanged(HeroState heroState)
		{
			transform.position = heroState.Position;
		}

		private void OnHeroAttacked()
		{
			if (animator == null) return;
			animator.SetTrigger(AttackHash);
		}

		private void OnHeroDied()
		{
			if (animator == null) return;
			animator.SetTrigger(DieHash);
			DetachWeapon();
			UpdateChargeParticle(false);
		}

		private void UpdateChargeParticle(bool isCharging)
		{
			if (chargeParticle == null) return;

			if (isCharging)
			{
				// chargeParticle.Play();
			}
			else
			{
				// chargeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
				chargeParticle.transform.localScale = Vector3.zero;
			}
		}

		private void UpdateChargeParticleScale()
		{
			if (chargeParticle == null) return;

			float scale = Mathf.Lerp(0f, maxChargeParticleScale, _heroController.CurrentChargeRatio);
			chargeParticle.transform.localScale = Vector3.one * scale;
		}

		private void DetachWeapon()
		{
			if (_currentWeaponView == null) return;

			_currentWeaponView.transform.SetParent(null);
			Rigidbody rb = _currentWeaponView.GetComponent<Rigidbody>();
			if (rb != null)
				rb.isKinematic = false;
		}
		
		public void OnAttackImpact()
		{
			_heroController?.ExecuteAttackDamage();
		}

		private void Update()
		{
			if (_heroController == null || _heroController.CurrentState.IsDead) return;

			Vector3 facingDirection = _heroController.CurrentState.FacingDirection;
			if (facingDirection.sqrMagnitude <= 0.01f) return;

			Quaternion targetRotation = Quaternion.LookRotation(-facingDirection);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

			UpdateWeaponScale();
			UpdateChargeParticleScale();
		}

		private void UpdateWeaponScale()
		{
			if (_currentWeaponView == null) return;

			float chargeRatio = _heroController.CurrentChargeRatio;
			float scale = Mathf.Lerp(1f, HeroConfig.Instance.MaxChargeWeaponScale, chargeRatio);
			_currentWeaponView.transform.localScale = Vector3.one * scale;
		}

		private void UpdateAnimator()
		{
			if (animator == null) return;
			if (_heroController is { CurrentState: { IsDead: true } })
			{
				animator.SetFloat(SpeedHash, 0f);
				return;
			}

			float speed = _currentMovementInput.magnitude;
			animator.SetFloat(SpeedHash, speed);
		}

		private void OnWeaponChanged(WeaponConfig newWeapon)
		{
			if (_currentWeaponView != null)
			{
				Destroy(_currentWeaponView.gameObject);
				_currentWeaponView = null;
			}

			SpawnCurrentWeapon();
		}

		private void SpawnCurrentWeapon()
		{
			if (_weaponsService.CurrentWeapon == null) return;

			Transform parent = weaponSlot != null ? weaponSlot : transform;
			_currentWeaponView = Instantiate(_weaponsService.CurrentWeapon.Prefab, parent);
			_currentWeaponView.transform.localPosition = Vector3.zero;
			_currentWeaponView.transform.localRotation = Quaternion.identity;
		}
	}
}