using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Animancer;

using SevenGame.Utility;

namespace SeleneGame.Core {

    [System.Serializable]
    public class JumpBehaviour : BehaviourStrategy {

        public ScaledTimeInterval cooldownTimer;

        
        public virtual bool canJump => cooldownTimer.isDone;


        private AnimationClip _jumpAnimation;

        public AnimationClip jumpAnimation {
            get {
                if ( _jumpAnimation == null )
                    _jumpAnimation = entity.character?.data.GetAnimation("Jump");
                return _jumpAnimation;
            }
        }


        public JumpBehaviour(Entity entity) : base(entity) {;}


        protected internal override void HandleInput(PlayerEntityController contoller) {;}

        protected internal virtual void Jump(Vector3 direction) {

            AnimancerState jumpState = entity.animancer.Layers[0].Play(jumpAnimation, 0.3f);
            jumpState.Events.OnEnd = () => {
                jumpState.Stop();
            };

            float jumpHeight = entity.character.data.jumpHeight * entity.jumpMultiplier;

            Debug.Log($"Entity {entity.name} jumped with force : {jumpHeight}");


            entity.inertia = Vector3.ProjectOnPlane(entity.inertia, direction) + (jumpHeight * direction);
            cooldownTimer.SetDuration(0.4f);

        }
    }

}