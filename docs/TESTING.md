# Guide et Stratégie de Tests - HTUP Controller Bridge

Ce document décrit la suite de tests et les procédures de validation pour le projet **HTUP Controller Bridge**.

---

## 1. Tests Unitaires du Client Mobile (Flutter)

Les tests vérifient la désérialisation exacte des paquets binaires de 64 octets, le calcul des axes normalisés IEEE 754, le décodage du masque de boutons et du D-Pad.

### Exécution :
```bash
cd flutter_client
flutter test
```

### Fichier de test :
- `flutter_client/test/controller_state_test.dart` : Valide l'intégrité de la trame binaire 64 octets émise par le pont .NET 8.

---

## 2. Tests Unitaires C# (.NET 8.0)

### Projets de tests :
- `tests/ControllerBridge.Core.Tests` :
  - `MappingProfileTests.cs` : Valide la création de profils, la substitution d'identifiants et les profils de jeu (`COD_Mobile.json`, `Delta_Force.json`, `Default_Ucom.json`).
  - `StandardInputNormalizerTests.cs` : Valide les algorithmes de deadzone axiale, de **deadzone radiale 2D** (`NormalizeStick2D`) et les courbes de réponse.
- `tests/ControllerBridge.Transport.Tests` :
  - `ControllerMessageTests.cs` : Valide la sérialisation, la désérialisation aller-retour et le calcul du checksum d'intégrité sur 64 octets.

### Exécution :
```bash
dotnet test
```

---

## 3. Matrice de Validation Matérielle & Hot-Plug

| Test | Procédure | Résultat Attendu |
|---|---|---|
| **Détection Manette Ucom** | Brancher une manette USB Ucom / DirectInput | L'interface affiche `Controller Connected` et commence à lire les axes sans redémarrer |
| **Débranchement à chaud** | Débrancher le câble USB en plein streaming | L'état bascule en `Waiting for Gamepad...` sans crash ni exception non gérée |
| **Rebranchement à chaud** | Rebrancher la manette | Détection immédiate et reprise automatique du flux UDP @ 120Hz |
| **Auto-Remap Wizard** | Cliquer sur `⚡ Auto-Remap All` et presser les touches demandées | Assignation automatique des 16 boutons en séquence avec indication visuelle |
| **Réglage Deadzone** | Déplacer le slider Deadzone à `0.15` | Élimination instantanée du drift du stick visible sur le radar circulaire |
