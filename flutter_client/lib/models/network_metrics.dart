/// Métriques de diagnostics réseau en temps réel côté mobile
class MobileNetworkMetrics {
  int packetsReceived = 0;
  int packetsLost = 0;
  double packetsPerSecond = 0.0;
  double latencyMs = 0.0;
  int lastSequenceNumber = -1;
  DateTime lastPacketTime = DateTime.now();

  double get packetLossPercentage {
    final total = packetsReceived + packetsLost;
    if (total == 0) return 0.0;
    return (packetsLost / total) * 100.0;
  }

  void recordPacket(int sequenceNumber, int remoteTimestampMs) {
    packetsReceived++;
    final now = DateTime.now();

    // Détection de perte de paquets
    if (lastSequenceNumber >= 0 && sequenceNumber > lastSequenceNumber + 1) {
      packetsLost += (sequenceNumber - lastSequenceNumber - 1);
    }
    lastSequenceNumber = sequenceNumber;

    // Calcul de latence approximative si l'horloge système est calibrée
    if (remoteTimestampMs > 0) {
      final localMs = now.millisecondsSinceEpoch;
      // Différence positive indicative
      final diff = (localMs - remoteTimestampMs).abs();
      if (diff < 1000) {
        latencyMs = (latencyMs * 0.9) + (diff * 0.1); // Moyenne lissée
      }
    }

    lastPacketTime = now;
  }

  void reset() {
    packetsReceived = 0;
    packetsLost = 0;
    packetsPerSecond = 0.0;
    latencyMs = 0.0;
    lastSequenceNumber = -1;
    lastPacketTime = DateTime.now();
  }
}
