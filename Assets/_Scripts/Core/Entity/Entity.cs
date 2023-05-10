using System;

using UnityEngine;

using Animancer;

using SevenGame.Utility;
using System.Reflection;

namespace SeleneGame.Core {

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CustomPhysicsComponent))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(AnimancerComponent))]
    [RequireComponent(typeof(Health))]
    [DisallowMultipleComponent]
    [SelectionBase]
    public class Entity : MonoBehaviour, IDamageable {
        

        public const float LIGHTWEIGHT_THRESHOLD = 20f;
        public const float HEAVYWEIGHT_THRESHOLD = 50f;

        
    
        [Tooltip("The entity's current Animator.")]
        [SerializeField][HideInInspector][ReadOnly] private Animator _animator;
        [Tooltip("The entity's current Animator.")]
        [SerializeField][HideInInspector][ReadOnly] private AnimancerComponent _animancer;

        [Tooltip("The entity's current Rigidbody.")]
        [SerializeField][HideInInspector][ReadOnly] private Rigidbody _rigidbody;
        
        [Tooltip("The entity's current Physics Script.")]
        [SerializeField][HideInInspector][ReadOnly] private CustomPhysicsComponent _physicsComponent;

        [Tooltip("The entity's current Health System.")]
        [SerializeField][HideInInspector][ReadOnly] private Health _health;
    

        [Header("Entity Data")]
        
        [Tooltip("The entity's current Character, defines their game Model, portraits, display name and base Stats.")]
        [SerializeReference] private Character _character;

        [Tooltip("The current state of the Entity, can be changed using the SetState method.")]
        [SerializeReference] private EntityBehaviour _behaviour;


        [Header("Movement")]

        [Tooltip("The forward direction in absolute space of the Entity.")]
        [SerializeField] private Vector3 _absoluteForward;
        
        [Tooltip("The forward direction in relative space of the Entity.")]
        [SerializeField] private Vector3 _relativeForward;

        [Tooltip("The direction in which the Entity is currently moving.")]
        public Vector3Data moveDirection;

        private Vector3 _totalMovement = Vector3.zero; 



        public event Action<float> onHeal;
        public event Action<float, DamageType> onDamage;

        public event Action<float> onHealed;
        public event Action<DamageData> onDamaged;
        public event Action onDeath;

        public event Action<Character> onSetCharacter;


    
        [Header("Gravity")]

        [Tooltip("The direction in which the Entity is attracted by Gravity.")]
        public Vector3 gravityDown = Vector3.down;

        [SerializeField] private Vector3 _inertia = Vector3.zero;

        [Tooltip("If the Entity is currently on the ground.")] 
        public BoolData onGround;
        public BoolData groundDetected;

        public RaycastHit groundHit;
        private Transform anchorTransform;

        public event Action<Vector3> onJump;
        public event Action<Vector3> onEvade;
        public event Action<DamageData> onParry;



        /// <summary>
        /// The entity's health system.
        /// </summary>
        public Health health => _health;

        /// <summary>
        /// The entity's Animator system.
        /// </summary>
        public Animator animator => _animator;

        /// <summary>
        /// The entity's Animancer component.
        /// </summary>
        public AnimancerComponent animancer => _animancer;

        /// <summary>
        /// The entity's Rigidbody.
        /// </summary>
        public new Rigidbody rigidbody => _rigidbody;

        /// <summary>
        /// The entity's Physics Script.
        /// </summary>
        public CustomPhysicsComponent physicsComponent => _physicsComponent;

        /// <summary>
        /// Returns an Armature Bone by name.
        /// </summary>
        /// <remarks>
        /// If the bone is not found, the main Transform of the Entity's Model is returned instead.
        /// </remarks>
        public GameObject this[string key]{
            get { 
                try { 
                    return character.model.costumeData.bones[key]; 
                } catch { 
                    return character?.model?.mainTransform?.gameObject ?? gameObject; 
                } 
            }
        }


        /// <summary>
        /// The entity's current Character, defines their Game Model, portraits, display name and base Stats.
        /// </summary>
        /// <remarks>
        /// You can set this value by calling <see cref="SetCharacter"/> and providing CharacterData.
        /// </remarks>
        public Character character {
            get => _character;
        }
        
        /// <summary>
        /// The main transform of the current Costume's model.
        /// </summary>
        public Transform modelTransform { 
            get {
                if (character == null || character.model == null)
                    return null;
                return character.model.mainTransform;
            }
        }

        /// <summary>
        /// The current state (Behaviour) of the Entity.
        /// </summary>
        /// <remarks>
        /// You can change the State using <see cref="SetBehaviour"/>.
        /// </remarks>
        public EntityBehaviour behaviour {
            get {
                if ( _behaviour == null )
                    ResetBehaviour();

                return _behaviour;
            }
        }


        /// <summary>
        /// The forward direction in absolute space of the Entity.
        /// </summary>
        /// <remarks>
        /// Editing this value also changes <see cref="relativeForward"/> to match.
        /// </remarks>
        public Vector3 absoluteForward { 
            get => _absoluteForward; 
            set { 
                _absoluteForward = value; 
                _relativeForward = Quaternion.Inverse(transform.rotation) * value; 
            } 
        }
        
        /// <summary>
        /// The forward direction in relative space of the Entity.
        /// </summary>
        /// <remarks>
        /// Editing this value also changes <see cref="absoluteForward"/> to match.
        /// </remarks>
        public Vector3 relativeForward { 
            get => _relativeForward; 
            set { 
                _relativeForward = value;
                _absoluteForward = transform.rotation * value;
            }
        }

        /// <summary>
        /// Returns true if the Entity is not moving.
        /// </summary>
        public bool isIdle => moveDirection.sqrMagnitude == 0;

        /// <summary>
        /// The strength of gravity applied to the Entity.
        /// </summary>
        public float gravityMultiplier => weight * behaviour?.gravityMultiplier ?? 1f;

        /// <summary>
        /// The strength of gravity applied to the Entity.
        /// </summary>
        public Vector3 gravityForce => gravityMultiplier * gravityDown;

        public float fallVelocity => Vector3.Dot(inertia, -gravityDown);

        public virtual float jumpMultiplier => 1f;
        

        public Vector3 inertia {
            get => _inertia;
            set {
                
                _inertia = value;
            }
        }

        public virtual float weight => character?.data?.weight ?? 1f;
        public WeightCategory weightCategory {
            get {
                switch ( weight ) {
                    case float i when (i <= LIGHTWEIGHT_THRESHOLD):
                        return WeightCategory.Light;
                    case float i when (i > LIGHTWEIGHT_THRESHOLD && i < HEAVYWEIGHT_THRESHOLD):
                        return WeightCategory.Medium;
                    case float i when (i >= HEAVYWEIGHT_THRESHOLD):
                        return WeightCategory.Heavy;
                    default:
                        return WeightCategory.Medium;
                }
            }
        }

        public bool isPlayer => Player.current?.entity == this;

        public bool inWater => physicsComponent.inWater;




        /// <summary>
        /// Create an Entity using the given Parameters
        /// </summary>
        /// <param name="entityType">The type of entity to create</param>
        /// <param name="character">The character of the entity to create</param>
        /// <param name="position">The position of the entity</param>
        /// <param name="rotation">The rotation of the entity</param>
        /// <param name="costume">The costume of the entity, leave empty to use character's default costume</param>
        public static Entity CreateEntity(System.Type entityType, Vector3 position, Quaternion rotation, CharacterData data, CharacterCostume costume = null) {
            GameObject entityGO = new GameObject("Entity");
            Entity entity = (Entity)entityGO.AddComponent(entityType);
            entityGO.AddComponent<EntityController>();

            entity.SetCharacter(data, costume);

            entity.transform.position = position;
            entity.transform.rotation = rotation;
            return entity;
        }

        /// <summary>
        /// Create an Entity with a PlayerEntityController.
        /// </summary>
        /// <param name="entityType">The type of entity to create</param>
        /// <param name="position">The position of the entity</param>
        /// <param name="rotation">The rotation of the entity</param>
        /// <param name="character">The character of the entity to create</param>
        /// <param name="costume">The costume of the entity, leave empty to use character's default costume</param>
        public static Entity CreatePlayerEntity(System.Type entityType, Vector3 position, Quaternion rotation, CharacterData data, CharacterCostume costume = null) {
            GameObject entityGO = new GameObject("Entity");
            Entity entity = (Entity)entityGO.AddComponent(entityType);
            entityGO.AddComponent<Player>();

            entity.SetCharacter(data, costume);

            entity.transform.position = position;
            entity.transform.rotation = rotation;

            return entity;
        }


        [ContextMenu("Set As Player Entity")]
        public void SetAsPlayer() {
            GameUtility.SafeDestroy(gameObject.GetComponent<EntityController>());
            gameObject.AddComponent<Player>();
            // Character.SetInstanceWithId("Player", character);
        }


        public void SetBehaviour<T>(EntityBehaviourBuilder<T> stateBuilder) where T : EntityBehaviour {
            if ( stateBuilder == null )
                throw new System.ArgumentNullException( nameof(stateBuilder) );

            if ( _behaviour is T )
                return;

            EntityBehaviour newState = stateBuilder.Build(this, _behaviour);
            _behaviour?.Dispose();
            _behaviour = newState;
        }

        public virtual void ResetBehaviour() {
            SetBehaviour( GroundedBehaviourBuilder.Default );
        }


        /// <summary>
        /// Set the Entity's current Character.
        /// </summary>
        /// <param name="characterData">The data of the Character</param>
        /// <param name="characterCostume">The costume to give the character, leave null to use CharacterData's base costume</param>
        public virtual void SetCharacter(CharacterData characterData, CharacterCostume costume = null) {

            _character?.Dispose();
            _character = characterData?.GetCharacter(this, costume) ?? null;

            _health.maxAmount = _character?.data?.maxHealth ?? 1f;

            _physicsComponent.size = _character?.data?.size ?? Vector3.one;

            onSetCharacter?.Invoke(_character);
        }

        /// <summary>
        /// Set the Entity's current Character Costume.
        /// </summary>
        /// <param name="costumeName">The name of the new Character Costume</param>
        public void SetCostume(string costumeName) {
            _character?.SetCostume(costumeName);
        }

        /// <summary>
        /// Set the Entity's current Character Costume.
        /// </summary>
        /// <param name="costume">The new Character Costume</param>
        public void SetCostume(CharacterCostume costume) {
            _character?.SetCostume(costume);
        }


        /// <summary>
        /// Set the current "Fighting Style" of the Entity
        /// (e. g. the different equipped weapons, the current stance, etc.)
        /// </summary>
        /// <param name="newStyle">The style to set the Entity to</param>
        public virtual void SetStyle(int newStyle){;}


        public virtual void HandleInput(Player controller) {
            behaviour?.HandleInput(controller);
        }
        public virtual void HandleAI(AIController controller) {
            behaviour?.HandleAI(controller);
        }


        public virtual void Move(Vector3 direction) {
            behaviour?.Move(direction);
        }
        public virtual void Jump() {
            behaviour?.Jump();
        }
        public virtual void Evade(Vector3 direction) {
            behaviour?.Evade(direction);
        }
        public virtual void LightAttack() {
            behaviour?.LightAttack();
        }
        public virtual void HeavyAttack() {
            behaviour?.HeavyAttack();
        }
        public virtual void SetSpeed(MovementSpeed speed) {
            behaviour?.SetSpeed(speed);
        }


        /// <summary>
        /// Deal damage to the Entity.
        /// </summary>
        /// <param name="amount">The amount of damage done to the Entity</param>
        /// <param name="knockback">The direction of Knockback applied through the damage</param>
        public void Damage(DamageData damageData) {

            if (damageData.owner == this) return;

            if (isPlayer || damageData.owner.isPlayer) {

                if (damageData.damageType == DamageType.Critical)
                    EntityManager.current.HardHitStop();
                else
                    EntityManager.current.SoftHitStop();
            }

            damageData.owner?.AwardDamage(damageData.amount, damageData.damageType);

            _health.amount -= damageData.amount;

            if (_health.amount == 0f)
                Kill();

            // TODO: Add actual knockback animations
            

            onDamaged?.Invoke(damageData);
        }

        /// <summary>
        /// Heal the Entity.
        /// </summary>
        /// <param name="amount">The amount of health the Entity is healed</param>
        public void Heal(float amount) {

            _health.amount += amount;

            onHealed?.Invoke(amount);
        }

        public void AwardDamage(float amount, DamageType damageType) {
            onDamage?.Invoke(amount, damageType);
        }

        public void AwardParry(DamageData damageData) {
            onParry?.Invoke(damageData);
        }

        /// <summary>
        /// Initiate the Entity's death sequence.
        /// </summary>
        public virtual void Kill(){
            onDeath?.Invoke();
        }

        /// <summary>
        /// Move in the given direction.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This is not supposed to be used to instruct the Entity to "Walk" to a specific position; 
        ///         use <see cref="Move"/> for that.
        ///     </para>
        ///     <para>
        ///         This is just used to change the Entity's position.
        ///     </para>
        /// </remarks>
        /// <param name="direction">The direction to move in</param>
        /// <param name="canStep">If the Entity can move up or down stair steps, like on a slope.</param>
        public void Displace(Vector3 direction, float deltaTime = -1f) {
            if (direction.sqrMagnitude == 0f) return;

            if (deltaTime < 0f) deltaTime = GameUtility.timeDelta;

            _totalMovement += direction * deltaTime;

        }

        public bool DisplaceStep(Vector3 direction, float deltaTime = -1f) {
            if (direction.sqrMagnitude == 0f) return false;

            if (deltaTime < 0f) deltaTime = GameUtility.timeDelta;

            // Check for valid walk
            Vector3 displacement = direction * deltaTime;
            if ( !onGround || !character.model.ColliderCast(Vector3.zero, displacement.normalized * (displacement.magnitude + 0.15f), out RaycastHit walkHit, out _, 0.15f, CollisionUtils.EntityCollisionMask))
                return false;


            // Check for valid step
            Vector3 checkOffset = gravityDown * character.data.stepHeight;
            if (!character.model.ColliderCast(displacement - checkOffset, checkOffset, out RaycastHit stepHit, out Collider castOrigin, 0f, CollisionUtils.EntityCollisionMask))
                return false;

            // Check if the step is low enough
            Vector3 stepDisplacement = Vector3.Project(stepHit.point - groundHit.point, gravityDown);
            if (stepDisplacement.sqrMagnitude > character.data.stepHeight * character.data.stepHeight)
                return false;


            transform.position += displacement + stepDisplacement;
            return true;

        }
        
        public void DisplaceImmediate(Vector3 displacement, bool fixClipping = true/* , bool fixPenetration = true */) {
            if (displacement.sqrMagnitude == 0f) return;


            // stop the character from clipping into obstacles
            RaycastHit walkHit = default;
            Collider castOrigin = default;
            bool castHit = fixClipping && character.model.ColliderCast(Vector3.zero, displacement.normalized * (displacement.magnitude + 0.15f), out walkHit, out castOrigin, 0.15f, CollisionUtils.EntityCollisionMask);

            if (castHit) {
                displacement = displacement.normalized * Mathf.Min(displacement.magnitude, walkHit.distance);
            }


            transform.position += displacement;


            // move the character out of the object it's clipping into
            Vector3 direction = default;
            float distance = default;
            bool penetrationHit = /* fixPenetration &&  */castHit && Physics.ComputePenetration(castOrigin, castOrigin.transform.position, castOrigin.transform.rotation, walkHit.collider, walkHit.collider.transform.position, walkHit.collider.transform.rotation, out direction, out distance);

            if (penetrationHit) {
                transform.position += direction * distance;
            }


            Physics.SyncTransforms();
        }

        public void DisplaceTo(Vector3 position, float deltaTime = -1f) {
            Displace(position - transform.position, deltaTime);
        }



        /// <summary>
        /// Apply gravity to the Entity.
        /// </summary>
        private void Gravity() {

            if (gravityMultiplier == 0f || weight == 0f || gravityDown == Vector3.zero) return;

            Vector3 verticalInertia = Vector3.Project( inertia, gravityDown );
            Vector3 horizontalInertia = inertia - verticalInertia;

            verticalInertia = Vector3.MoveTowards( verticalInertia, gravityForce, 35f * GameUtility.timeDelta );
            horizontalInertia = Vector3.MoveTowards( horizontalInertia, Vector3.zero, 0.25f * GameUtility.timeDelta );
            
            inertia = horizontalInertia + verticalInertia;

            if (onGround)
                inertia = inertia.NullifyInDirection(gravityDown);

        }

        /// <summary>
        /// Apply all instructed movement to the Entity.
        /// This is where collision is calculated.
        /// </summary>
        private void ExecuteMovement() {

            moveDirection.SetVal( _totalMovement.normalized );

            Vector3 totalDisplacement = _totalMovement + (inertia * GameUtility.timeDelta);

            DisplaceImmediate(totalDisplacement);

            Physics.SyncTransforms();
            
            _totalMovement = Vector3.zero;
        }

        private void EntityCollision() {
                
            if (character.model == null) return;

            Collider[] overlaps = character.model.ColliderOverlap(0f, CollisionUtils.EntityObjectMask);

            foreach (Collider overlap in overlaps) {
                
                Vector3 displacement = Vector3.ProjectOnPlane((transform.position - overlap.transform.position), groundHit.normal);

                _totalMovement += displacement * GameUtility.timeDelta * 2f;
            }
    
        }

        protected virtual void Start(){
            transform.rotation = Quaternion.identity;
            absoluteForward = transform.forward;
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            rigidbody.interpolation = RigidbodyInterpolation.None;
            
            GameUtility.SetLayerRecursively(gameObject, CollisionUtils.EntityObjectLayer);
            

            if (character == null) {
                Debug.LogError($"Entity {name} has no Character assigned!", this);
                enabled = false;
            }
        }

        private void Reset() => ResetEntity();
    
        [ContextMenu("Reset")]
        private void ResetEntity() => EntityReset();
        protected virtual void EntityReset(){
            _animator ??= GetComponent<Animator>();
            _animancer ??= GetComponent<AnimancerComponent>();
            _rigidbody ??= GetComponent<Rigidbody>();
            _physicsComponent ??= GetComponent<CustomPhysicsComponent>();
            _health ??= GetComponent<Health>();

            _character?.Dispose();
            _character = null;
        }

        protected virtual void OnEnable(){
            EntityManager.current.entityList.Add( this );
        }
        protected virtual void OnDisable(){
            EntityManager.current.entityList.Remove( this );
        }
        
        protected virtual void OnDestroy(){;}

        protected virtual void Update(){

            bool closeToGround = false;
            bool castHit = character.model.ColliderCast(Vector3.zero, gravityDown.normalized * 0.35f, out groundHit, out Collider castOrigin, 0.15f, CollisionUtils.EntityCollisionMask);
            if (castHit) {
                closeToGround = castOrigin.ColliderCast(castOrigin.transform.position, gravityDown.normalized * 0.1f, out _, 0.15f, CollisionUtils.EntityCollisionMask);
            }
            groundDetected.SetVal( castHit );
            onGround.SetVal( closeToGround );


            behaviour?.Update();
            character?.Update();

        }
        

        protected virtual void LateUpdate(){

            behaviour?.LateUpdate();
            character?.LateUpdate();

            Gravity();
            EntityCollision();
            ExecuteMovement();
        }

        protected virtual void FixedUpdate() {

            behaviour?.FixedUpdate();
            character?.FixedUpdate();
        }

        // NOTE : this hasn't been tested yet. This is supposed to handle collisions for Root Motion Animations.
        protected virtual void OnAnimatorMove() {
            Vector3 deltaPosition = _animator.deltaPosition;
            Vector3 movement = deltaPosition / GameUtility.timeDelta;
            if (movement.sqrMagnitude > 0f) {
                _totalMovement += movement;
            }
        }

        protected virtual void OnTriggerEnter(Collider collider) {
            if (collider.gameObject.tag != "Anchor")
                return;

            anchorTransform = collider.transform;
            transform.SetParent(this.anchorTransform);
        }

        protected virtual void OnTriggerStay(Collider collider) {
            if (!anchorTransform)
                OnTriggerEnter(collider);
        }

        protected virtual void OnTriggerExit(Collider collider) {
            if (collider.transform == anchorTransform) {
                transform.SetParent(null);
                anchorTransform = null;
            }
        }

        

        public enum WeightCategory {
            Light,
            Medium,
            Heavy
        }

        public enum MovementSpeed {
            Idle,
            Slow,
            Normal,
            Fast
        }

    }
}