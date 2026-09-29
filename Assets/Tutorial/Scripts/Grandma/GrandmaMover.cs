using UnityEngine;
using UnityEngine.AI;

namespace GardenGrowers.NPC
{
    /// <summary>
    /// Moves Gogo around: walking to a point or following the player.
    /// Uses the NavMesh if the scene has one, otherwise walks in straight lines.
    /// </summary>
    [DisallowMultipleComponent]
    public class GrandmaMover : MonoBehaviour
    {
        public enum MoveMode { Idle, GoTo, Follow }

        [Header("Speed")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 1.9f;
        [SerializeField, Min(0.1f)] private float hurrySpeed = 4.4f;
        [SerializeField, Min(0.1f)] private float acceleration = 6f;
        [Tooltip("How quickly she turns (smaller = snappier).")]
        [SerializeField, Min(0.01f)] private float turnSmoothTime = 0.14f;

        [Header("Following the player")]
        [SerializeField, Min(0.5f)] private float followDistance = 3.6f;
        [Tooltip("If the player walks closer than this, she steps aside to give them room.")]
        [SerializeField, Min(0.5f)] private float personalSpace = 2.3f;
        [Tooltip("Further than this and she hurries.")]
        [SerializeField, Min(1f)] private float hurryDistance = 9f;
        [Tooltip("Further than this, and out of sight, she quietly catches up behind the player.")]
        [SerializeField, Min(5f)] private float catchUpDistance = 30f;
        [Tooltip("If a Go To takes longer than this and she's out of sight, she pops to the spot (stuck safety net).")]
        [SerializeField, Min(5f)] private float goToGiveUpSeconds = 25f;

        [Header("Turning in place")]
        [Tooltip("She only turns to face someone once they are this far off to the side (degrees).")]
        [SerializeField, Range(0f, 90f)] private float faceAngleThreshold = 40f;

        [Header("Body")]
        [Tooltip("A solid capsule so the player bumps into her instead of walking through her. It sits on the Ignore Raycast layer, so it never blocks interacting with things.")]
        [SerializeField] private bool solidBody = true;
        [SerializeField, Min(0.1f)] private float bodyRadius = 0.35f;

        [Header("NavMesh")]
        [Tooltip("If the scene has a baked NavMesh, add a NavMeshAgent so she walks around obstacles.")]
        [SerializeField] private bool useNavMeshIfAvailable = true;
        [SerializeField, Min(0.1f)] private float agentRadius = 0.45f;
        [SerializeField, Min(0.5f)] private float agentHeight = 2.5f;

        [Header("Ground (only used without a NavMesh)")]
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField, Min(0.5f)] private float groundProbeHeight = 3f;

        private NavMeshAgent agent;
        private bool useAgent;
        private MoveMode mode = MoveMode.Idle;
        private Vector3 destination;
        private float stopDistance = 0.35f;
        private Transform followTarget;
        private Transform faceTarget;
        private Vector3? faceDirection;
        private Vector3 velocity;
        private float yawVelocity;
        private float lastYaw;
        private float goToTimer;
        private bool turningInPlace;
        private bool makingRoom;
        private bool hasRoomSpot;
        private Vector3 roomSpot;
        private Vector3 roomSpotFor;
        private NavMeshPath path;
        private readonly RaycastHit[] groundHits = new RaycastHit[8];

        // Angles to try for a free spot, from the preferred direction outwards.
        private static readonly float[] SearchAngles = { 0f, 35f, -35f, 70f, -70f, 105f, -105f, 140f, -140f, 180f };

        public MoveMode Mode => mode;
        public Vector3 Velocity => velocity;
        public float Speed => new Vector3(velocity.x, 0f, velocity.z).magnitude;
        public float AngularVelocity { get; private set; }
        public bool IsTurningInPlace => turningInPlace;
        public bool HasArrived { get; private set; } = true;
        public bool UsesNavMesh => useAgent;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            lastYaw = transform.eulerAngles.y;
        }

