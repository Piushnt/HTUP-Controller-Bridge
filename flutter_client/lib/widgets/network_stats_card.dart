import 'package:flutter/material.dart';
import '../models/network_metrics.dart';
import '../services/udp_receiver_service.dart';

class NetworkStatsCard extends StatelessWidget {
  final MobileNetworkMetrics metrics;
  final ConnectionStatus status;

  const NetworkStatsCard({
    super.key,
    required this.metrics,
    required this.status,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFE2E8F0)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.03),
            blurRadius: 10,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'LIVE NETWORK & XINPUT STATUS',
                style: TextStyle(
                  fontSize: 11,
                  fontWeight: FontWeight.bold,
                  letterSpacing: 0.8,
                  color: Color(0xFF64748B),
                ),
              ),
              _buildStatusBadge(),
            ],
          ),
          const SizedBox(height: 12),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            children: [
              _buildMetricItem(
                'REFRESH RATE',
                '${metrics.packetsPerSecond.toStringAsFixed(1)} Hz',
                const Color(0xFF06B6D4),
              ),
              _buildMetricItem(
                'LATENCY',
                '${metrics.latencyMs.toStringAsFixed(1)} ms',
                const Color(0xFF10B981),
              ),
              _buildMetricItem(
                'PACKET LOSS',
                '${metrics.packetLossPercentage.toStringAsFixed(2)} %',
                metrics.packetLossPercentage > 1.0 ? const Color(0xFFEF4444) : const Color(0xFF64748B),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildStatusBadge() {
    Color badgeBg;
    Color badgeText;
    String label;

    switch (status) {
      case ConnectionStatus.connected:
        badgeBg = const Color(0xFFECFDF5);
        badgeText = const Color(0xFF065F46);
        label = '• 120HZ XINPUT';
        break;
      case ConnectionStatus.connecting:
        badgeBg = const Color(0xFFFEF3C7);
        badgeText = const Color(0xFFB45309);
        label = '• CONNECTING...';
        break;
      case ConnectionStatus.lost:
        badgeBg = const Color(0xFFFEF2F2);
        badgeText = const Color(0xFF991B1B);
        label = '• SIGNAL LOST';
        break;
      case ConnectionStatus.disconnected:
      default:
        badgeBg = const Color(0xFFF1F5F9);
        badgeText = const Color(0xFF64748B);
        label = '• OFFLINE';
        break;
    }

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: badgeBg,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: badgeText.withOpacity(0.3)),
      ),
      child: Text(
        label,
        style: TextStyle(
          fontSize: 10,
          fontWeight: FontWeight.bold,
          color: badgeText,
        ),
      ),
    );
  }

  Widget _buildMetricItem(String title, String value, Color valueColor) {
    return Column(
      children: [
        Text(
          title,
          style: const TextStyle(fontSize: 9, fontWeight: FontWeight.bold, color: Color(0xFF94A3B8)),
        ),
        const SizedBox(height: 3),
        Text(
          value,
          style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold, color: valueColor),
        ),
      ],
    );
  }
}
