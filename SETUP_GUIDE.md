# 🚀 Hướng Dẫn Thiết Lập Angel Arena trong Unity 6

## Bước 1: Mở Project sau khi Unity 6.4 cài xong

1. Mở **Unity Hub**
2. Click **"Add"** → **"Add project from disk"**
3. Chọn thư mục: `C:\Users\Truc Duy\.gemini\antigravity\scratch\AngelArena-Unity`
4. Chọn Unity version: **6000.4.8f1** (Unity 6.4)
5. Click **Open** — Unity sẽ import tự động (~2-5 phút)

> [!NOTE]
> Lần đầu mở sẽ thấy nhiều warning về missing references — đây là bình thường. Scripts đã compile sẵn.

---

## Bước 2: Tạo Scenes

### Main Menu Scene
1. **File → New Scene** → chọn "Basic (URP)"
2. Lưu: `Assets/Scenes/MainMenu.unity`

### Game Scene  
1. **File → New Scene** → chọn "Basic (URP)"
2. Lưu: `Assets/Scenes/GameScene.unity`
3. Thêm vào Build Settings: **File → Build Settings → Add Open Scenes**

---

## Bước 3: Thiết lập GameScene

### A. Camera
```
GameObject → Camera → đặt tên "Main Camera"
  Thêm component: CameraController
  Settings:
    - Projection: Orthographic
    - Size: 540 (hiển thị ~1080px height)
    - Clear Flags: Solid Color → màu #080A18
```

### B. GameManager
```
Create Empty → đặt tên "GameManager"
  Thêm component: GameManager
  Thêm component: SteamManager (nếu đã cài Steamworks.NET)
```

### C. Player
```
Create Empty → đặt tên "Player"
  Tag: "Player"
  Layer: "Player"
  
  Thêm components:
    - PlayerController
    - SkillSystem  
    - Rigidbody2D  (Gravity Scale: 0, Collision Detection: Continuous)
    - CircleCollider2D (Radius: 16)
    - SpriteRenderer
  
  Tạo thư mục con "Visual" với SpriteRenderer cho sprite nhân vật
```

### D. EnemySpawner
```
Create Empty → đặt tên "EnemySpawner"
  Thêm component: EnemySpawner
```

### E. SkillPrefabs Registry
```
Create Empty → đặt tên "SkillPrefabs"
  Thêm component: SkillPrefabs
  (Gán prefabs sau khi tạo)
```

### F. HUD Canvas
```
GameObject → UI → Canvas
  Canvas Scaler: Scale With Screen Size
  Reference Resolution: 1920 × 1080
  
  Thêm component: HUDManager
  
  Child objects:
    - HP Bar (Slider)
    - XP Bar (Slider)
    - Level Text (TMP_Text)
    - Survival Timer (TMP_Text)
    - Boss Bar Root (GameObject)
      - Boss HP Bar (Slider)
      - Boss Name Text (TMP_Text)
      - Boss Phase Text (TMP_Text)
    - Kill Streak Root (GameObject)
      - Kill Streak Text (TMP_Text)
    - Vignette Image (Image - full screen, Alpha=0)
    - Boss Warning Root (GameObject)
      - Boss Warning Text (TMP_Text)
```

---

## Bước 4: Cài Steamworks.NET (Optional nhưng khuyến nghị)

```
Window → Package Manager
→ + → Add package from git URL
→ Nhập: https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net
→ Add
```

---

## Bước 5: Tạo ScriptableObject Assets

### Characters (7 nhân vật)
```
Right-click Assets/ScriptableObjects/Characters/
→ Create → AngelArena → Character Data

Tạo 7 file:
  - Assassin.asset   (maxHp:180, speed:210, atkMult:1.45, defMult:0.65)
  - Fighter.asset    (maxHp:320, speed:155, atkMult:1.20, defMult:1.45)
  - Mage.asset       (maxHp:175, speed:165, atkMult:1.35, defMult:0.75)
  - Ranger.asset     (maxHp:195, speed:185, atkMult:1.25, defMult:0.85)
  - Paladin.asset    (maxHp:280, speed:145, atkMult:1.00, defMult:1.35)
  - Necromancer.asset(maxHp:200, speed:158, atkMult:1.10, defMult:0.90)
  - Druid.asset      (maxHp:240, speed:160, atkMult:0.95, defMult:1.10)
```

### Skills (tạo sau từng bước)
```
Right-click Assets/ScriptableObjects/Skills/
→ Create → AngelArena → Skill Data
```

---

## Bước 6: Test Game

1. Chọn **GameScene** trong hierarchy
2. Gán **selectedCharacter** trong GameManager Inspector
3. Nhấn **Play** ▶
4. WASD để di chuyển player
5. Skills tự động cast khi cooldown hết

---

## Bước 7: Build cho Windows

```
File → Build Settings
  Platform: PC, Mac & Linux Standalone
  Target Platform: Windows
  Architecture: x86_64
  
  Player Settings:
    Company Name: Angel Arena Studio
    Product Name: Angel Arena
    Version: 1.0.0
  
→ Build
```

---

## Bước 8: Steam Publishing

1. Đăng ký: https://partner.steamgames.com/
2. Trả $100 Steam Direct fee
3. Nhận App ID → điền vào `SteamManager.cs`
4. Cài Steamworks SDK
5. Upload build qua SteamCMD

---

## ⚠️ Lưu ý Unity 6

Unity 6.4 dùng API mới:
- `Rigidbody2D.linearVelocity` thay vì `velocity` ✅ (scripts đã dùng đúng)
- `Physics2D.OverlapCircleAll` ✅
- New Input System ✅
- URP 17.x ✅
