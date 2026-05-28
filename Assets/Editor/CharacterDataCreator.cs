#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using AngelArena.Data;

namespace AngelArena.Editor
{
    /// <summary>
    /// Creates all 7 CharacterData ScriptableObjects automatically.
    /// Menu: AngelArena → Setup → 4. Create All Character Data
    /// </summary>
    public static class CharacterDataCreator
    {
        private struct CharDef
        {
            public string id, name, desc, skillId;
            public float hp, spd, atk, def, lifesteal;
            public Color color;
            public int pvHp, pvSpd, pvAtk, pvDef;
            public CharacterWeaponType weapon;
        }

        private static readonly CharDef[] Chars = new[]
        {
            new CharDef { id="assassin",   name="Assassin",   weapon=CharacterWeaponType.Daggers,     hp=180, spd=210, atk=1.45f, def=0.65f, lifesteal=0.03f, color=new Color(0.6f,0.1f,0.8f), pvHp=35,  pvSpd=90,  pvAtk=85,  pvDef=30,  skillId="shadow_blades",  desc="Kẻ ẩn trong bóng tối. Tốc độ cao, sát thương chí mạng." },
            new CharDef { id="fighter",    name="Fighter",    weapon=CharacterWeaponType.Sword,       hp=320, spd=155, atk=1.20f, def=1.45f, lifesteal=0.02f, color=new Color(0.9f,0.4f,0.1f), pvHp=85,  pvSpd=45,  pvAtk=65,  pvDef=90,  skillId="war_slash",      desc="Chiến binh dày dặn. Máu cao, phòng thủ vượt trội." },
            new CharDef { id="mage",       name="Mage",       weapon=CharacterWeaponType.Wand,        hp=175, spd=165, atk=1.35f, def=0.75f, lifesteal=0.0f,  color=new Color(0.2f,0.5f,1.0f), pvHp=30,  pvSpd=55,  pvAtk=95,  pvDef=35,  skillId="fireball",       desc="Pháp sư huyền bí. AoE diện rộng, elemental damage." },
            new CharDef { id="ranger",     name="Ranger",     weapon=CharacterWeaponType.Bow,         hp=195, spd=185, atk=1.25f, def=0.85f, lifesteal=0.01f, color=new Color(0.2f,0.8f,0.3f), pvHp=40,  pvSpd=75,  pvAtk=75,  pvDef=40,  skillId="arrow_rain",     desc="Xạ thủ tầm xa. Tấn công từ khoảng cách an toàn." },
            new CharDef { id="paladin",    name="Paladin",    weapon=CharacterWeaponType.Hammer,      hp=280, spd=145, atk=1.00f, def=1.35f, lifesteal=0.04f, color=new Color(1.0f,0.9f,0.2f), pvHp=75,  pvSpd=35,  pvAtk=50,  pvDef=85,  skillId="holy_light",     desc="Hiệp sĩ thánh. Hồi phục đồng đội, buff khả năng sống." },
            new CharDef { id="necromancer",name="Necromancer",weapon=CharacterWeaponType.SkullStaff, hp=200, spd=158, atk=1.10f, def=0.90f, lifesteal=0.05f, color=new Color(0.5f,0.9f,0.5f), pvHp=45,  pvSpd=50,  pvAtk=60,  pvDef=45,  skillId="raise_skeleton", desc="Thầy phù thủy bóng tối. Triệu hồi quân đoàn xương." },
            new CharDef { id="druid",      name="Druid",      weapon=CharacterWeaponType.NatureStaff, hp=240, spd=160, atk=0.95f, def=1.10f, lifesteal=0.02f, color=new Color(0.4f,0.8f,0.2f), pvHp=60,  pvSpd=52,  pvAtk=45,  pvDef=65,  skillId="briar_patch",    desc="Người giữ rừng. Kiểm soát đám đông, độc tố diện rộng." },
        };

