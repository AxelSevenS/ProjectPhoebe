using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Animancer;

using SevenGame.Utility;

namespace SeleneGame.Core {

    public partial class GroundedBehaviour {

        private float movementSpeedWeight;
        [SerializeField] [HideInInspector] private LinearMixerState _movementStartMixer;
        [SerializeField] [HideInInspector] private LinearMixerState _movementMixer;
        [SerializeField] [HideInInspector] private LinearMixerState _movementStopMixer;
        [SerializeField] [HideInInspector] private AnimancerState _idleState;

        private AnimationClip _fallAnimation;
        private AnimationClip _landAnimation;


        private AnimancerLayer _layer => entity.animancer.Layers[0];

        public LinearMixerState movementStartMixer {
            get {
                if (_movementStartMixer == null) {
            
                    _movementStartMixer?.Destroy();
                    _movementStartMixer = new LinearMixerState();
                    _movementStartMixer.Initialize(
                        entity.character?.data.GetAnimation("MoveSlowStart"),
                        entity.character?.data.GetAnimation("MoveNormalStart"),
                        entity.character?.data.GetAnimation("MoveFastStart"),
                        1f,
                        2f,
                        3f
                    );
                    _layer.AddChild(_movementStartMixer);
                    _movementStartMixer.SetDebugName("Movement Start");
                    _movementStartMixer.Stop();
                }
                return _movementStartMixer;
            }
        }

        public LinearMixerState movementMixer {
            get {
                if (_movementMixer == null) {
                    
                    _movementMixer?.Destroy();
                    _movementMixer = new LinearMixerState();
                    _movementMixer.Initialize(
                        entity.character?.data.GetAnimation("MoveSlowCycle"),
                        entity.character?.data.GetAnimation("MoveNormalCycle"),
                        entity.character?.data.GetAnimation("MoveFastCycle"),
                        1f,
                        2f,
                        3f
                    );
                    _layer.AddChild(_movementMixer);
                    _movementMixer.SetDebugName("Movement");
                    _movementMixer.Stop();
                }
                return _movementMixer;
            }
        }

        public LinearMixerState movementStopMixer {
            get {
                if (_movementStopMixer == null) {
                    
                    _movementStopMixer?.Destroy();
                    _movementStopMixer = new LinearMixerState();
                    _movementStopMixer.Initialize(
                        entity.character?.data.GetAnimation("MoveSlowStop"),
                        entity.character?.data.GetAnimation("MoveNormalStop"),
                        entity.character?.data.GetAnimation("MoveFastStop"),
                        1f,
                        2f,
                        3f
                    );
                    _layer.AddChild(_movementStopMixer);
                    _movementStopMixer.SetDebugName("Movement Stop");
                    _movementStopMixer.Stop();
                }
                return _movementStopMixer;
            }
        }


        protected AnimancerState idleState {
            get {
                _idleState ??= _layer.GetOrCreateState(entity.character.data.GetAnimation("Idle"));
                return _idleState;
            }
        }

        private void AnimationInitialize() {

            _fallAnimation = entity.character?.data.GetAnimation("Fall");
            _landAnimation = entity.character?.data.GetAnimation("Land");

        }

        private void DefaultAnimationState() {

            if (entity.onGround) {
                if (movementSpeed == Entity.MovementSpeed.Idle) {
                    IdleAnimation();
                } else {
                    MovementStartAnimation(movementSpeed);
                }
            } else {
                _layer.Play(_fallAnimation, 0.3f);
            }
        }

        private void MovementStartAnimation(Entity.MovementSpeed speed) {

            // Debug.Log("start");

            movementStartMixer.Parameter = (float)speed;
            _layer.Play(_movementStartMixer, 0.15f);

            _movementStartMixer.Events.OnEnd = () => {
                // _movementStartMixer.Stop();
                MovementCycleAnimation();
            };
        }

        private void MovementCycleAnimation() {

            movementMixer.Parameter = (float)movementSpeed;
            _layer.Play(_movementMixer, 0.15f);

            // _movementMixer.Events.OnEnd = () => {
            //     // _movementMixer.Stop();
            //     MovementStopAnimation();
            // };
        }

        private void MovementStopAnimation() {

            if (movementSpeed == Entity.MovementSpeed.Idle) return;

            // Debug.Log("stop");

            movementStopMixer.Parameter = (float)movementSpeed;
            _layer.Play(_movementStopMixer, 0.15f);

            _movementStopMixer.Events.OnEnd = () => {
                // _movementStopMixer.Stop();
                IdleAnimation();
            };
        }

        private void IdleAnimation() {

            // Debug.Log("idle");

            _layer.Play(idleState, 0.15f);
        }




        private void Animation(){

            movementSpeedWeight = Mathf.MoveTowards(movementSpeedWeight, (float)movementSpeed, 5f * GameUtility.timeDelta);

            movementMixer.Parameter = movementSpeedWeight;

            if (!_layer.IsAnyStatePlaying()) {
                DefaultAnimationState();
            }

            if ( entity.onGround.started ) {
                AnimancerState landState = _layer.Play(_landAnimation, 0.1f);
                landState.Events.OnEnd = () => {
                    DefaultAnimationState();
                };
            }
        }

    }
}