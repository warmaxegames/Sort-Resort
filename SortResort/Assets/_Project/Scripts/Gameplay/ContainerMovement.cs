using UnityEngine;
using System;
using System.Collections.Generic;

namespace SortResort
{
    /// <summary>
    /// Handles container movement patterns (back_and_forth, carousel, falling).
    /// Attach to containers that need to move.
    /// </summary>
    public class ContainerMovement : MonoBehaviour
    {
        public enum MoveType
        {
            None,
            BackAndForth,
            Carousel
        }

        public enum MoveDirection
        {
            Left,
            Right,
            Up,
            Down
        }

        [Header("Movement Settings")]
        [SerializeField] private MoveType moveType = MoveType.None;
        [SerializeField] private MoveDirection moveDirection = MoveDirection.Right;
        [SerializeField] private float moveSpeed = 50f;
        [SerializeField] private float moveDistance = 200f;

        [Header("Falling Settings")]
        [SerializeField] private bool isFalling;
        [SerializeField] private float fallSpeed = 100f;
        [SerializeField] private float fallTargetY;
        [SerializeField] private bool despawnOnMatch;
        [SerializeField] private float stackFallDistance = 1.98f; // Height of one container in Unity units (198px)

        [Header("State")]
        [SerializeField] private bool isMoving = true;
        [SerializeField] private bool isPaused;

        // Internal state
        private Vector3 startPosition;
        private Vector3 endPosition;
        private float currentProgress; // 0 to 1 for back_and_forth
        private int direction = 1; // 1 = forward, -1 = backward
        private bool hasFallen;
        private ItemContainer container;
        private bool isStackFalling; // For chain-reaction falling
        private float stackFallTargetY;

        // Static tracking for stack-based falling
        private static List<ContainerMovement> allStackContainers = new List<ContainerMovement>();
        private static event Action<ContainerMovement> OnContainerDespawned;

        public bool IsMoving => isMoving && !isPaused;
        public bool IsFalling => isFalling && !hasFallen;
        public bool DespawnOnMatch => despawnOnMatch;

        private void Awake()
        {
            container = GetComponent<ItemContainer>();
        }

        /// <summary>
        /// Initialize movement from container definition
        /// </summary>
        public void Initialize(ContainerDefinition definition)
        {
            if (definition.is_moving)
            {
                moveType = ParseMoveType(definition.move_type);
                moveDirection = ParseDirection(definition.move_direction);
                moveSpeed = definition.move_speed > 0 ? definition.move_speed : 50f;
                moveDistance = definition.move_distance > 0 ? definition.move_distance : 200f;
                isMoving = true;

                CalculateEndPosition();

                Debug.Log($"[ContainerMovement] {definition.id} initialized: type={moveType}, dir={moveDirection}, speed={moveSpeed}, distance={moveDistance}");
            }

            if (definition.is_falling)
            {
                isFalling = true;
                fallSpeed = definition.fall_speed > 0 ? definition.fall_speed : 100f;
                // Convert Godot Y to Unity Y
                fallTargetY = (600f - definition.fall_target_y) / 100f;
                despawnOnMatch = definition.despawn_on_match;

                Debug.Log($"[ContainerMovement] {definition.id} falling: speed={fallSpeed}, targetY={fallTargetY}");
            }

            // For despawn-on-match containers, also check definition directly
            if (definition.despawn_on_match)
            {
                despawnOnMatch = true;
                fallSpeed = definition.fall_speed > 0 ? definition.fall_speed : 200f;
            }
        }

        private void Start()
        {
            startPosition = transform.position;
            CalculateEndPosition();

            if (container != null && despawnOnMatch)
            {
                container.OnContainerEmpty += OnContainerEmpty;

                // Register for stack-based falling
                allStackContainers.Add(this);
                OnContainerDespawned += HandleContainerDespawned;
            }
        }

        private void CalculateEndPosition()
        {
            Vector3 directionVector = GetDirectionVector();
            // Convert pixel distance to Unity units (100 pixels per unit)
            float unityDistance = moveDistance / 100f;
            endPosition = startPosition + directionVector * unityDistance;
        }