        [MenuItem("AngelArena/Setup/4. Create All Character Data")]
        public static void CreateAllCharacters()
        {
            string folder = "Assets/ScriptableObjects/Characters";
            System.IO.Directory.CreateDirectory(folder);

            int created = 0;
            foreach (var c in Chars)
            {
                string path = $"{folder}/{c.id}.asset";
                if (AssetDatabase.LoadAssetAtPath<CharacterData>(path) != null)
                {
                    Debug.Log($"[AngelArena] Already exists: {path}");
                    continue;
                }
                var data            = ScriptableObject.CreateInstance<CharacterData>();
                data.characterId    = c.id;
                data.characterName  = c.name;
                data.description    = c.desc;
                data.startSkillId   = c.skillId;
                data.maxHp          = c.hp;
                data.moveSpeed      = c.spd;
                data.atkMult        = c.atk;
                data.defMult        = c.def;
                data.baseLifesteal  = c.lifesteal;
                data.characterColor = c.color;
                data.weaponType     = c.weapon;
                data.statPreview    = new StatPreview { hp = c.pvHp, spd = c.pvSpd, atk = c.pvAtk, def = c.pvDef };

                AssetDatabase.CreateAsset(data, path);
                created++;
                Debug.Log($"[AngelArena] Created: {path}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = created > 0
                ? $"Created {created} CharacterData assets in:\n{folder}"
                : "All CharacterData assets already exist!";
            EditorUtility.DisplayDialog("Character Data", msg + "\n\nAssign them in MainMenuController or GameManager Inspector.", "OK");
        }

        [MenuItem("AngelArena/Setup/5. Create Starter Skill Data")]
        public static void CreateStarterSkills()
        {
            string folder = "Assets/ScriptableObjects/Skills";
            System.IO.Directory.CreateDirectory(folder);

            var skills = new[]
            {
                // id, name, desc, branch, cd, dmgBase, aoe, legendary
                ("shadow_blades",  "Shadow Blades",  "Phóng lưỡi dao bóng tối theo mọi hướng",   "assassin",    1.2f, 35f,  80f,  false),
                ("war_slash",      "War Slash",      "Chém AoE gần mạnh mẽ quanh nhân vật",      "fighter",     0.8f, 55f,  90f,  false),
                ("fireball",       "Fireball",       "Bắn cầu lửa nổ AoE vào nhóm quái",         "mage",        1.5f, 65f,  110f, false),
                ("arrow_rain",     "Arrow Rain",     "Mưa tên xuyên qua hàng quái",               "ranger",      1.0f, 40f,  0f,   false),
                ("holy_light",     "Holy Light",     "Thánh quang hồi HP và thiêu đốt quái",      "paladin",     2.0f, 45f,  100f, false),
                ("raise_skeleton", "Raise Skeleton", "Triệu hồi bộ xương chiến đấu cho bạn",      "necromancer", 4.0f, 30f,  0f,   false),
                ("briar_patch",    "Briar Patch",    "Tạo vùng gai độc làm chậm và gây độc",     "druid",       3.0f, 20f,  120f, false),
                ("lightning",      "Chain Lightning","Sét dây chuyền nhảy qua nhiều kẻ thù",      "",            1.8f, 50f,  0f,   false),
                ("frost_nova",     "Frost Nova",     "Băng bùng nổ, đóng băng tất cả xung quanh","",            3.5f, 45f,  150f, false),
                ("void_beam",      "Void Beam",      "Tia hủy diệt xuyên thẳng qua địch",        "",            1.0f, 75f,  0f,   true),
            };

            int created = 0;
            foreach (var (id, name, desc, branch, cd, dmg, aoe, legendary) in skills)
            {
                string path = $"{folder}/{id}.asset";
                if (AssetDatabase.LoadAssetAtPath<SkillData>(path) != null) continue;

                var s = ScriptableObject.CreateInstance<SkillData>();
                s.skillId       = id;
                s.skillName     = name;
                s.description   = desc;
                s.branchId      = branch;
                s.cooldown      = cd;
                s.baseDamage    = dmg;
                s.baseAoeRadius = aoe;
                s.isLegendary   = legendary;
                s.maxLevel      = legendary ? 1 : 5;
                s.skillColor    = legendary ? new Color(1f, 0.7f, 0f) : Color.white;
                s.isAoe         = aoe > 0;
                s.isProjectile  = aoe <= 0;

                AssetDatabase.CreateAsset(s, path);
                created++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Skill Data", $"Created {created} SkillData assets in:\n{folder}", "OK");
        }
    }
}
#endif
