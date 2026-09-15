using System;
using System.Collections.Generic;
using UnityEngine;

// itemId -> 아이템 스프라이트. 기존에 연결한 아이템 그림을 그대로 쓴다. HONETi 는 틀과 버튼에만 쓴다.
namespace Birdkov.NaYeongMin.Ui
{
    [CreateAssetMenu(fileName = "ItemIconTable", menuName = "Birdkov/NaYeongMin/Item Icon Table")]
    public sealed class ItemIconTable : ScriptableObject
    {
        [Serializable]
        public struct IconBinding
        {
            public int itemId;
            public Sprite sprite;
        }

        [SerializeField] private IconBinding[] bindings = Array.Empty<IconBinding>();

        private Dictionary<int, Sprite> lookup;

        public IReadOnlyList<IconBinding> Bindings => bindings;

        public Sprite GetIcon(int itemId)
        {
            if (lookup == null)
            {
                Rebuild();
            }

            Sprite sprite;
            return lookup.TryGetValue(itemId, out sprite) ? sprite : null;
        }

        public void Rebuild()
        {
            lookup = new Dictionary<int, Sprite>();
            if (bindings == null)
            {
                return;
            }

            foreach (IconBinding binding in bindings)
            {
                if (binding.sprite != null)
                {
                    lookup[binding.itemId] = binding.sprite;
                }
            }
        }

        private void OnValidate()
        {
            lookup = null;
        }
    }
}