        private void Start()
        {
            // Only add an agent if there's a NavMesh, so scenes without one get no warnings.
            if (agent == null && useNavMeshIfAvailable && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                agent = gameObject.AddComponent<NavMeshAgent>();
                agent.radius = agentRadius;
                agent.height = agentHeight;
                agent.baseOffset = 0f;
                agent.angularSpeed = 0f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            }

            if (agent != null)
            {
                agent.updateRotation = false;
                agent.autoBraking = true;
            }

            if (solidBody) AddBody();

            useAgent = agent != null && agent.enabled && agent.isOnNavMesh;
            if (agent != null && !useAgent) agent.enabled = false;
            if (!useAgent) Debug.Log("[Gogo] No NavMesh here, so she walks in straight lines. Bake the Tutorial NavMesh for proper paths.", this);
        }

        private void AddBody()
        {
            if (transform.Find("Body Collider") != null) return;

            var body = new GameObject("Body Collider") { layer = 2 }; // Ignore Raycast
            body.transform.SetParent(transform, false);
            var capsule = body.AddComponent<CapsuleCollider>();
            capsule.radius = bodyRadius;
            capsule.height = agentHeight;
            capsule.center = new Vector3(0f, agentHeight * 0.5f, 0f);

            // Kinematic, so physics never pushes her around.
            var rigid = GetComponent<Rigidbody>();
            if (rigid == null) rigid = gameObject.AddComponent<Rigidbody>();
            rigid.isKinematic = true;
            rigid.useGravity = false;
            rigid.interpolation = RigidbodyInterpolation.None;
        }

        //Commands

        public void GoTo(Vector3 point, float stopAt = 0.35f)
        {
            mode = MoveMode.GoTo;
            destination = point;
            stopDistance = Mathf.Max(0.1f, stopAt);
            HasArrived = false;
            goToTimer = 0f;
            if (useAgent) agent.SetDestination(point);
        }

        public void Follow(Transform target)
        {
            mode = MoveMode.Follow;
            followTarget = target;
            HasArrived = false;
        }

        public void Stop()
        {
            mode = MoveMode.Idle;
            HasArrived = true;
            if (useAgent && agent.isOnNavMesh) agent.ResetPath();
        }

        /// <summary>Who to face when standing still.</summary>
        public void FaceTowards(Transform target)
        {
            faceTarget = target;
            faceDirection = null;
        }

        public void FaceTowards(Vector3 worldPoint)
        {
            Vector3 direction = worldPoint - transform.position;
            direction.y = 0f;
            faceDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : (Vector3?)null;
            faceTarget = null;
        }

        private float FlatDistance(Vector3 point)
        {
            Vector3 d = point - transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        /// <summary>Degrees between where she faces and the given point (flat).</summary>
        public float AngleTo(Vector3 worldPoint)
        {
            Vector3 direction = worldPoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return 0f;
            return Vector3.Angle(transform.forward, direction);
        }

        public void Warp(Vector3 point)
        {
            if (useAgent) agent.Warp(point);
            else transform.position = SnapToGround(point);
            velocity = Vector3.zero;
        }

        //Frame update

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 goal = transform.position;
            float stop = stopDistance;
            float desiredSpeed = 0f;

            switch (mode)
            {
                case MoveMode.GoTo:
                    goal = destination;
                    desiredSpeed = FlatDistance(destination) > hurryDistance ? hurrySpeed : walkSpeed;
                    goToTimer += dt;
                    if (!HasArrived && goToTimer > goToGiveUpSeconds && !IsVisibleToCamera())
                    {
                        Warp(destination); // stuck out of sight, so pop to the spot
                        goToTimer = 0f;
                    }
                    break;

                case MoveMode.Follow:
                    if (followTarget == null)
                    {
                        Stop();
                        break;
                    }
                    desiredSpeed = PlanFollow(out goal, out stop);
                    break;
            }

            if (useAgent) MoveWithAgent(goal, stop, desiredSpeed);
            else MoveDirect(goal, stop, desiredSpeed, dt);

            UpdateFacing(dt);
        }

