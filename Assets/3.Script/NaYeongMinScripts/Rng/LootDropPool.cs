using System;
using System.Collections.Generic;
using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;

namespace Birdkov.NaYeongMin.Rng
{
    public sealed class LootDropPool : MonoBehaviour
    {
        [SerializeField] private LootDropObject prefab;
        [SerializeField, Min(1)] private int initialSize = InventorySettings.DropObjectPoolSize;

        private readonly Queue<LootDropObject> available = new Queue<LootDropObject>();
        private readonly HashSet<LootDropObject> rented = new HashSet<LootDropObject>();

        private void Awake()
        {
            Prewarm();
        }

        public void Prewarm()
        {
            if (prefab == null)
            {
                Debug.LogError("LootDropObject prefab is not assigned.", this);
                return;
            }

            while (available.Count + rented.Count < initialSize)
            {
                available.Enqueue(CreateInstance());
            }
        }

        public LootDropObject Rent(Vector3 position, Quaternion rotation, LootContainerData loot)
        {
            if (prefab == null)
            {
                throw new InvalidOperationException("LootDropObject prefab is not assigned.");
            }

            if (loot == null)
            {
                throw new ArgumentNullException(nameof(loot));
            }

            Prewarm();
            if (available.Count == 0)
            {
                return null;
            }

            LootDropObject instance = available.Dequeue();
            rented.Add(instance);
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetLoot(loot);
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Return(LootDropObject instance)
        {
            if (instance == null || !rented.Remove(instance))
            {
                return;
            }

            instance.Clear();
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(transform, false);
            available.Enqueue(instance);
        }

        private LootDropObject CreateInstance()
        {
            LootDropObject instance = Instantiate(prefab, transform);
            instance.gameObject.SetActive(false);
            instance.Emptied += Return;
            return instance;
        }
    }
}
