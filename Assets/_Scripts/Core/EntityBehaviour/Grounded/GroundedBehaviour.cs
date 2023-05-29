using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SeleneGame.Core.UI;

using Animancer;

using SevenGame.Utility;
using System;

namespace SeleneGame.Core {
    
    [System.Serializable]
    public partial class GroundedBehaviour : EntityBehaviour {


        public Entity.MovementSpeed movementSpeed = Entity.MovementSpeed.Normal;

        private Vector3 rotationForward;
        private Vector3Data moveDirection;
        public float moveSpeed;




        public override float gravityMultiplier => 1f;
        // public override Vector3 cameraPosition {
        //     get {
        //         float dynamicCameraDistance = moveSpeed;
        //         if (entity.character.baseSpeed > 0f)
        //             dynamicCameraDistance /= entity.character.baseSpeed;

        //         return base.cameraPosition - new Vector3(0, 0, dynamicCameraDistance);
        //     }
        // }
        public override CameraController.CameraType cameraType => CameraController.CameraType.ThirdPersonGrounded;


        protected override Vector3 jumpDirection => base.jumpDirection;

        protected override Vector3 evadeDirection => moveDirection.sqrMagnitude == 0f ? -entity.absoluteForward : base.evadeDirection;

        protected override bool canParry => base.canParry;


        public override Vector3 direction => moveDirection;
        public override float speed => moveSpeed;



        public GroundedBehaviour(Entity entity, EntityBehaviour previousBehaviour) : base(entity, previousBehaviour) {

            _evadeBehaviour = new GroundedEvadeBehaviour(entity);
            _jumpBehaviour = new GroundedJumpBehaviour(entity);

            AnimationInitialize();

            IdleAnimation();

            if (previousBehaviour == null) return;

            Move(previousBehaviour.direction);
            moveSpeed = previousBehaviour.speed;

            entity.onSetCharacter += OnSetCharacter;
        }

        protected override void DisposeBehavior() {
            base.DisposeBehavior();
            
            entity.onSetCharacter -= OnSetCharacter;
        }

        private void OnSetCharacter(Character character) {
            AnimationInitialize();
        }

        protected internal override void HandleInput(Player controller){

            base.HandleInput(controller);

            controller.RawInputToGroundedMovement(out Quaternion cameraRotation, out Vector3 groundedMovement);

            if (groundedMovement.sqrMagnitude <= 0f)
                SetSpeed(Entity.MovementSpeed.Idle);
            else if ( DialogueController.current.Enabled || ((groundedMovement.sqrMagnitude <= 0.25 || controller.walkInput) && entity.onGround) ) 
                SetSpeed(Entity.MovementSpeed.Slow);
            else if ( controller.evadeInput )
                SetSpeed(Entity.MovementSpeed.Fast);
            else
                SetSpeed(Entity.MovementSpeed.Normal);
                

            Move(groundedMovement);

            if ( controller.evadeInput.Tapped() )
                Evade(evadeDirection);

            // if ( KeyInputData.SimultaneousTap( controller.lightAttackInput, controller.heavyAttackInput ) ){
            //     // parry
            // }
                
            if ( controller.jumpInput )
                Jump();
        }


        protected internal override void Move(Vector3 direction) {
            this.moveDirection.SetVal( Vector3.ClampMagnitude(direction, 1f) );
        }
        protected internal override void Jump() {
            base.Jump();
        }
        protected internal override void Evade(Vector3 direction) {
            base.Evade(direction);
        }
        protected internal override void LightAttack() {
            base.LightAttack();
        }
        protected internal override void HeavyAttack() {
            base.HeavyAttack();
        }
        protected internal override void SetSpeed(Entity.MovementSpeed speed) {
            
            if (entity.onGround && !evadeBehaviour.state) {
                if (speed == Entity.MovementSpeed.Idle) {

                    MovementStopAnimation();
                
                } else if ((int)speed > (int)movementSpeed) {

                    MovementStartAnimation(speed);

                }
            }
            movementSpeed = speed;
        }
        

