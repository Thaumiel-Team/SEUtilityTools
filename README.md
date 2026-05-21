# SE Utility Tools

A desktop utility application for **Space Engineers** players. Browse blueprint statistics, query dedicated servers, and manage game-related configurations.

---

## Features

### Blueprint Calculator
- **Browse Local Blueprints:** Automatically discovers and loads all blueprints from `%AppData%\SpaceEngineers\Blueprints\local`
- **Detailed Statistics:** View total PCU, block count, large/small grid distribution, and unique block types
- **Component Breakdown:** Aggregates all required materials and components needed to build the blueprint
- **Block Inventory:** Grouped block list with quantities, grid size, and individual PCU values
- **Icon Support:** Displays blueprint thumbnails and block icons (with DDS-to-PNG conversion support)
- **Async Loading:** Non-blocking blueprint loading with progress tracking for large collections

### Server Query
- **Live Server Status** Query any Space Engineers steam server
- **Player Information:** View current player count, max slots, and individual player session times
- **Query History:** Maintains a list of previously queried servers for quick re-checks
- **One-Click Refresh:** Update server data without re-entering connection details

### Settings & Configuration
- **Game Path:** Configure where your Space Engineers installation directory is
- **Debug Logging:** Toggle verbose console logging for troubleshooting
- **Icon Conversion:** Optionally convert block icons from DDS to PNG at startup
- **Persistent Config:** All settings are saved and restored between sessions

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| UI Framework | Avalonia UI |
| Icons | Projektanker.Icons.Avalonia (FontAwesome) |
| Dialogs | MsBox.Avalonia |
| Server Query | Okolni.Source.Query |
| Serialization | YAML |
| Data | Space Engineers `.sbc` XML |

---

## Prerequisites

- **.NET 10.0** (or later)
- **Space Engineers** installed (for blueprint and block data features)
- Windows, or Linux

---

## Configuration

On first launch, navigate to **Settings** and set:

| Setting | Description |
|---------|-------------|
| `Space Engineers Directory` | Path to your Space Engineers installation (e.g., `C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers`) |
| `Debug Mode` | Enable for detailed log output |
| `Convert Icons on Start` | Automatically convert DDS icons to PNG for better display |

Configuration is persisted to disk and reloaded automatically on startup.