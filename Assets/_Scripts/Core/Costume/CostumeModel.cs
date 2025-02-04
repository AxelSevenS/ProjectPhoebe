using System;
using System.Collections;
using System.Collections.Generic;
using SevenGame.Utility;
using UnityEngine;

namespace SeleneGame.Core {

    [Serializable]
    public abstract class CostumeModel<TCostume> : IDisposable where TCostume : Costume {

        private bool disposedValue;
        [SerializeField] [ReadOnly] protected TCostume _costume;
        [SerializeField] [ReadOnly] protected ModelProperties _costumeData;


        public abstract Transform mainTransform { get; }
        public TCostume costume => _costume;
        public ModelProperties costumeData => _costumeData;



        public CostumeModel(TCostume costume) {
            _costume = costume;
        }

        public abstract void Unload();

        public abstract void Display();
        public abstract void Hide();


        public virtual void Update(){;}
        public virtual void LateUpdate(){;}
        public virtual void FixedUpdate(){;}


        protected void Dispose(bool disposing) {

            if (!disposedValue) {
                if (disposing)
                    Unload();

                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