        private bool HandleWaterHover() {

            if ( entity.character.model.ColliderCast( Vector3.zero, entity.gravityDown, out RaycastHit hit, out _, 0.15f, CollisionUtils.WaterMask ) ) {
                entity.groundHit = hit;
                entity.onGround.SetVal(true);
                return true;
            }

            return false;
        }


        public override void Update(){

            base.Update();

            // Update the Entity's up direction
            entity.transform.rotation = Quaternion.FromToRotation(entity.transform.up, -entity.gravityDown) * entity.transform.rotation;

            
            // Hover over water as long as the entity is moving
            bool canWaterHover = entity.weightCategory == Entity.WeightCategory.Light && moveDirection.zeroTimer < 0.6f;
            bool waterHover = canWaterHover && HandleWaterHover();

            // If the entity can do it, swim in water
            bool canSwim = !waterHover && entity.weightCategory != Entity.WeightCategory.Heavy;
            float distanceToWaterSurface = entity.physicsComponent.totalWaterHeight - entity.transform.position.y;
            
            if ( entity.inWater && canSwim && distanceToWaterSurface > 0f ) {
                entity.SetBehaviour( SwimmingBehaviourBuilder.Default );
            }

            if ( entity.onGround ) {
                entity.inertia = Vector3.MoveTowards(entity.inertia, Vector3.zero, 10f * GameUtility.timeDelta);
            }


            if ( moveDirection.sqrMagnitude != 0f ) {

                Vector3 groundedMovement = moveDirection;
                if (entity.groundDetected) {
                    groundedMovement = Quaternion.FromToRotation(-entity.gravityDown, entity.groundHit.normal) * groundedMovement;
                    Debug.DrawRay(entity.transform.position, entity.groundHit.normal, Color.red);
                    Debug.DrawRay(entity.transform.position, groundedMovement, Color.blue);
                }

                entity.absoluteForward = Vector3.Slerp( entity.absoluteForward, groundedMovement, 100f * GameUtility.timeDelta);
            }



            if ( !_evadeBehaviour.state ) {
                rotationForward = Vector3.ProjectOnPlane(entity.absoluteForward, -entity.gravityDown).normalized;
            } else if ( moveDirection.sqrMagnitude != 0f ) {
                _evadeBehaviour.currentDirection = Vector3.Lerp(_evadeBehaviour.currentDirection, entity.absoluteForward, _evadeBehaviour.Time * _evadeBehaviour.Time);
            }
            
            entity.character.model.RotateTowards(rotationForward, -entity.gravityDown);


            float newSpeed = 0f;
            if (moveDirection.sqrMagnitude != 0f) {
                switch (movementSpeed) {
                    case Entity.MovementSpeed.Slow:
                        newSpeed = entity.character.data.slowSpeed;
                        break;
                    case Entity.MovementSpeed.Normal:
                        newSpeed = entity.character.data.baseSpeed;
                        break;
                    case Entity.MovementSpeed.Fast:
                        newSpeed = entity.character.data.sprintSpeed;
                        break;
                }
            }

            // Acceleration is quicker than Deceleration 
            float speedDelta = newSpeed > moveSpeed ? 1f : 0.45f;
            moveSpeed = Mathf.MoveTowards(moveSpeed, newSpeed, speedDelta * entity.character.data.acceleration * GameUtility.timeDelta);
            
            // Evade movement restricts the Walking movement.
            moveSpeed *= Mathf.Max(1 - _evadeBehaviour.Speed, 0.05f);

            Animation();

            entity.DisplaceStep(entity.absoluteForward * moveSpeed);
            
        }

        public override void LateUpdate() {
            base.LateUpdate();

            moveDirection.SetVal(Vector3.zero);
            
        }

    }
}