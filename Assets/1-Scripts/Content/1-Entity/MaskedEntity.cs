using System;
using System.Collections.Generic;
using UnityEngine;

using SeleneGame.Core;
using SeleneGame.States;

using SevenGame.Utility;

namespace SeleneGame.Content {

    public class MaskedEntity : ArmedEntity {

        [Header("Mask")]
        
        [SerializeField] [ReadOnly] protected EidolonMask mask;

        public bool focusing;
        public float shiftCooldown;

        private List<GrabbedObject> grabbedObjects = new List<GrabbedObject>();
        private readonly Vector3[] grabbedObjectPositions = new Vector3[4]{
            new Vector3(3.5f, 1.5f, 3f), 
            new Vector3(-3.5f, 1.5f, 3f), 
            new Vector3(2.5f, 2.5f, 3f), 
            new Vector3(-2.5f, 2.5f, 3f)
        };

        private SpeedlinesEffect speedlines;
        public float shiftEnergy = 0f;


        
        public override float weight => character.weight * weapons.current.weightModifier;
        public override float jumpMultiplier => 2 - (weight / 15f);

        protected virtual bool isMasked {
            get {
                if (state is MaskedState || state is SwimmingState)
                    return true;

                else if (state is HumanoidGroundedState HumanoidGroundedState)
                    return Vector3.Dot ( gravityDown, Vector3.down ) < 0.95f /* || weapons.current.shifting */;

                return false;

            }
        }

        public override State defaultState => new HumanoidGroundedState();



        public void SetMask(EidolonMask mask, EidolonMaskCostume costume = null) {
            try {
                mask.Initialize(this, costume);
            } catch (Exception e) {
                Debug.LogError($"Error while Setting Mask {mask.name} : {e.Message}");
            }

            this.mask = mask;
        }

        protected override void LoadModel() {
            base.LoadModel();
        }

        protected override void UnloadModel(){
            base.UnloadModel();
            
            GameUtility.SafeDestroy(speedlines);
        }   
        
        public override void Grab(Grabbable grabbable){
            if (grabbedObjects.Count >= 4) return;
            
            grabbedObjects.Add( 
                new GrabbedObject( 
                    UnityEngine.Random.insideUnitSphere.normalized * 0.3f, 
                    grabbedObjectPositions[grabbedObjects.Count], 
                    grabbable) 
                );
            grabbable.grabbed = true;
        }
        public override void Throw(Grabbable grabbable){

            foreach (GrabbedObject grabbed in grabbedObjects) {
                if (grabbed.grabbable == grabbable) {
                    grabbedObjects.Remove(grabbed);
                    grabbable.grabbed = false;
                    break;
                }
            }

            /// TODO: Get the direction to Target
            Vector3 targetDirection = absoluteForward;

            grabbable.rb.AddForce(targetDirection * 30f, ForceMode.Impulse);

            var impulseParticle = Instantiate(Global.LoadParticle("ShiftImpulseParticles"), transform.position + targetDirection*2f, Quaternion.LookRotation(targetDirection, rotation * Vector3.up));
            Destroy(impulseParticle, 1.2f);
        }


        public void Shift(){
            shiftCooldown = 0.3f;
            if (onGround) rigidbody.velocity += -gravityDown*3f;
            
            SetState( new MaskedState() );
        }

        public void StopShifting(Vector3 newDown){
            gravityDown = newDown;
            SetState( defaultState );
        }
        
        protected void ToggleShift(){
            if (shiftCooldown > 0f) return;

            if (state is MaskedState) 
                StopShifting(Vector3.down);
            else if (state is HumanoidGroundedState) {
                Shift();
            }
        }

        protected sealed override void ResetWeapons() {
            _weapons?.Dispose();
            _weapons = new MaskedWeaponInventory(this);
        }

        protected void ResetMask() {
            mask?.UnloadModel();
            SetMask(EidolonMask.GetInstance("Erebus"));
        }




        protected override void Awake() {
            base.Awake();
            
            
            // if (speedlines == null) {
            //     GameObject speedlinesObject = GameObject.Instantiate(Resources.Load("Prefabs/Effects/Speedlines"), EffectManager.current.transform) as GameObject;
            //     speedlines = speedlinesObject.GetComponent<SpeedlinesEffect>();
            //     speedlines.SetFollowedObject(gameObject);
            // }
        }

        protected override void EntityReset(){
            base.EntityReset();
            ResetMask();
        }

        protected override void Update(){
            base.Update();

            shiftCooldown = Mathf.MoveTowards( shiftCooldown, 0f, GameUtility.timeDelta );
            
            if (entityController.shiftInput.tapped){
                
                ToggleShift();
            }

            mask.SetState( isMasked );
        }

        protected override void LateUpdate() {
            base.LateUpdate();
            
            mask.MaskUpdate();
        }

        protected override void FixedUpdate() {
            base.FixedUpdate();
            
            int i = 0;
            foreach (GrabbedObject grabbed in grabbedObjects) {

                grabbed.grabbable.rb.AddTorque(grabbed.randomSpin.x, grabbed.randomSpin.y, grabbed.randomSpin.z, ForceMode.VelocityChange);

                grabbed.grabbable.transform.position = Vector3.Lerp(grabbed.grabbable.transform.position, transform.position + rotation * grabbedObjectPositions[i], 10f* GameUtility.timeDelta);

                i++;
            }

            if (shiftCooldown > 0f){
                shiftCooldown -= GameUtility.timeDelta;
            }

            mask.MaskFixedUpdate();

        }


        private struct GrabbedObject {
            public Vector3 randomSpin;
            public Vector3 position;
            public Grabbable grabbable;

            public GrabbedObject(Vector3 randomSpin, Vector3 position, Grabbable grabbable) {
                this.randomSpin = randomSpin;
                this.position = position;
                this.grabbable = grabbable;
            }
        }
        

    }
}




