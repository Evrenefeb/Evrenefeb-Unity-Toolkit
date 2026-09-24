using System;
using UnityEngine;

namespace Evrenefeb.Toolkit.Persistence {
    public class TEST_PersistentPlayerObject : MonoBehaviour
    {

        public TEST_PersistentPlayerFloatStatClass HP = new TEST_PersistentPlayerFloatStatClass { StatName = "Health", StatValue = 100 };
        public TEST_PersistentPlayerFloatStatClass MP = new TEST_PersistentPlayerFloatStatClass { StatName = "Mana", StatValue = 200.5f };

        public bool A = true;
        //public Transform TRA;
        public Vector3 VECP;

        [Serializable]
        public class TEST_PersistentPlayerFloatStatClass
        {
            public string StatName;
            public float StatValue;
        }
    }
}
