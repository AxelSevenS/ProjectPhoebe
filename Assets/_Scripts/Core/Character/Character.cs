using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;

using SevenGame.Utility;

namespace SeleneGame.Core {

    public class Character : Costumable<CharacterData, CharacterCostume, CharacterModel> {

        [SerializeField] [ReadOnly] private Entity _entity;


        public Entity entity => _entity;

        

        public Character(Entity entity, CharacterData data, CharacterCostume costume = null) : base(data) {
            _entity = entity;
            displayed = true;
            SetCostume(costume);
        }

        public override void SetCostume(CharacterCostume costume) {
            _model?.Dispose();

            costume ??= data.baseCostume ?? AddressablesUtils.GetDefaultAsset<CharacterCostume>();
            _model = costume?.LoadModel(entity);
            
            if (displayed)
                _model?.Display();
            else
                _model?.Hide();
        }


        public override void Update() {
            base.Update();

            data?.CharacterUpdate(this);

            model?.Update();
        }
        public override void LateUpdate() {
            base.LateUpdate();

            data?.CharacterLateUpdate(this);

            model?.LateUpdate();
        }
        public override void FixedUpdate() {
            base.FixedUpdate();

            data?.CharacterFixedUpdate(this);

            model?.FixedUpdate();
        }

    }

}