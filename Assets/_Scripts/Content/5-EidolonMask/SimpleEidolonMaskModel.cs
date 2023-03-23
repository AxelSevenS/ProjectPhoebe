using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SeleneGame.Core;

using EasySplines;
using SevenGame.Utility;

namespace SeleneGame.Content {

    public sealed class SimpleEidolonMaskModel : EidolonMaskModel {

        [SerializeField] [ReadOnly] private GameObject _model;
        [SerializeField] [ReadOnly] private Animator _animator;


        public override Transform mainTransform => _model.transform;

        public float maskPosT { get; private set;} = 1f;
        public bool onRight { get; private set;} = true;
        public Vector3 hoveringPosition { get; private set;}
        public Vector3 relativePos { get; private set;}


        private Transform headTransform => mask.maskedEntity["head"]?.transform ?? null;

        public bool faceState {
            get {
                if (mask.maskedEntity.behaviour is MaskedBehaviour || mask.maskedEntity.behaviour is SwimmingBehaviour)
                    return true;

                else if (mask.maskedEntity.behaviour is GroundedBehaviour)
                    return Vector3.Dot( mask.maskedEntity.gravityDown, Vector3.down ) < 0.95f;

                return false;
            }
        }
        public bool onFace => faceState || (positionBlocked(leftPosition) && positionBlocked(rightPosition));
        public Vector3 rightPosition => mask.maskedEntity.modelTransform.rotation * new Vector3(1.2f, 1.3f, -0.8f);
        public Vector3 leftPosition => mask.maskedEntity.modelTransform.rotation * new Vector3(-1.2f, 1.3f, -0.8f);



        public SimpleEidolonMaskModel(MaskedEntity entity, EidolonMask mask, SimpleEidolonMaskCostume costume) : base(entity, mask, costume) {
            if (mask?.maskedEntity != null && costume?.model != null) {

                _model = GameObject.Instantiate(costume.model, mask.maskedEntity.transform);

                _costumeData = _model.GetComponent<ModelProperties>();

                _animator = _model.GetComponent<Animator>();
                _animator ??= _model.AddComponent<Animator>();
            }
        }


        private bool positionBlocked(Vector3 position) {
            return Physics.SphereCast(mask.maskedEntity.modelTransform.position, 0.35f, position, out _, position.magnitude, Global.GroundMask);
        }


        public override void Unload() {
            _model = GameUtility.SafeDestroy(_model);
        }

        public override void Display() {
            _model?.SetActive(true);
        }

        public override void Hide() {
            _model?.SetActive(false);
        }


        public override void LateUpdate() {
            base.LateUpdate();


            if (_model == null || headTransform == null)
                return;


            relativePos = Vector3.Lerp(relativePos, onRight ? rightPosition : leftPosition, 3f * GameUtility.timeDelta);

            if (!onFace && (positionBlocked(relativePos)))
                onRight = !onRight;


            hoveringPosition = Vector3.Lerp(hoveringPosition, mask.maskedEntity.modelTransform.position + relativePos, 15f * Time.deltaTime);
            
            maskPosT = Mathf.MoveTowards(maskPosT, (onFace ? 1f : 0f), 4f * GameUtility.timeDelta);


            BezierQuadratic currentCurve = new BezierQuadratic(
                hoveringPosition,
                headTransform.position,
                headTransform.position + headTransform.forward
            );

            _model.transform.position = currentCurve.GetPoint(maskPosT).position;
            _model.transform.rotation = Quaternion.Slerp(mask.maskedEntity.modelTransform.rotation, headTransform.rotation, maskPosT);
        }

        // public override void LateUpdate() {
        //     base.LateUpdate();


        //     // if (_animator != null) {
        //     //     _animator?.SetBool("OnFace", onFace);
        //     //     _animator?.SetFloat("OnRight", onRight ? 1f : 0f);
        //     // }

        // }

        public override void FixedUpdate() {
            base.FixedUpdate();
        }
    } 
}