using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evrenefeb.Toolkit.Persistence {
    public class TEST_PersistentCubeObject : MonoBehaviour
    {
        public int I;
        public float F;
        public bool B;
        public string[] SS;
        public List<int> IList = new();

        public List<TEST_PersistentCubeStatClass> CubeStatList = new();

        [Serializable]
        public class TEST_PersistentCubeStatClass
        {
            public string ID;
            public Vector3 VEC;
        }
    }
}
