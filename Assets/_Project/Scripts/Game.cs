using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ReCraft
{
    public enum Tool { Hand = 0, Driver = 1, Sander = 2, Cloth = 3, Spray = 4 }

    [Serializable]
    public class CatalogEntry
    {
        public string id;
        public int best;
        public int count;
    }

    [Serializable]
    public class SaveData
    {
        public int gold = 40;
        public int stars;
        public int[] toolLv = { 1, 1, 1, 1, 1 };
        public bool autoOrder;
        public bool magTray;
        public bool ended;
        public int tutorialStep;
        public int jobsDone;
        public List<CatalogEntry> catalog = new();
    }

    public class Offer
    {
        public ItemDef item;
        public float severity;   // 0..1 손상 정도
        public bool auction;     // true = 경매 매입(선불), false = 의뢰 수주
        public int cost;
        public int reward;
        public int seed;
    }

    /// <summary>전역 진행 상태 · 저장 · 밸런스 테이블.</summary>
    public static class G
    {
        public static SaveData data = new();
        public static bool noSave;
        public static event Action Changed;

        public const int Tier2Stars = 8;
        public const int Tier3Stars = 18;
        public const int FinalStars = 30;

        public static readonly string[] ToolNames = { "손", "드라이버", "녹 제거", "세척", "도색" };
        static readonly string[][] LevelNames =
        {
            new[] { "맨손", "맨손", "맨손" },
            new[] { "수동 드라이버", "전동 드릴", "마그네틱 임팩트 건" },
            new[] { "수동 사포", "회전 와이어 브러시", "샌드블라스터" },
            new[] { "마른 헝겊", "고압 에어건", "초음파 세척조" },
            new[] { "캔 스프레이", "에어브러시", "자동 도색 부스" },
        };
        static readonly int[][] UpgradeCost =
        {
            new[] { 0, 0 },
            new[] { 120, 520 },
            new[] { 150, 600 },
            new[] { 100, 450 },
            new[] { 140, 560 },
        };
        public const int AutoOrderCost = 220;
        public const int MagTrayCost = 260;

        public static int Lv(Tool t) => data.toolLv[(int)t];
        public static string LevelName(Tool t, int lv) => LevelNames[(int)t][Mathf.Clamp(lv, 1, 3) - 1];
        public static string ToolName(Tool t) => LevelName(t, Lv(t));
        public static int NextCost(Tool t) => Lv(t) >= 3 ? -1 : UpgradeCost[(int)t][Lv(t) - 1];

        // ── 도구 성능 ──
        public static float ScrewTime => (Lv(Tool.Driver) == 1 ? 3f : Lv(Tool.Driver) == 2 ? 0.5f : 0.35f) * (data.magTray ? 0.6f : 1f);
        public static float ScrewRadius => Lv(Tool.Driver) >= 3 ? 0.16f : 0f;

        public static float BrushRadius(Tool t)
        {
            int lv = Lv(t);
            return t switch
            {
                Tool.Sander => lv == 1 ? 0.014f : lv == 2 ? 0.032f : 0.075f,
                Tool.Cloth => lv == 1 ? 0.018f : lv == 2 ? 0.04f : 0.07f,
                Tool.Spray => lv == 1 ? 0.02f : lv == 2 ? 0.04f : 0.07f,
                _ => 0f,
            };
        }

        /// <summary>초당 제거/도포 강도 (1 = 1초에 완전 제거).</summary>
        public static float BrushPower(Tool t)
        {
            int lv = Lv(t);
            return t switch
            {
                Tool.Sander => lv == 1 ? 2.2f : lv == 2 ? 4.5f : 12f,
                Tool.Cloth => lv == 1 ? 3f : lv == 2 ? 6f : 12f,
                Tool.Spray => lv == 1 ? 2.5f : lv == 2 ? 5f : 10f,
                _ => 0f,
            };
        }

        public static bool Spend(int amount)
        {
            if (data.gold < amount) return false;
            data.gold -= amount;
            Notify();
            return true;
        }

        public static void Earn(int gold, int stars)
        {
            data.gold += gold;
            data.stars += stars;
            Notify();
        }

        public static bool UpgradeTool(Tool t)
        {
            int cost = NextCost(t);
            if (cost < 0 || !Spend(cost)) return false;
            data.toolLv[(int)t]++;
            Save();
            Notify();
            return true;
        }

        public static void Record(string itemId, int score)
        {
            var e = data.catalog.Find(c => c.id == itemId);
            if (e == null) { e = new CatalogEntry { id = itemId }; data.catalog.Add(e); }
            e.count++;
            e.best = Mathf.Max(e.best, score);
            data.jobsDone++;
        }

        public static CatalogEntry Entry(string itemId) => data.catalog.Find(c => c.id == itemId);

        public static void Notify() => Changed?.Invoke();

        // ── 의뢰 생성 ──
        public static List<Offer> MakeOffers(System.Random rng)
        {
            var pool = new List<ItemDef>();
            foreach (var it in Items.All)
                if (it.tier == 1 || (it.tier == 2 && data.stars >= Tier2Stars) || (it.tier == 3 && data.stars >= Tier3Stars)) pool.Add(it);
            var offers = new List<Offer>();
            var used = new HashSet<string>();
            for (int i = 0; i < 3; i++)
            {
                ItemDef it;
                int guard = 0;
                do { it = pool[rng.Next(pool.Count)]; } while (used.Contains(it.id) && ++guard < 20);
                used.Add(it.id);
                float sev = Mathf.Clamp01(0.25f + (float)rng.NextDouble() * 0.5f + data.stars * 0.008f);
                bool auction = i == 2;
                int value = Mathf.RoundToInt(it.value * (0.8f + sev * 0.6f) / 5f) * 5;
                offers.Add(new Offer
                {
                    item = it, severity = sev, auction = auction, seed = rng.Next(),
                    cost = auction ? Mathf.RoundToInt(value * 0.3f / 5f) * 5 : 0,
                    reward = auction ? Mathf.RoundToInt(value * 1.6f / 5f) * 5 : value,
                });
            }
            if (data.stars >= FinalStars || data.ended)
            {
                // 전설의 의뢰: 아직 복원하지 못한 유물부터 하나씩 (모두 끝났으면 아무거나)
                var relics = Relics;
                ItemDef next = relics.Find(r => !RelicDone(r.id)) ?? relics[rng.Next(relics.Count)];
                offers.Add(new Offer { item = next, severity = 0.9f, auction = false, cost = 0, reward = next.value, seed = rng.Next() });
            }
            return offers;
        }

        public const int RelicScore = 80;
        public static List<ItemDef> Relics => Items.All.FindAll(i => i.tier == 4);
        public static bool RelicDone(string id) { var e = Entry(id); return e != null && e.best >= RelicScore; }
        public static int RelicsDone => Relics.FindAll(r => RelicDone(r.id)).Count;

        // ── 저장 ──
        static string SavePath => Path.Combine(Application.persistentDataPath, "recraft_save.json");

        public static void Load()
        {
            data = new SaveData();
            if (noSave) return;
            try
            {
                if (File.Exists(SavePath))
                {
                    var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                    if (d != null && d.toolLv != null && d.toolLv.Length == 5) data = d;
                    // v0.1 세이브(튜토리얼 필드 없음)로 이미 납품해 본 플레이어는 튜토리얼 생략
                    if (data.jobsDone > 0 && data.tutorialStep == 0) data.tutorialStep = Tutorial.Done;
                }
            }
            catch (Exception e) { Debug.LogWarning("[Save] load failed: " + e.Message); }
        }

        public static void Save()
        {
            if (noSave) return;
            try { File.WriteAllText(SavePath, JsonUtility.ToJson(data)); }
            catch (Exception e) { Debug.LogWarning("[Save] save failed: " + e.Message); }
        }

        public static void ResetSave()
        {
            data = new SaveData();
            Save();
            Notify();
        }
    }
}
