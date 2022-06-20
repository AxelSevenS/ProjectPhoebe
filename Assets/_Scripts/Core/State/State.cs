using System;
using UnityEngine;
using UnityEditor;

namespace SeleneGame.Core {
    
    [System.Serializable]
    public abstract class State { 

        public virtual int id => 0;
        [ReadOnly] public new string name;
        [HideInInspector] public Entity entity;

        public Vector3 cameraPosition => GetCameraPosition();
        public Vector3 jumpDirection => GetJumpDirection();

        protected virtual Vector3 GetCameraPosition() => new Vector3(1f, 1f, -3.5f);
        protected virtual Vector3 GetJumpDirection() => -entity.gravityDown;




        public virtual void OnEnter(Entity entity){
            this.entity = entity;
            name = GetType().Name.Replace("State","");
        }
        public virtual void OnExit(){;}
        public virtual void StateUpdate(){;}
        public virtual void StateLateUpdate(){;}
        public virtual void StateFixedUpdate(){;}

        public virtual void StateAnimation(){;}


        public static Type GetStateTypeByName(string stateName){
            return Type.GetType($"SeleneGame.States.{stateName}State");
        }
        

        public virtual float UpdateMoveSpeed(){
            return entity.moveSpeed;
        }

        public virtual void HandleInput(){;} 

    }
}