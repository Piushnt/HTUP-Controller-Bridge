# HTUP Controller Bridge - Guide Développeur

Ce guide fournit l'ensemble des informations pour compiler, développer et étendre la solution **HTUP Controller Bridge**.

---

## 1. Structure Technique de la Solution

```
HtupControllerBridge.sln
├── src/ControllerBridge.Core/          # Modèles, normalisation radiale 2D, gestionnaire de profils JSON
├── src/ControllerBridge.Acquisition/   # Pilotes physiques (WindowsJoystickBackend) et gestionnaire Hot-Plug
├── src/ControllerBridge.Transport/     # Protocole binaire 64 octets et serveur UDP cadencé à 120Hz
├── src/ControllerBridge.Desktop/       # Application bureau Avalonia UI 11.x (MVVM Windows 11 Fluent)
├── flutter_client/                     # Application mobile Flutter (Android / iOS)
└── tests/                              # Tests unitaires et d'intégration
```

---

## 2. Compilation et Exécution

### Prérequis
- **.NET 8.0 SDK**
- **Flutter SDK 3.x+**

### Desktop (PC Windows)
```powershell
cd src/ControllerBridge.Desktop
dotnet build
dotnet run
```

### Client Mobile (Flutter)
```powershell
cd flutter_client
flutter pub get
flutter test
flutter run
```

---

## 3. Composants Clés & Mécanismes Internes

### A. Pilote d'Acquisition Physique (`WindowsJoystickBackend.cs`)
- Utilise l'API Windows `winmm.dll` (`joyGetPosEx`, `joyGetDevCaps`) pour capturer 100% des manettes USB génériques et Ucom sans nécessiter de pilote tiers ni de DLL native externe.
- Polling cadencé à ~125 Hz via `PeriodicTimer` et surveillance Hot-Plug en arrière-plan.

### B. Auto-Remap Wizard (`MainWindowViewModel.cs`)
- Permet de remapper toute la manette en séquence pas-à-pas sans interaction souris :
  `A` ➔ `B` ➔ `X` ➔ `Y` ➔ `LB` ➔ `RB` ➔ `LT` ➔ `RT` ➔ `D-Pad (4 directions)` ➔ `LS` ➔ `RS` ➔ `Back` ➔ `Start`.
- Les coordonnées d'ancrage de la bulle d'annotation et de la ligne pointillée se mettent à jour automatiquement selon le bouton ciblé.

### C. Client Mobile & Protocole Réseau (`flutter_client`)
- Socket UDP non-bloquant (`RawDatagramSocket`).
- Décodage binaire direct d'un buffer de 64 octets Little-Endian avec contrôle d'intégrité Checksum XOR.
