# 💣 MortalStrike

**MortalStrike** is a BepInEx mod for [SPT (Single Player Tarkov)](https://www.sp-tarkov.com/) that adds a dynamic artillery shelling system to raids. Supports both solo play and [Fika](https://github.com/project-fika/Fika-Plugin) multiplayer (including headless server mode).

---

## ✨ Features

- **Random artillery strikes** during raids with configurable chance and timing
- **Two escalating events:**
  - **Event 1 — Random Strike:** a focused barrage targeting player positions and key map landmarks
  - **Event 2 — Full Map Purge:** a total map-wide bombardment in rolling waves, triggered near the end of a raid
- **Smart target selection:** prioritizes live player positions, then map-specific landmarks, then random points
- **Cover detection:** players under roofs or behind walls take no damage
- **Audio warnings:** voiced countdowns (1 minute / 10 seconds / bombing / end) with rotation to avoid repetition
- **Full Fika support:** headless server manages events and syncs sound/damage to all clients via custom packets
- **Configurable:** all timings, chances, damage values, and wave parameters are exposed in BepInEx config

---

## 🗺️ Supported Maps

All standard SPT maps are supported with hand-placed landmark points for target selection:

`Shoreline` · `Woods` · `Customs` · `Interchange` · `Reserve` · `Lighthouse` · `Streets of Tarkov` · `Factory (Day/Night)` · `The Lab` · `Ground Zero`

---

## 📦 Installation

### Requirements
- SPT **3.10+**
- BepInEx (included with SPT)
- *(Optional)* Fika for multiplayer

### Steps

1. Download the latest release from the [Releases](../../releases) page
2. Extract the archive into your SPT folder
3. The structure should look like:
```
SPT/
└── BepInEx/
    └── plugins/
        └── MortalStrike/
            ├── MortalStrike.dll
            └── MortalStrikeSound/
                ├── 1MinuteTo.mp3
                ├── 10SecondsBefore.mp3
                ├── Bombing.mp3
                └── End.mp3
```
4. Launch SPT and start a raid

---

## ⚙️ Configuration

Config file is generated on first launch at:
```
BepInEx/config/com.mortalstrike.mod.cfg
```

| Section | Key | Default | Description |
|---|---|---|---|
| `Event1_RandomStrike` | `Chance` | `0.20` | Probability of Event 1 per raid (0.0–1.0) |
| `Event1_RandomStrike` | `MinDelayMinutes` | `5` | Minimum delay before strike begins |
| `Event1_RandomStrike` | `MaxDelayMinutes` | `10` | Maximum delay before strike begins |
| `Event1_RandomStrike` | `TargetCount` | `12` | Number of strike targets |
| `Event1_RandomStrike` | `DelayBetweenStrikes` | `10` | Seconds between individual strikes |
| `Event2_FullMapPurge` | `Chance` | `0.20` | Probability that Event 1 escalates to Event 2 |
| `Event2_FullMapPurge` | `StartBeforeEndMinutes` | `10` | Minutes before raid end when Event 2 triggers |
| `Event2_FullMapPurge` | `WaveDelaySec` | `90` | Seconds between bombardment waves |
| `Event2_FullMapPurge` | `ZonesPerWave` | `2` | Number of zones hit per wave |
| `Damage` | `DamageRadius` | `15` | Blast radius in meters |
| `Damage` | `DamageLethalRadius` | `4` | Lethal radius (max damage) in meters |
| `Damage` | `DamageMax` | `120` | Maximum damage (inside lethal radius) |
| `Damage` | `DamageMin` | `15` | Minimum damage (at edge of blast radius) |
| `Debug` | `ForceEventNextRaid` | `false` | Force an event to trigger in the next raid (for testing) |

---

## 🔊 Custom Audio

You can replace the audio files in `BepInEx/plugins/MortalStrike/MortalStrikeSound/` with your own `.mp3` files.

To add multiple variants per sound group (for rotation), use the naming pattern:
```
1MinuteTo.mp3
1MinuteTo(2).mp3
1MinuteTo(3).mp3
```
The mod will automatically pick a random unused clip per raid and cycle through all variants before repeating.

**Sound groups:**

| Group | File prefix | Plays when |
|---|---|---|
| 1 minute warning | `1MinuteTo` | 60 seconds before strike |
| 10 second warning | `10SecondsBefore` | 10 seconds before strike |
| Bombing | `Bombing` | Strike begins |
| End | `End` | Strike ends |

---

## 🖥️ Fika / Headless Support

MortalStrike fully supports Fika multiplayer:

- **Headless server** manages all event logic, shelling zones, and damage calculation
- Custom `MortalStrikePacket` broadcasts sound cues and damage instructions to all connected clients
- **Clients** receive packets, play audio locally, and apply damage to themselves
- Cover detection runs on the server using raycasts

---

## 🛠️ Building from Source

1. Clone the repository
2. Copy the required `.dll` references from your SPT installation into the project's reference folder (see `MortalStrike.csproj`)
3. Build with Visual Studio or `dotnet build`

---

## 📄 License

This project is released under the [MIT License](LICENSE).

---

## 🙏 Credits

- Built on top of SPT and BepInEx
- Multiplayer support via [Fika](https://github.com/project-fika/Fika-Plugin)
