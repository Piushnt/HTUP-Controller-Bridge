# HTUP Controller Bridge - Architecture Système

```mermaid
graph TD
    subgraph "Desktop (.NET 8.0 / Windows)"
        HW["🎮 Manette Physique USB / Ucom / DirectInput"] -->|WinMM & RawInput P/Invoke| Backend["WindowsJoystickBackend\n(~125 Hz Polling)"]
        Sim["🧪 SimulatedControllerBackend"] --> Manager["ControllerBackendManager"]
        Backend --> Manager
        
        Manager -->|Événements Raw Input| Normalizer["StandardInputNormalizer\n(Deadzones Radiales & Calibrage)"]
        Normalizer --> Profile["MappingProfile\n(Profil Ucom / Remapping)"]
        Profile --> Engine["BridgeEngine\n(Orchestrateur Principal)"]
        
        Engine -->|UI Dispatcher| UI["Avalonia UI (MainWindow)\n(Visualiseur & Statuts)"]
        Engine -->|PeriodicTimer 120Hz| Server["UdpControllerServer\n(Buffer 64 octets)"]
    end

    subgraph "Réseau Wi-Fi Local"
        Server -->|Trame UDP 64 bytes @ 120Hz| Network["Wi-Fi UDP Broadcast / Unicast"]
    end

    subgraph "Mobile Client (Flutter)"
        Network --> Client["UdpReceiverService\n(RawDatagramSocket)"]
        Client --> Parser["MobileControllerState.fromBinary()"]
        Client --> Telemetry["MobileNetworkMetrics\n(Hz, Jitter, Perte)"]
        
        Parser --> Screen["HomeScreen\n(PadLink Flutter)"]
        Telemetry --> Screen
        Screen --> Viz["GamepadVisualizer\n(Sticks 2D, Triggers, Boutons)"]
    end
```

## Modules et Responsabilités

1. **`ControllerBridge.Core`**
   - Modèles de domaine universels (`ControllerState`, `ControllerButton`, `AnalogAxis`, `ControllerCapabilities`).
   - Moteur de normalisation mathématique (`IInputNormalizer`, `StandardInputNormalizer`) avec support des deadzones radiales 2D et des courbes de sensibilité.
   - Système de remapping et profils prédéfinis (`MappingProfile`, profil Ucom).
   - Métriques de diagnostic (`NetworkMetrics`, `ControllerDiagnostics`).

2. **`ControllerBridge.Acquisition`**
   - Pilote d'acquisition natif Windows (`WindowsJoystickBackend`) basé sur WinMM et HID, supportant 100% des manettes USB génériques et Ucom sans DLL externe.
   - Gestion dynamique du **Hot-Plug** (détection automatique de branchement/débranchement).
   - Backend simulé (`SimulatedControllerBackend`) pour tests autonomes.
   - `ControllerBackendManager` pour commutation fluide entre matériel réel et simulation.

3. **`ControllerBridge.Transport`**
   - Protocole binaire compact 64 octets (`ControllerMessage`).
   - Serveur UDP asynchrone non-bloquant (`UdpControllerServer`) cadencé à 120Hz.
   - Générateur de QR Code pour appairage automatique (`QrCodeGenerator`).

4. **`ControllerBridge.Desktop`**
   - Application de bureau Avalonia UI 11.x MVVM.
   - Service d'orchestration (`BridgeEngine`).
   - Interface moderne avec visualiseur d'axes, barres de triggers, boutons en direct, et monitoring réseau.

5. **`flutter_client`**
   - Application mobile Flutter (Android & iOS).
   - Réception UDP basse latence (`UdpReceiverService`).
   - Visualiseur de manette virtuel dynamique avec rendu 2D des sticks par `CustomPainter`.
