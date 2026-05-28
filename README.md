# 🎮 Angel Arena — Unity Edition

## Giới thiệu
Angel Arena Unity là phiên bản game survival roguelite 2.5D làm lại từ phiên bản web, 
được thiết kế để xuất bản trên **Steam**.

## Yêu cầu
- Unity 2022.3 LTS (tải từ https://unity.com/download)
- Windows 10/11 64-bit
- Visual Studio 2022 hoặc VS Code với C# extension

## Cài đặt

### Bước 1: Cài Unity Hub
1. Truy cập https://unity.com/download
2. Tải Unity Hub
3. Cài Unity 2022.3 LTS với modules:
   - Windows Build Support (IL2CPP)
   - WebGL Build Support (optional)
   - Android Build Support (optional)

### Bước 2: Mở Project
1. Mở Unity Hub
2. Click "Add" > chọn thư mục `AngelArena-Unity`
3. Unity sẽ tự động import packages từ `Packages/manifest.json`

### Bước 3: Cài Steamworks.NET
1. Vào Window > Package Manager
2. Click "+" > "Add package from git URL"
3. Nhập: `https://github.com/rlabrecque/Steamworks.NET.git?path=/com.rlabrecque.steamworks.net`

### Bước 4: Thiết lập Scenes
1. Tạo scene `MainMenu` và `GameScene`
2. Gán các GameObjects theo cấu trúc trong `Assets/Scripts/`

## Cấu trúc Scripts

| Folder | Nội dung |
|--------|---------|
| `Scripts/Core/` | GameManager, WaveScaling, CameraController, GameConstants |
| `Scripts/Player/` | PlayerController, SkillSystem |
| `Scripts/Skills/` | SkillHandlers (23+ skills) |
| `Scripts/Enemies/` | EnemyController, EnemySpawner |
| `Scripts/Data/` | ScriptableObjects (Character, Skill, Enemy, Passive) |
| `Scripts/UI/` | HUDManager, UIUpgradeScreen |
| `Scripts/Steam/` | SteamManager (achievements, leaderboard, cloud) |

## Steam Publishing

1. Đăng ký tài khoản Steamworks: https://partner.steamgames.com/
2. Trả phí Steam Direct: **$100**
3. Tạo App ID
4. Điền vào `SteamManager.cs`: `public uint appId = YOUR_APP_ID;`
5. Build: File > Build Settings > Windows x64 > Build

## Roadmap
- [ ] Phase 1: Core game loop
- [ ] Phase 2: 7 Characters + 30 skills
- [ ] Phase 3: 15+ enemy types + 6 bosses
- [ ] Phase 4: 2.5D map, lighting, VFX
- [ ] Phase 5: HUD, menus
- [ ] Phase 6: Steam integration
- [ ] Phase 7: Polish + QA
- [ ] Phase 8: Steam Launch 🚀

## Liên hệ
Project được phát triển từ Angel Arena Web (HTML5 Canvas).
