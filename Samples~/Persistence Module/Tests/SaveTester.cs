using System;
using System.Collections.Generic;
using UnityEngine;
using static Evrenefeb.Toolkit.Persistence.TEST_PersistentPlayerObject;

namespace Evrenefeb.Toolkit.Persistence
{
    public class SaveTester : MonoBehaviour
    {
        [SerializeField] private TEST_PersistentPlayerObject _player;

        [Serializable]
        public class PlayerDataObject
        {
            public TEST_PersistentPlayerFloatStatClass HP;
            public TEST_PersistentPlayerFloatStatClass MP;

            public bool A;
            public Vector3 VECP;

            public SerializableTransform TRA;
        }
        [ContextMenu("Save World")]
        public void SaveWorld()
        {
            #region Player Save
            var playerDataObj = new PlayerDataObject();

            playerDataObj.HP = new TEST_PersistentPlayerFloatStatClass
            {
                StatName = _player.HP.StatName,
                StatValue = _player.HP.StatValue
            };

            playerDataObj.MP = new TEST_PersistentPlayerFloatStatClass
            {
                StatName = _player.MP.StatName,
                StatValue = _player.MP.StatValue
            };

            playerDataObj.A = _player.A;            
            playerDataObj.VECP = _player.VECP;

            playerDataObj.TRA = SerializableTransform.FromTransform(_player.transform);

            #endregion

            PersistenceManager.Save<PlayerDataObject>("player", playerDataObj, SaveFormat.JSON, jsonPrettyPrint:true);

            
        }

        [ContextMenu("Load World")]
        public void LoadWorld()
        {
            var playerDataObj = PersistenceManager.Load<PlayerDataObject>("player", SaveFormat.JSON);

            #region Player Load

            _player.HP = playerDataObj.HP;
            _player.MP = playerDataObj.MP;
            _player.A = playerDataObj.A;
            _player.VECP = playerDataObj.VECP;

            playerDataObj.TRA.ApplyTo(_player.transform);

            #endregion


        }
    }
}
