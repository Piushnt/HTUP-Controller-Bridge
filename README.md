![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Flutter](https://img.shields.io/badge/Flutter-3.x-02569B?logo=flutter)
![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS%20%7C%20Android%20%7C%20iOS-lightgrey)
![License](https://img.shields.io/badge/License-MIT-blue.svg)

# HTUP Controller Bridge (Ulrich PadLink)

**Transformez n'importe quelle manette USB/DirectInput générique (Ucom, Twin Joystick, etc.) branchée sur PC en un contrôleur sans fil fluide et universel pour smartphone (Android/iOS) avec émulation XInput et interface Windows 11 Fluent.**

---

## 🎮 Fonctionnalités Clés

- 🕹️ **Acquisition Réelle & Universelle** : Pilote natif Windows WinMM / DirectInput avec support de 100% des manettes USB génériques et Ucom à ~125 Hz.
- 🔌 **Détection à Chaud (Hot-Plug)** : Branchement et débranchement des périphériques en temps réel sans blocage ni plantage.
- 🛡️ **Émulation XInput Canonique** : Normalisation de l'état sous forme d'une manette Xbox virtuelle pour une compatibilité parfaite avec les jeux mobiles AAA (*Call of Duty: Mobile*, *Delta Force*, *Warzone Mobile*).
- ⚡ **Auto-Remap Wizard Pas-à-Pas** : Configuration complète de la manette bouton par bouton sans toucher à la souris.
- 🎯 **Visual Remapper Interactif** : Cliquez sur n'importe quel bouton de la manette stylisée pour écouter et assigner un bouton physique instantanément.
- 📊 **Radars Néon & Sliders Réactifs** : Jauges circulaires pour sticks analogiques gauche (cyan) et droit (violet) avec réglage en direct des deadzones radiales et sensibilités.
- 📡 **Streaming UDP 120Hz Basse Latence** : Protocole binaire ultra-compact de 64 octets avec contrôle d'intégrité (latence < 2.5 ms).
- 📱 **Client Mobile Flutter Aligné** : Application Android / iOS moderne avec thème Fluent, visualiseur en temps réel et métriques de télémétrie réseau.

---

## 🚀 Démarrage Rapide

### 1. Application Desktop (PC Windows)
```bash
cd src/ControllerBridge.Desktop
dotnet run
```

### 2. Client Mobile (Android / iOS)
```bash
cd flutter_client
flutter pub get
flutter run
```

---

## 📚 Documentation Technique

- [Architecture Système](docs/ARCHITECTURE.md) - Conception globale, séparation en couches et flux de données
- [Spécification du Protocole Réseau](docs/PROTOCOL.md) - Format de trame binaire 64 octets et mapping XInput
- [Guide Développeur](docs/DEVELOPMENT.md) - Guide de compilation, extension de backends et styles XAML
- [Stratégie de Tests](docs/TESTING.md) - Tests unitaires et validation

---

## 📂 Structure du Répertoire

```
HTUP-Controller-Bridge/
├── src/
│   ├── ControllerBridge.Core/           # Modèles de domaine, normalisation radiale et profils de jeu
│   ├── ControllerBridge.Acquisition/    # Pilote natif WindowsJoystickBackend et gestionnaire Hot-Plug
│   ├── ControllerBridge.Transport/      # Protocole ControllerMessage et serveur UdpControllerServer 120Hz
│   └── ControllerBridge.Desktop/        # Application Windows 11 Fluent UI (Avalonia 11.x MVVM)
│
├── flutter_client/                      # Client Mobile Flutter (Android & iOS)
│   ├── lib/models/                      # Décodage binaire et métriques
│   ├── lib/services/                    # Socket UDP et heartbeat
│   └── lib/widgets/                     # GamepadVisualizer et NetworkStatsCard
│
├── tests/                               # Tests unitaires C# et tests d'intégration
├── docs/                                # Spécifications et documentation complète
└── HtupControllerBridge.sln             # Solution .NET 8.0
```
