# HTUP Controller Bridge - Feuille de Route (Roadmap)

## Statut Actuel : Version 1.0.0 (Production-Ready)

| Phase | Description | Statut |
|---|---|---|
| **Phase 1 : Cœur & Normalisation** | Modèles canoniques, deadzones radiales 2D, filtrage anti-drift | ✅ Terminé |
| **Phase 2 : Acquisition Réelle** | Pilote WindowsJoystickBackend (WinMM/DirectInput), détection Hot-Plug dynamique | ✅ Terminé |
| **Phase 3 : Transport UDP 120Hz** | Protocole binaire 64 octets, streaming cadencé et monitoring | ✅ Terminé |
| **Phase 4 : UI Windows 11 Fluent** | Interface pixel-perfect, manette Xbox stylisée, radars néon cyan/violet, sliders réactifs | ✅ Terminé |
| **Phase 5 : Auto-Remap Wizard** | Assistant séquentiel bouton par bouton 100% sans les mains + remapping direct cliquable | ✅ Terminé |
| **Phase 6 : Client Mobile Flutter** | Application Android/iOS complète avec visualiseur dynamique et télémétrie | ✅ Terminé |

---

## Prochaines Évolutions (v1.1+)

- 📡 **Découverte Automatique mDNS / ZeroConf** : Éliminer la saisie manuelle de l'adresse IP en permettant à l'application mobile de détecter automatiquement le pont PC sur le réseau local.
- 📳 **Haptic Feedback / Retours de Vibration** : Transmission des paquets de retour de force depuis le mobile vers les moteurs de vibration de la manette PC.
- 🔵 **Bluetooth HID Direct** (expérimental) : Support du profil Bluetooth HID pour les environnements sans réseau Wi-Fi local.
