using System;
using System.Collections.Generic;
using System.IO;
using Birdkov.NaYeongMin.InventorySystem;
using UnityEngine;

// 마지막으로 죽은 자리와 그때 잃은 아이템. 한 번에 하나만 존재한다 (새로 죽으면 덮어쓴다).
// 사망 오브젝트 자체(생성/UI/회수)는 NaYeongMin 쪽(PlayerDeathSpawner / PlayerDeathContainer)이 담당하고,
// 여기서는 씬을 나갔다 와도 그 자리에 남아 있도록 파일(deathbox.json)에 기록만 한다.
// NaYeongMin 의 PlayerSaveData(인벤토리 세이브)는 건드리지 않는다.
[Serializable]
public class DeathBoxRecord
{
    public string sceneName;
    public Vector3 position;

    // 비어 있지 않은 슬롯만 담는다 (잔탄 remainingRounds, 마모 durabilityDamage 포함)
    public List<GridSlotData> slots = new List<GridSlotData>();

    public bool HasItems
    {
        get { return slots != null && slots.Count > 0; }
    }

    // 슬롯 목록에서 비어 있지 않은 것만 복사해 모은다
    public static List<GridSlotData> CopyNonEmpty(IEnumerable<GridSlotData> source)
    {
        List<GridSlotData> result = new List<GridSlotData>();

        if (source != null)
        {
            foreach (GridSlotData slot in source)
            {
                if (slot != null && !slot.IsEmpty())
                {
                    result.Add(new GridSlotData
                    {
                        itemId = slot.itemId,
                        amount = slot.amount,
                        remainingRounds = slot.remainingRounds,
                        durabilityDamage = slot.durabilityDamage
                    });
                }
            }
        }

        return result;
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
        DeathBoxRecord record = null;

        try
        {
            if (File.Exists(FilePath))
            {
                record = JsonUtility.FromJson<DeathBoxRecord>(File.ReadAllText(FilePath));
                if (record != null && !record.HasItems)
                {
                    record = null;
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("DeathBoxStorage: 읽기 실패 - " + exception.Message);
            record = null;
        }

        return record;
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
