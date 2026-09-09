using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using Birdkov.NaYeongMin.Rng;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Birdkov.NaYeongMin.Tests
{
    public class LootDropPoolTests
    {
        [Test]
        public void Prefab_CanRentThirtyDropObjects()
        {
            LootDropPool prefab = AssetDatabase.LoadAssetAtPath<LootDropPool>(
                "Assets/2.Model/Prefabs/NaYeongMin/LootDropPool.prefab");
            LootDropPool pool = Object.Instantiate(prefab);
            HashSet<LootDropObject> rented = new HashSet<LootDropObject>();

            for (int index = 0; index < InventorySettings.DropObjectPoolSize; index++)
            {
                rented.Add(pool.Rent(Vector3.zero, Quaternion.identity, new LootContainerData()));
            }

            Assert.AreEqual(InventorySettings.DropObjectPoolSize, rented.Count);
            Assert.IsNull(pool.Rent(Vector3.zero, Quaternion.identity, new LootContainerData()));
            foreach (LootDropObject instance in rented)
            {
                pool.Return(instance);
                Assert.AreSame(instance, pool.Rent(Vector3.zero, Quaternion.identity, new LootContainerData()));
                break;
            }
            Object.DestroyImmediate(pool.gameObject);
        }
    }
}