        private Vector3 GetDirectionVector()
        {
            return moveDirection switch
            {
                MoveDirection.Left => Vector3.left,
                MoveDirection.Right => Vector3.right,
                MoveDirection.Up => Vector3.up,
                MoveDirection.Down => Vector3.down,
                _ => Vector3.right
            };
        }

        private void Update()
        {
            if (isPaused) return;

            // Stack falling takes priority (Tetris-like effect)
            if (isStackFalling)
            {
                UpdateStackFalling();
                return;
            }

            if (isFalling && !hasFallen)
            {
                UpdateFalling();
            }
            else if (isMoving && moveType != MoveType.None)
            {
                UpdateMovement();
            }
        }

        private void UpdateStackFalling()
        {
            float unitySpeed = fallSpeed / 100f;
            float step = unitySpeed * Time.deltaTime;

            Vector3 pos = transform.position;
            if (pos.y > stackFallTargetY)
            {
                pos.y -= step;
                if (pos.y <= stackFallTargetY)
                {
                    pos.y = stackFallTargetY;
                    isStackFalling = false;
                    Debug.Log($"[ContainerMovement] {gameObject.name} finished stack falling to Y={stackFallTargetY}");
                }
                transform.position = pos;
            }
            else
            {
                isStackFalling = false;
            }
        }

        /// <summary>
        /// Called when another container in a stack despawns - check if we need to fall
        /// </summary>
        private void HandleContainerDespawned(ContainerMovement despawnedContainer)
        {
            if (despawnedContainer == this) return;
            if (this == null || gameObject == null) return;

            // Check if we're in the same column (same X position, within tolerance)
            float xTolerance = 0.1f;
            if (Mathf.Abs(transform.position.x - despawnedContainer.transform.position.x) > xTolerance)
                return;

            // Check if we're above the despawned container
            if (transform.position.y <= despawnedContainer.transform.position.y)
                return;

            // We're above a despawned container in the same column - fall down!
            Debug.Log($"[ContainerMovement] {gameObject.name} falling due to {despawnedContainer.gameObject.name} despawning");
            TriggerStackFall(stackFallDistance);
        }

        /// <summary>
        /// Start falling down by a specified distance
        /// </summary>
        public void TriggerStackFall(float distance)
        {
            stackFallTargetY = transform.position.y - distance;
            isStackFalling = true;
        }

        private void UpdateMovement()
        {
            // Calculate speed in Unity units per second
            float unitySpeed = moveSpeed / 100f;
            float distanceThisFrame = unitySpeed * Time.deltaTime;

            switch (moveType)
            {
                case MoveType.BackAndForth:
                    UpdateBackAndForth(distanceThisFrame);
                    break;
                case MoveType.Carousel:
                    UpdateCarousel(distanceThisFrame);
                    break;
            }
        }

        private void UpdateBackAndForth(float distanceThisFrame)
        {
            // Calculate total distance in Unity units
            float totalDistance = Vector3.Distance(startPosition, endPosition);
            if (totalDistance <= 0) return;

            // Update progress
            float progressDelta = distanceThisFrame / totalDistance;
            currentProgress += progressDelta * direction;

            // Reverse at ends
            if (currentProgress >= 1f)
            {
                currentProgress = 1f;
                direction = -1;
            }
            else if (currentProgress <= 0f)
            {
                currentProgress = 0f;
                direction = 1;
            }

            // Apply position
            transform.position = Vector3.Lerp(startPosition, endPosition, currentProgress);
        }

