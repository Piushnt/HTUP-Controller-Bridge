# HTUP Controller Bridge - Spécification du Protocole Réseau (v1.0)

## 1. Vue d'ensemble
Le protocole de transport **PadLink** est conçu pour un streaming UDP ultra-rapide à basse latence (~120 Hz) entre le pont PC (`ControllerBridge.Desktop`) et le client mobile (`flutter_client`).

- **Format de trame :** Binaire compact à taille fixe (64 octets).
- **Transport :** UDP (Port par défaut : `5555`).
- **Endianness :** Little-Endian (Standard IEEE 754 pour les nombres à virgule flottante).

---

## 2. Structure du Paquet Binaire (64 Octets)

| Offset | Champ | Type | Description |
|---|---|---|---|
| `0` | **ProtocolVersion** | `uint8` | Version majeure du protocole (`0x01`) |
| `1` | **MessageType** | `uint8` | Type de message (`0x01`: HandshakeReq, `0x02`: HandshakeResp, `0x03`: ControllerState) |
| `2-5` | **SequenceNumber** | `uint32` | Numéro de séquence séquentiel pour calcul de perte de paquets |
| `6-13` | **TimestampMs** | `uint64` | Horodatage système en millisecondes |
| `14-16` | **ButtonBitfield** | `uint8[3]` | Masque binaire de 24 bits pour l'état des boutons (1 = Pressé, 0 = Relâché) |
| `17-20` | **LeftStickX** | `float32` | Axe X du stick gauche normalisé `[-1.0, 1.0]` |
| `21-24` | **LeftStickY** | `float32` | Axe Y du stick gauche normalisé `[-1.0, 1.0]` |
| `25-28` | **RightStickX** | `float32` | Axe X du stick droit normalisé `[-1.0, 1.0]` |
| `29-32` | **RightStickY** | `float32` | Axe Y du stick droit normalisé `[-1.0, 1.0]` |
| `33` | **LeftTrigger** | `uint8` | Gâchette gauche (`0` à `255` mappé en `0.0` à `1.0`) |
| `34` | **RightTrigger** | `uint8` | Gâchette droite (`0` à `255` mappé en `0.0` à `1.0`) |
| `35` | **DPad** | `uint8` | Croix directionnelle (Bits 0-1: `X + 1`, Bits 2-3: `Y + 1`) |
| `36` | **Flags** | `uint8` | Bit 0: `IsConnected`, Bit 1: `IsCalibrated` |
| `37-40` | **ClientId** | `uint32` | Identifiant du client mobile assigné |
| `41-62` | **Reserved** | `uint8[22]` | Octets de réserve pour extensions futures (Haptic/IMU) |
| `63` | **Checksum** | `uint8` | Checksum XOR sur les octets `0` à `62` |

---

## 3. Mapping des Boutons (Masque binaire 24 bits)

| Bit Index | Bouton Canonique | Rôle Physique Ucom |
|---|---|---|
| `0` | `A` | Bouton 1 / Croix |
| `1` | `B` | Bouton 2 / Rond |
| `2` | `X` | Bouton 3 / Carré |
| `3` | `Y` | Bouton 4 / Triangle |
| `4` | `LB` | Gâchette L1 |
| `5` | `RB` | Gâchette R1 |
| `6` | `LT_Button` | Gâchette L2 digitale |
| `7` | `RT_Button` | Gâchette R2 digitale |
| `8` | `LS` | Clic stick gauche (L3) |
| `9` | `RS` | Clic stick droit (R3) |
| `10` | `Start` | Bouton Start |
| `11` | `Back` | Bouton Select |
| `12` | `DPadUp` | Croix Haut |
| `13` | `DPadDown` | Croix Bas |
| `14` | `DPadLeft` | Croix Gauche |
| `15` | `DPadRight` | Croix Droite |
| `16` | `Guide` | Bouton Home / Mode |