        private float PlanFollow(out Vector3 goal, out float stop)
        {
            Vector3 fromPlayer = transform.position - followTarget.position;
            fromPlayer.y = 0f;
            float distance = fromPlayer.magnitude;
            stop = 0.6f;

            if (distance > catchUpDistance && !IsVisibleToCamera())
            {
                CatchUp();
                goal = transform.position;
                return 0f;
            }

            Vector3 direction = distance > 0.01f ? fromPlayer / distance : -followTarget.forward;
            goal = followTarget.position + direction * followDistance;

            // Player walked right up to her, so step aside until there's room again.
            if (distance < personalSpace) makingRoom = true;
            else if (distance > personalSpace + 0.8f) makingRoom = false;
            if (makingRoom)
            {
                goal = MakeRoomSpot(direction);
                stop = 0.2f;
                return walkSpeed;
            }

            hasRoomSpot = false;
            if (distance < followDistance + 0.5f) return 0f; // close enough, just stand there
            return distance > hurryDistance ? hurrySpeed : walkSpeed;
        }

        /// <summary>A free spot off to the side, so she doesn't block the player's way.</summary>
        private Vector3 MakeRoomSpot(Vector3 fromPlayerDirection)
        {
            // Keep the same spot while the player stays put, so she walks there smoothly.
            Vector3 moved = followTarget.position - roomSpotFor;
            moved.y = 0f;
            if (hasRoomSpot && moved.sqrMagnitude < 1f) return roomSpot;

            Vector3 forward = followTarget.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 side = Vector3.Cross(Vector3.up, forward);
            float sign = Vector3.Dot(fromPlayerDirection, side) >= 0f ? 1f : -1f;
            Vector3 away = (fromPlayerDirection + side * sign * 1.2f).normalized;

            roomSpot = FindSpotAround(followTarget.position, away, personalSpace + 1.2f);
            roomSpotFor = followTarget.position;
            hasRoomSpot = true;
            return roomSpot;
        }

        /// <summary>A spot she can really walk to, trying the preferred direction first.</summary>
        public Vector3 FindSpotAround(Vector3 centre, Vector3 preferred, float distance)
        {
            preferred.y = 0f;
            if (preferred.sqrMagnitude < 0.0001f) preferred = Vector3.forward;
            preferred.Normalize();

            Vector3 fallback = centre + preferred * distance;
            if (!useAgent || !agent.isOnNavMesh) return fallback;
            if (path == null) path = new NavMeshPath();

            foreach (float angle in SearchAngles)
            {
                Vector3 candidate = centre + Quaternion.Euler(0f, angle, 0f) * preferred * distance;
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1f, NavMesh.AllAreas)) continue;

                // Snapped back towards the player (the spot was inside a wall)? Try another way.
                Vector3 flat = hit.position - centre;
                flat.y = 0f;
                if (flat.magnitude < distance * 0.7f) continue;

