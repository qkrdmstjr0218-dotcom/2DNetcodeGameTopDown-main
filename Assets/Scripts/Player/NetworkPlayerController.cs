using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Cinemachine;
using TopDownShooter.Core;
using TopDownShooter.Pooling;
using TopDownShooter.Managers;

namespace TopDownShooter.Networking
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class NetworkPlayerController : NetworkBehaviour
    {
        public static readonly List<NetworkPlayerController> ActivePlayers = new();
        [SerializeField] private PlayerInputHandler inputHandler;
        [SerializeField] private float moveSpeed = 5;
        [SerializeField] private float sprintMultiplier = 1.6f;
        [SerializeField] private bool allowSprintFire = true;
        [SerializeField] private Transform firePoint;
        [SerializeField] private ProjectileConfigSO projectileConfig;
        [SerializeField] private WeaponDefinitionSO[] weapons;
        [SerializeField] private CinemachineCamera playerCamera;
        [SerializeField] private GameEventChannelSO playerDownedEvent;
        private readonly NetworkVariable<int> score = new(0);
        private readonly NetworkVariable<int> weaponIndex = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<Vector2> aim = new(Vector2.right, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> sprint = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private Rigidbody2D body;
        private NetworkHealth health;
        private OperatorAimVisual visual;
        private ProjectilePredictionPool predictedShots;
        private uint shotSequence, lastServerSequence;
        private double nextServerFireTime;
        private float nextLocalFire, nextAimSend;
        private Vector2 localAim = Vector2.right;
        private WeaponDefinitionSO fallback;
        private NetworkList<WeaponUpgradeState> upgrades;
        private void Awake() { upgrades = new NetworkList<WeaponUpgradeState>(); }
        public NetworkVariable<int> Score => score;
        public int SelectedWeapon => weaponIndex.Value;
        public bool IsSprinting => sprint.Value;
        public Vector2 AimDirection => IsOwner ? localAim : aim.Value;
        public WeaponDefinitionSO CurrentWeapon => GetWeapon(SelectedWeapon);
        public WeaponRuntimeStats CurrentStats => GetWeaponStats(SelectedWeapon);
        public WeaponRuntimeStats GetWeaponStats(int index)
            => new(GetWeapon(index), upgrades != null && index >= 0 && index < upgrades.Count ? upgrades[index] : default);
        // Future upgrade selection/shop systems call this on the server after validating a reward.
        public bool ApplyWeaponUpgrade(int index, int damageBonus = 0, float cooldownMultiplier = 1,
            float rangeBonus = 0, int targetBonus = 0, bool unlimitedPiercing = false)
        {
            if (!IsServer || index < 0 || index >= upgrades.Count || damageBonus < 0 || targetBonus < 0
                || !float.IsFinite(cooldownMultiplier) || cooldownMultiplier <= 0 || !float.IsFinite(rangeBonus) || rangeBonus < 0) return false;
            var state = upgrades[index];
            state.DamageBonus = (int)System.Math.Min(99999L,(long)state.DamageBonus+damageBonus);
            state.CooldownScale = Mathf.Clamp((state.CooldownScale > 0 ? state.CooldownScale : 1) * cooldownMultiplier,0.05f,10);
            state.RangeBonus = Mathf.Min(1000,state.RangeBonus+rangeBonus);
            state.TargetBonus = (int)System.Math.Min(99999L,(long)state.TargetBonus+targetBonus);
            state.UnlimitedPiercing |= unlimitedPiercing;
            upgrades[index] = state;
            return true;
        }
        public int WeaponCount => weapons != null && weapons.Length > 0 ? weapons.Length : 1;
        public bool CanAct => health != null && !health.IsDowned.Value;
        public uint ShotCount => shotSequence;
        public Vector2 MoveInput { get; private set; }
        private WeaponDefinitionSO GetWeapon(int index)
        {
            if (weapons != null && index >= 0 && index < weapons.Length && weapons[index] != null) return weapons[index];
            if (fallback == null) fallback = ScriptableObject.CreateInstance<WeaponDefinitionSO>();
            return fallback;
        }
        public override void OnNetworkSpawn()
        {
            body = GetComponent<Rigidbody2D>(); health = GetComponent<NetworkHealth>();
            visual = GetComponent<OperatorAimVisual>();
            ActivePlayers.Add(this);
            if (IsServer)
            {
                upgrades.Clear();
                for (int i=0;i<WeaponCount;i++) upgrades.Add(new WeaponUpgradeState { CooldownScale=1 });
            }
            body.interpolation = IsOwner ? RigidbodyInterpolation2D.Interpolate : RigidbodyInterpolation2D.None;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            if (inputHandler == null) inputHandler = GetComponent<PlayerInputHandler>();
            inputHandler?.EnableInput(IsOwner);
            if (playerCamera == null) playerCamera = GetComponentInChildren<CinemachineCamera>(true);
            if (playerCamera != null) { playerCamera.Follow = transform; playerCamera.gameObject.SetActive(IsOwner); }
            if (projectileConfig != null && projectileConfig.ProjectilePrefab != null)
            {
                NetworkObjectPool.Instance.RegisterPrefab(projectileConfig.ProjectilePrefab.GetComponent<NetworkObject>(), 64);
                if (IsOwner && !IsServer) predictedShots = new ProjectilePredictionPool(projectileConfig.ProjectilePrefab, 64);
            }
            nextLocalFire = 0; shotSequence = lastServerSequence = 0; nextServerFireTime = 0;
        }
        private void Update()
        {
            predictedShots?.Tick();
            if (!IsSpawned) return;
            bool posed = false;
            if (IsOwner)
            {
                bool active = CanAct && (Application.isFocused || Application.isBatchMode) && !PlaygroundSessionUI.PointerOverPanel;
                if (active)
                {
                    localAim = CalculateAimDirection();
                    var keyboard = Keyboard.current;
                    if (keyboard != null)
                    {
                        if (keyboard.digit1Key.wasPressedThisFrame) SelectWeapon(0);
                        if (keyboard.digit2Key.wasPressedThisFrame) SelectWeapon(1);
                        if (keyboard.digit3Key.wasPressedThisFrame) SelectWeapon(2);
                    }
                    float wheel = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0;
                    if (wheel != 0) SelectWeapon((SelectedWeapon + (wheel > 0 ? -1 : 1) + WeaponCount) % WeaponCount);
                    visual?.Pose(localAim, CurrentWeapon, body.linearVelocity.magnitude, OwnerClientId);
                    posed = true;
                    if (Mouse.current != null && Mouse.current.leftButton.isPressed && (allowSprintFire || !IsSprinting)) HandleAttack();
                }
                if (Time.unscaledTime >= nextAimSend && (aim.Value - localAim).sqrMagnitude > 0.0001f)
                { aim.Value = localAim; nextAimSend = Time.unscaledTime + 0.05f; }
            }
            if (!posed) visual?.Pose(IsOwner ? localAim : AimDirection, CurrentWeapon, body.linearVelocity.magnitude, OwnerClientId);
        }
        public void SelectWeapon(int index)
        { if (IsOwner && CanAct && index >= 0 && index < WeaponCount) weaponIndex.Value = index; }
        private Vector2 CalculateAimDirection()
        {
            if (Camera.main == null || Mouse.current == null) return localAim;
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            var plane = new Plane(Vector3.forward, transform.position);
            if (!plane.Raycast(ray, out float distance)) return localAim;
            Vector2 direction = ray.GetPoint(distance) - transform.position;
            return direction.sqrMagnitude < 0.001f ? localAim : direction.normalized;
        }
        private void FixedUpdate()
        {
            if (!IsSpawned || !IsOwner || body == null) return;
            MoveInput = CanAct && (Application.isFocused || Application.isBatchMode) && inputHandler != null ? inputHandler.MoveInput : Vector2.zero;
            bool running = CanAct && MoveInput.sqrMagnitude > 0.01f && (Application.isFocused || Application.isBatchMode) && Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
            if (sprint.Value != running) sprint.Value = running;
            body.linearVelocity = Vector2.ClampMagnitude(MoveInput, 1) * moveSpeed * (running ? sprintMultiplier : 1);
        }
        public static Vector2 PelletDirection(Vector2 direction, uint sequence, int pellet, int count, float spread, ulong owner)
        {
            uint hash = unchecked(sequence * 747796405u + (uint)pellet * 2891336453u + (uint)owner * 277803737u);
            hash = unchecked((hash ^ (hash >> 16)) * 2246822519u);
            float unit = (hash & 65535) / 65535f;
            float angle = count == 1 ? Mathf.Lerp(-spread, spread, unit) : Mathf.Lerp(-spread, spread, (pellet + unit) / count);
            return Quaternion.Euler(0, 0, angle) * direction.normalized;
        }
        private void HandleAttack()
        {
            if (!IsOwner || !CanAct || projectileConfig == null || Time.time < nextLocalFire) return;
            var weapon = CurrentWeapon;
            var stats = CurrentStats;
            nextLocalFire = Time.time + stats.Interval;
            uint sequence = ++shotSequence;
            Vector3 origin = visual != null ? visual.Muzzle : transform.position + (Vector3)localAim * 0.6f;
            int count = Mathf.Clamp(weapon.pellets, 1, 8);
            float spread = weapon.spread + (IsSprinting ? weapon.sprintSpread : 0);
            for (int i = 0; i < count; i++) predictedShots?.Predict(sequence * 8 + (uint)i, origin, PelletDirection(localAim, sequence, i, count, spread, OwnerClientId), weapon.speed, stats.Range / weapon.speed);
            SoundManager.Instance?.PlaySfx("Shoot"); visual?.Kick();
            FireServerRpc(localAim, origin, sequence, SelectedWeapon, IsSprinting);
        }
        [ServerRpc]
        private void FireServerRpc(Vector2 direction, Vector3 requestedOrigin, uint sequence, int index, bool running)
        {
            double now = NetworkManager.ServerTime.Time;
            bool valid = float.IsFinite(direction.x) && float.IsFinite(direction.y) && float.IsFinite(requestedOrigin.x) && float.IsFinite(requestedOrigin.y) && float.IsFinite(requestedOrigin.z)
                && float.IsFinite(direction.sqrMagnitude) && direction.sqrMagnitude > 0.001f && sequence > lastServerSequence
                && index >= 0 && index < WeaponCount && CanAct && now + 0.02 >= nextServerFireTime && (allowSprintFire || !running);
            if (!valid) { RejectShotClientRpc(sequence); return; }
            lastServerSequence = sequence;
            var weapon = GetWeapon(index);
            var stats = GetWeaponStats(index);
            nextServerFireTime = System.Math.Max(now, nextServerFireTime) + stats.Interval;
            if (projectileConfig == null || projectileConfig.ProjectilePrefab == null) return;
            direction.Normalize();
            float allowance = moveSpeed * sprintMultiplier * 0.25f + 1.5f;
            Vector3 origin = (requestedOrigin - transform.position).sqrMagnitude < allowance * allowance
                ? new Vector3(requestedOrigin.x, requestedOrigin.y, transform.position.z) : transform.position;
            var obstruction = Physics2D.Linecast(transform.position, origin, 1 << 8);
            if (obstruction.collider != null) origin = obstruction.point - direction * 0.03f;
            int count = Mathf.Clamp(weapon.pellets, 1, 8);
            float spread = weapon.spread + (running ? weapon.sprintSpread : 0);
            for (int i = 0; i < count; i++)
            {
                Vector2 shotDirection = PelletDirection(direction, sequence, i, count, spread, OwnerClientId);
                uint pelletSequence = sequence * 8 + (uint)i;
                NetworkObjectPool.Instance.Spawn(projectileConfig.ProjectilePrefab.GetComponent<NetworkObject>(), origin, Quaternion.identity,
                    instance => instance.GetComponent<NetworkProjectile>().Initialize(shotDirection, weapon.speed, stats.Damage, stats.Range / weapon.speed, OwnerClientId, pelletSequence, now, stats.Targets));
            }
        }
        [ClientRpc] private void RejectShotClientRpc(uint sequence)
        { if (IsOwner) for (uint i = 0; i < 8; i++) predictedShots?.Release(sequence * 8 + i); }
        public bool ConsumePredictedShot(uint sequence, out Vector3 position, out float age)
        { position = default; age = 0; return predictedShots != null && predictedShots.Consume(sequence, out position, out age); }
        public void AddScore(int amount) { if (IsServer) score.Value += amount; }
        public void NotifyDowned() { playerDownedEvent?.Raise(); }
        public void ResetPosition() { if (IsServer) ResetPositionClientRpc(); }
        [ClientRpc] private void ResetPositionClientRpc()
        {
            if (!IsOwner) return;
            body.linearVelocity = Vector2.zero; body.position = Vector2.zero;
            if (TryGetComponent<NetworkTransform>(out var sync) && sync.CanCommitToTransform) sync.Teleport(Vector3.zero, transform.rotation, transform.localScale);
        }
        public override void OnNetworkDespawn()
        { ActivePlayers.Remove(this); inputHandler?.EnableInput(false); predictedShots?.Dispose(); predictedShots = null; }
        public override void OnDestroy() { upgrades?.Dispose(); if (fallback != null) Destroy(fallback); base.OnDestroy(); }
    }
}