        private void UpdateCarousel(float distanceThisFrame)
        {
            // Move continuously in one direction
            Vector3 directionVector = GetDirectionVector();
            transform.position += directionVector * distanceThisFrame;

            // For seamless train effect:
            // - Detect wrap when container exits screen edge
            // - Wrap BY the train length (moveDistance) to maintain spacing between containers
            float screenLeftEdge = -6.5f;   // A bit past left edge
            float screenRightEdge = 6.5f;   // A bit past right edge
            float trainLength = moveDistance / 100f; // Use configured move_distance as train length

            Vector3 pos = transform.position;

            if (moveDirection == MoveDirection.Right)
            {
                // Moving right - when past right edge, wrap back by train length
                if (pos.x > screenRightEdge)
                {
                    pos.x -= trainLength;
                    transform.position = pos;
                }
            }
            else if (moveDirection == MoveDirection.Left)
            {
                // Moving left - when past left edge, wrap forward by train length
                if (pos.x < screenLeftEdge)
                {
                    pos.x += trainLength;
                    transform.position = pos;
                }
            }
            else if (moveDirection == MoveDirection.Down)
            {
                // Moving down - wrap by train length only (no extra offset)
                // Position enough containers in JSON to cover visible area + buffer
                float screenBottom = -10f;
                if (pos.y < screenBottom)
                {
                    pos.y += trainLength;
                    transform.position = pos;
                }
            }
            else if (moveDirection == MoveDirection.Up)
            {
                // Moving up - wrap by train length only
                float screenTop = 10f;
                if (pos.y > screenTop)
                {
                    pos.y -= trainLength;
                    transform.position = pos;
                }
            }
        }

        private void UpdateFalling()
        {
            float unitySpeed = fallSpeed / 100f;
            float step = unitySpeed * Time.deltaTime;

            // Move toward target Y
            Vector3 pos = transform.position;
            float targetY = fallTargetY;

            if (pos.y > targetY)
            {
                pos.y -= step;
                if (pos.y <= targetY)
                {
                    pos.y = targetY;
                    hasFallen = true;
                    OnFallComplete();
                }
                transform.position = pos;
            }
            else
            {
                hasFallen = true;
            }
        }

        private void OnFallComplete()
        {
            Debug.Log($"[ContainerMovement] {gameObject.name} finished falling");

            // Start normal movement if configured
            if (moveType != MoveType.None)
            {
                startPosition = transform.position;
                CalculateEndPosition();
            }
        }

        private void OnContainerEmpty(ItemContainer c)
        {
            if (despawnOnMatch)
            {
                Debug.Log($"[ContainerMovement] {gameObject.name} despawning after becoming empty");

                // Notify other containers BEFORE we animate out
                OnContainerDespawned?.Invoke(this);

                // Animate out and destroy
                LeanTween.scale(gameObject, Vector3.zero, 0.3f)
                    .setEase(LeanTweenType.easeInBack)
                    .setOnComplete(() => Destroy(gameObject));
            }
        }

        /// <summary>
        /// Pause/resume movement
        /// </summary>
        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        /// <summary>
        /// Stop movement entirely
        /// </summary>
        public void StopMovement()
        {
            isMoving = false;
            isFalling = false;
        }

        private MoveType ParseMoveType(string type)
        {
            if (string.IsNullOrEmpty(type)) return MoveType.None;

            return type.ToLower() switch
            {
                "back_and_forth" => MoveType.BackAndForth,
                "backandforth" => MoveType.BackAndForth,
                "carousel" => MoveType.Carousel,
                _ => MoveType.None
            };
        }

        private MoveDirection ParseDirection(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return MoveDirection.Right;

            return dir.ToLower() switch
            {
                "left" => MoveDirection.Left,
                "right" => MoveDirection.Right,
                "up" => MoveDirection.Up,
                "down" => MoveDirection.Down,
                _ => MoveDirection.Right
            };
        }

        private void OnDestroy()
        {
            if (container != null)
            {
                container.OnContainerEmpty -= OnContainerEmpty;
            }

            // Clean up stack tracking
            allStackContainers.Remove(this);
            OnContainerDespawned -= HandleContainerDespawned;

            LeanTween.cancel(gameObject);
        }

        /// <summary>
        /// Clear all static references (call when loading a new level)
        /// </summary>
        public static void ClearAllStackContainers()
        {
            allStackContainers.Clear();
            OnContainerDespawned = null;
        }
    }
}