                // Reachable, and not by a long detour round a fence.
                if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) continue;
                if (PathLength(path) > FlatDistance(hit.position) * 2f + 3f) continue;

                return hit.position;
            }

            return fallback;
        }

        private static float PathLength(NavMeshPath navPath)
        {
            Vector3[] corners = navPath.corners;
            float length = 0f;
            for (int i = 1; i < corners.Length; i++) length += Vector3.Distance(corners[i - 1], corners[i]);
            return length;
        }

        private void MoveWithAgent(Vector3 goal, float stop, float desiredSpeed)
        {
            if (!agent.isOnNavMesh) return;

            agent.speed = Mathf.Max(0.01f, desiredSpeed);
            agent.acceleration = acceleration;
            agent.stoppingDistance = stop;

            if (mode != MoveMode.Idle && desiredSpeed > 0f)
            {
                if (!agent.hasPath || (agent.destination - goal).sqrMagnitude > 0.25f) agent.SetDestination(goal);
                agent.isStopped = false;
            }
            else
            {
                agent.isStopped = true;
            }

            velocity = agent.isStopped ? Vector3.MoveTowards(velocity, Vector3.zero, acceleration * Time.deltaTime) : agent.velocity;
            HasArrived = mode == MoveMode.Idle || (!agent.pathPending && agent.remainingDistance <= stop + 0.05f) || desiredSpeed <= 0f;
        }

        private void MoveDirect(Vector3 goal, float stop, float desiredSpeed, float dt)
        {
            Vector3 toGoal = goal - transform.position;
            toGoal.y = 0f;
            float distance = toGoal.magnitude;

            float targetSpeed = 0f;
            if (mode != MoveMode.Idle && desiredSpeed > 0f && distance > stop)
            {
                // Ease off as she gets close so she doesn't overshoot.
                targetSpeed = desiredSpeed * Mathf.Clamp01((distance - stop) / 1.2f + 0.25f);
            }

            Vector3 desiredVelocity = distance > 0.001f ? toGoal / distance * targetSpeed : Vector3.zero;
            velocity = Vector3.MoveTowards(velocity, desiredVelocity, acceleration * dt);

            Vector3 next = transform.position + velocity * dt;
            transform.position = SnapToGround(next);
            HasArrived = mode == MoveMode.Idle || distance <= stop + 0.05f || desiredSpeed <= 0f;
        }

        private void UpdateFacing(float dt)
        {
            float currentYaw = transform.eulerAngles.y;
            float? targetYaw = null;

            Vector3 flatVelocity = new Vector3(velocity.x, 0f, velocity.z);
            if (flatVelocity.magnitude > 0.25f)
            {
                targetYaw = Mathf.Atan2(flatVelocity.x, flatVelocity.z) * Mathf.Rad2Deg;
                turningInPlace = false;
            }
            else
            {
                Vector3? direction = faceDirection;
                if (!direction.HasValue && faceTarget != null)
                {
                    Vector3 d = faceTarget.position - transform.position;
                    d.y = 0f;
                    if (d.sqrMagnitude > 0.0001f) direction = d;
                }

                if (direction.HasValue)
                {
                    float wanted = Mathf.Atan2(direction.Value.x, direction.Value.z) * Mathf.Rad2Deg;
                    float off = Mathf.Abs(Mathf.DeltaAngle(currentYaw, wanted));
                    if (off > faceAngleThreshold) turningInPlace = true;
                    if (turningInPlace) targetYaw = wanted;
                    if (off < 4f) turningInPlace = false;
                }
                else
                {
                    turningInPlace = false;
                }
            }

            if (targetYaw.HasValue)
            {
                float yaw = Mathf.SmoothDampAngle(currentYaw, targetYaw.Value, ref yawVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }

            float newYaw = transform.eulerAngles.y;
            AngularVelocity = Mathf.DeltaAngle(lastYaw, newYaw) / dt;
            lastYaw = newYaw;
        }

        private Vector3 SnapToGround(Vector3 point)
        {
            Vector3 origin = point + Vector3.up * groundProbeHeight;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits, groundProbeHeight * 2f, groundMask, QueryTriggerInteraction.Ignore);

            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = groundHits[i];
                if (!IsGround(hit.collider)) continue;
                if (hit.distance >= bestDistance) continue;
                bestDistance = hit.distance;
                point.y = hit.point.y;
            }

            return point;
        }

        /// <summary>Ignore Gogo herself, the player, and small interactables like plots and pickups.</summary>
        private bool IsGround(Collider candidate)
        {
            if (candidate.transform.IsChildOf(transform)) return false;
            if (candidate is CharacterController) return false;
            GameObject other = candidate.gameObject;
            return !other.CompareTag("Player") && !other.CompareTag("Interactable");
        }

        private bool IsVisibleToCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return false;
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
            var bounds = new Bounds(transform.position + Vector3.up, new Vector3(1.5f, 3f, 1.5f));
            return GeometryUtility.TestPlanesAABB(planes, bounds);
        }

        private void CatchUp()
        {
            if (followTarget == null) return;
            Vector3 behind = followTarget.position - followTarget.forward * 7f + followTarget.right * 1.5f;

            if (useAgent)
            {
                if (NavMesh.SamplePosition(behind, out NavMeshHit hit, 5f, NavMesh.AllAreas)) agent.Warp(hit.position);
            }
            else
            {
                transform.position = SnapToGround(behind);
            }

            velocity = Vector3.zero;
        }
    }
}
