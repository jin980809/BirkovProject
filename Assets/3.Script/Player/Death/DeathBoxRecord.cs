using System;
using System.Collections.Generic;
using System.IO;
using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;

// 마지막으로 죽은 자리와 그때 잃은 아이템. 한 번에 하나만 존재한다 (새로 죽으면 덮어쓴다).
// NaYeongMin 의 PlayerSaveData 는 건드리지 않고 별도 파일(deathbox.json)에 저장한다.
[Serializable]
public class DeathBoxRecord
{
    public string sceneName;
    public Vector3 position;
    public float rotationY;

    // 비어 있지 않은 슬롯만 앞에서부터 채운 목록 (장비 슬롯 -> 가방 순서)
    public List<GridSlotData> slots = new List<GridSlotData>();

    // 상자에 담을 큰 크기 프리셋. 가방 25칸 + 장비 4칸 = 29칸이 들어가야 해서 기존 프리셋(최대 15칸)으로는 모자란다.
    // NaYeongMin 쪽 LootContainerSize 에 Box5x6 이 추가돼 있어야 한다. 아직 없으면 false 를 돌려주고,
    // 기록(deathbox.json)은 그대로 남으므로 프리셋이 추가된 뒤에 상자가 나타난다.
    public static bool TryGetLargePreset(out LootContainerSize size)
    {
        return Enum.TryParse("Box5x6", out size);
    }

    // 프리셋 크기에 맞는 LootContainerData 로 만든다. 프리셋이 없으면 false.
    public bool TryBuildLoot(out LootContainerData loot)
    {
        loot = null;

        if (!TryGetLargePreset(out LootContainerSize size))
        {
            return false;
        }

        loot = new LootContainerData(size);

        int count = Mathf.Min(loot.loot.slots.Count, slots.Count);
        for (int i = 0; i < count; i++)
        {
            CopySlot(slots[i], loot.loot.slots[i]);
        }

        if (slots.Count > loot.loot.slots.Count)
        {
            Debug.LogWarning("DeathBoxRecord: 잃은 아이템(" + slots.Count + "칸)이 상자 크기(" + loot.loot.slots.Count + "칸)보다 많습니다.");
        }

        return true;
    }

    public static void CopySlot(GridSlotData from, GridSlotData to)
    {
        to.itemId = from.itemId;
        to.amount = from.amount;
        to.remainingRounds = from.remainingRounds;
        to.durabilityDamage = from.durabilityDamage;
    }

    // 슬롯 목록에서 비어 있지 않은 것만 복사해 모은다
    public static void AppendNonEmpty(List<GridSlotData> source, List<GridSlotData> target)
    {
        for (int i = 0; i < source.Count; i++)
        {
            if (source[i].IsEmpty())
            {
                continue;
            }

            GridSlotData copy = new GridSlotData();
            CopySlot(source[i], copy);
            target.Add(copy);
        }
    }
}

// deathbox.json 읽기/쓰기. 인벤토리 저장과 같은 폴더를 쓴다.
public class DeathBoxStorage
{
    private const string FileName = "deathbox.json";

    private string FilePath
    {
        get { return Path.Combine(Application.persistentDataPath, "NaYeongMinTestBench", FileName); }
    }

    // 기록이 없거나 비어 있으면 null
    public DeathBoxRecord Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            DeathBoxRecord record = JsonUtility.FromJson<DeathBoxRecord>(File.ReadAllText(FilePath));
            if (record == null || record.slots == null || record.slots.Count == 0)
            {
                return null;
            }

            return record;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("DeathBoxStorage: 읽기 실패 - " + exception.Message);
            return null;
        }
    }

    public void Save(DeathBoxRecord record)
    {
        try
        {
            string directory = Path.GetDirectoryName(FilePath);
            Directory.CreateDirectory(directory);

            // 쓰는 도중 꺼져도 기존 파일이 깨지지 않게 임시 파일에 먼저 쓰고 덮어쓴다
            string temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(record));
            File.Copy(temp, FilePath, true);
            File.Delete(temp);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("DeathBoxStorage: 저장 실패 - " + exception.Message);
        }
    }

    public void Delete()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("DeathBoxStorage: 삭제 실패 - " + exception.Message);
        }
    }
}
