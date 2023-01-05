using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SevenGame.Utility;

namespace SeleneGame.Core {
    
    public class Seat : ObjectFollower, IInteractable{

        protected const string SEAT_INTERACT_DESCRIPTION = "Sit Down";



        [Space(15)]

        private Entity _seatEntity;
        public Entity seatOccupant;

        [SerializeField] protected bool _affectCamera = false;

        [Space(15)]
        
        [SerializeField] private List<SittingPose> _sittingPoses = new List<SittingPose>();
        
        public SittingPose currentSittingPose { get; private set; }




        public Entity seatEntity {
            get {
                if (_seatEntity == null) {
                    _seatEntity = GetComponent<Entity>();
                }
                return _seatEntity;
            }
            set => _seatEntity = value;
        }

        public Vector3 sitPosition { 
            get {
                Vector3 seatOccupantUp = seatOccupant?.modelTransform.up ?? transform.up;
                float seatOccupantSize = seatOccupant?.character.size.y/2f ?? 1.67f;

                return transform.TransformPoint(currentSittingPose.position) + (seatOccupantUp * seatOccupantSize);
            }
        }

        public Quaternion sitRotation {
            get {
                return transform.rotation * currentSittingPose.rotation;
            }
        }

        public bool affectCamera => _affectCamera;

        protected virtual string seatedInteractionText => "";

        public bool isSeated => seatOccupant != null;

        public string InteractDescription => seatOccupant ? seatedInteractionText : SEAT_INTERACT_DESCRIPTION;

        public virtual bool IsInteractable => !isSeated;



        public void SetSittingPoses( IEnumerable<SittingPose> newPoses ) {
            _sittingPoses = new List<SittingPose>(newPoses);
        }
        

        public void Interact(Entity entity){
            if (entity == seatOccupant){
                SeatedInteract(entity);
                return;
            }
            StartSitting(entity);
        }

        protected virtual void SeatedInteract(Entity entity){;}

    
        private /* async */ void StartSitting(Entity entity){
            
            StopSitting();

            seatOccupant = entity;
            
            // Find the closest sitting pose
            foreach (SittingPose pose in _sittingPoses) {
                float distanceToOldPosition = Vector3.Distance(transform.TransformPoint(currentSittingPose.position), seatOccupant.transform.position);
                float distanceToCurrentPosition = Vector3.Distance(transform.TransformPoint(pose.position), seatOccupant.transform.position);
                if (distanceToCurrentPosition < distanceToOldPosition){
                    currentSittingPose = pose;
                }
            }

            // Move the entity to the seat
            // if (speed < 4){
            //     seatOccupant.ResetState();
            //     seatOccupant.walkingTo = true;
            //     await seatOccupant.WalkTo( sitPosition, (Entity.WalkSpeed)(speed+1) );
            //     await seatOccupant.TurnTo( transform.rotation * -sittingDir );
            //     seatOccupant.walkingTo = false;
            // }
            
            entity.SetState<Sitting>();
            ((Sitting)entity.state).seat = this;
        }

        public void StopSitting(){

            if (!isSeated) return;

            seatOccupant.ResetState();
        }




        private void Update(){
            FollowObject();
        }

        private void OnDrawGizmosSelected(){
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(sitPosition, 0.3f);
            Gizmos.DrawLine(transform.position, sitPosition);
        }



        [System.Serializable]
        public struct SittingPose {

            public Vector3 position;
            public Quaternion rotation;

            public AnimationClip startAnimation;
            public AnimationClip loopAnimation;
            public AnimationClip endAnimation;

            public SittingPose(Vector3 position, Quaternion rotation, AnimationClip startAnimation, AnimationClip loopAnimation, AnimationClip endAnimation){
                this.position = position;
                this.rotation = rotation;
                this.startAnimation = startAnimation;
                this.loopAnimation = loopAnimation;
                this.endAnimation = endAnimation;
            }
        }

    }
}