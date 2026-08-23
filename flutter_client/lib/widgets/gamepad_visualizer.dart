import 'package:flutter/material.dart';
import '../models/controller_state.dart';

/// Visualiseur de manette Xbox interactif avec design Windows 11 Fluent / Néon
class GamepadVisualizer extends StatelessWidget {
  final MobileControllerState state;

  const GamepadVisualizer({super.key, required this.state});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: const Color(0xFFE2E8F0)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.04),
            blurRadius: 16,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          // 1. Triggers & Shoulders Bar (LB, RB, LT, RT)
          _buildTriggersBar(),

          const SizedBox(height: 16),

          // 2. Stylized Gamepad Area
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            children: [
              // Left Section: D-Pad & Left Radar Stick
              Column(
                children: [
                  _buildRadarStick('LEFT STICK', state.leftStickX, state.leftStickY, const Color(0xFF06B6D4), state.isPressed(GamepadButton.ls)),
                  const SizedBox(height: 14),
                  _buildDPad(),
                ],
              ),

              // Center Section: Brand & Action Buttons
              Column(
                children: [
                  // Center Xbox Logo
                  Container(
                    width: 36,
                    height: 36,
                    decoration: BoxDecoration(
                      color: const Color(0xFF0F172A),
                      shape: BoxShape.circle,
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withOpacity(0.15),
                          blurRadius: 8,
                        ),
                      ],
                    ),
                    alignment: Alignment.center,
                    child: const Text('X', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 16)),
                  ),
                  const SizedBox(height: 14),
                  Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      _buildSmallMenuButton('⧉', state.isPressed(GamepadButton.back)),
                      const SizedBox(width: 14),
                      _buildSmallMenuButton('☰', state.isPressed(GamepadButton.start)),
                    ],
                  ),
                  const SizedBox(height: 14),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: state.isConnected ? const Color(0xFFECFDF5) : const Color(0xFFFEF2F2),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: state.isConnected ? const Color(0xFFA7F3D0) : const Color(0xFFFECACA)),
                    ),
                    child: Text(
                      state.isConnected ? '• XINPUT ACTIVE' : '• STANDBY',
                      style: TextStyle(
                        fontSize: 10,
                        fontWeight: FontWeight.bold,
                        color: state.isConnected ? const Color(0xFF065F46) : const Color(0xFF991B1B),
                      ),
                    ),
                  ),
                ],
              ),

              // Right Section: Face Buttons (A, B, X, Y) & Right Radar Stick
              Column(
                children: [
                  _buildFaceButtons(),
                  const SizedBox(height: 14),
                  _buildRadarStick('RIGHT STICK', state.rightStickX, state.rightStickY, const Color(0xFF8B5CF6), state.isPressed(GamepadButton.rs)),
                ],
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildTriggersBar() {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        // Left Trigger & Shoulder
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _buildTriggerGauge('LT (L2)', state.leftTrigger, const Color(0xFF06B6D4)),
            const SizedBox(height: 4),
            _buildShoulderButton('LB', state.isPressed(GamepadButton.lb), const Color(0xFF06B6D4)),
          ],
        ),

        // Right Trigger & Shoulder
        Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            _buildTriggerGauge('RT (R2)', state.rightTrigger, const Color(0xFF8B5CF6)),
            const SizedBox(height: 4),
            _buildShoulderButton('RB', state.isPressed(GamepadButton.rb), const Color(0xFF8B5CF6)),
          ],
        ),
      ],
    );
  }

  Widget _buildTriggerGauge(String label, double value, Color activeColor) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: const TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Color(0xFF64748B))),
        const SizedBox(height: 2),
        Container(
          width: 90,
          height: 8,
          decoration: BoxDecoration(
            color: const Color(0xFFF1F5F9),
            borderRadius: BorderRadius.circular(4),
          ),
          child: FractionallySizedBox(
            alignment: Alignment.centerLeft,
            widthFactor: value.clamp(0.0, 1.0),
            child: Container(
              decoration: BoxDecoration(
                color: activeColor,
                borderRadius: BorderRadius.circular(4),
              ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildShoulderButton(String label, bool isPressed, Color accentColor) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 5),
      decoration: BoxDecoration(
        color: isPressed ? accentColor : const Color(0xFFF8FAFC),
        borderRadius: BorderRadius.circular(6),
        border: Border.all(color: isPressed ? accentColor : const Color(0xFFCBD5E1)),
      ),
      child: Text(
        label,
        style: TextStyle(
          fontSize: 11,
          fontWeight: FontWeight.bold,
          color: isPressed ? Colors.white : const Color(0xFF334155),
        ),
      ),
    );
  }

  Widget _buildRadarStick(String label, double x, double y, Color accentColor, bool isClicked) {
    return Column(
      children: [
        Container(
          width: 76,
          height: 76,
          decoration: BoxDecoration(
            color: const Color(0xFFF8FAFC),
            shape: BoxShape.circle,
            border: Border.all(color: accentColor, width: 1.5),
          ),
          child: CustomPaint(
            painter: _RadarStickPainter(stickX: x, stickY: y, accentColor: accentColor, isClicked: isClicked),
          ),
        ),
        const SizedBox(height: 4),
        Text(label, style: const TextStyle(fontSize: 9, fontWeight: FontWeight.bold, color: Color(0xFF64748B))),
      ],
    );
  }

  Widget _buildDPad() {
    final up = state.dPadY > 0 || state.isPressed(GamepadButton.dPadUp);
    final down = state.dPadY < 0 || state.isPressed(GamepadButton.dPadDown);
    final left = state.dPadX < 0 || state.isPressed(GamepadButton.dPadLeft);
    final right = state.dPadX > 0 || state.isPressed(GamepadButton.dPadRight);

    return SizedBox(
      width: 74,
      height: 74,
      child: Stack(
        alignment: Alignment.center,
        children: [
          Positioned(top: 0, child: _buildDPadButton('▲', up)),
          Positioned(bottom: 0, child: _buildDPadButton('▼', down)),
          Positioned(left: 0, child: _buildDPadButton('◀', left)),
          Positioned(right: 0, child: _buildDPadButton('▶', right)),
          Container(width: 22, height: 22, color: const Color(0xFFF1F5F9)),
        ],
      ),
    );
  }

  Widget _buildDPadButton(String arrow, bool active) {
    return Container(
      width: 24,
      height: 24,
      decoration: BoxDecoration(
        color: active ? const Color(0xFF06B6D4) : const Color(0xFFF1F5F9),
        borderRadius: BorderRadius.circular(4),
        border: Border.all(color: active ? const Color(0xFF06B6D4) : const Color(0xFFCBD5E1)),
      ),
      alignment: Alignment.center,
      child: Text(
        arrow,
        style: TextStyle(fontSize: 10, color: active ? Colors.white : const Color(0xFF475569)),
      ),
    );
  }

  Widget _buildFaceButtons() {
    final a = state.isPressed(GamepadButton.a);
    final b = state.isPressed(GamepadButton.b);
    final x = state.isPressed(GamepadButton.x);
    final y = state.isPressed(GamepadButton.y);

    return SizedBox(
      width: 76,
      height: 76,
      child: Stack(
        alignment: Alignment.center,
        children: [
          // Y (Yellow)
          Positioned(top: 0, child: _buildCircleFaceButton('Y', const Color(0xFFF59E0B), const Color(0xFFFEF3C7), y)),
          // A (Green)
          Positioned(bottom: 0, child: _buildCircleFaceButton('A', const Color(0xFF10B981), const Color(0xFFDCFCE7), a)),
          // X (Blue)
          Positioned(left: 0, child: _buildCircleFaceButton('X', const Color(0xFF3B82F6), const Color(0xFFDBEAFE), x)),
          // B (Red)
          Positioned(right: 0, child: _buildCircleFaceButton('B', const Color(0xFFEF4444), const Color(0xFFFEE2E2), b)),
        ],
      ),
    );
  }

  Widget _buildCircleFaceButton(String label, Color borderCol, Color bgCol, bool pressed) {
    return Container(
      width: 24,
      height: 24,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        color: pressed ? borderCol : bgCol,
        border: Border.all(color: borderCol, width: 1.5),
      ),
      alignment: Alignment.center,
      child: Text(
        label,
        style: TextStyle(
          fontSize: 10,
          fontWeight: FontWeight.bold,
          color: pressed ? Colors.white : borderCol,
        ),
      ),
    );
  }

  Widget _buildSmallMenuButton(String label, bool pressed) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 3),
      decoration: BoxDecoration(
        color: pressed ? const Color(0xFF06B6D4) : const Color(0xFFF1F5F9),
        borderRadius: BorderRadius.circular(4),
        border: Border.all(color: const Color(0xFFCBD5E1)),
      ),
      child: Text(
        label,
        style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: pressed ? Colors.white : const Color(0xFF475569)),
      ),
    );
  }
}

class _RadarStickPainter extends CustomPainter {
  final double stickX;
  final double stickY;
  final Color accentColor;
  final bool isClicked;

  _RadarStickPainter({required this.stickX, required this.stickY, required this.accentColor, required this.isClicked});

  @override
  void paint(Canvas canvas, Size size) {
    final center = Offset(size.width / 2, size.height / 2);
    final radius = size.width / 2;

    // Crosshairs
    final gridPaint = Paint()
      ..color = const Color(0xFFE2E8F0)
      ..strokeWidth = 1;
    canvas.drawLine(Offset(center.dx, 0), Offset(center.dx, size.height), gridPaint);
    canvas.drawLine(Offset(0, center.dy), Offset(size.width, center.dy), gridPaint);

    // Dynamic dot position
    final maxTravel = radius * 0.65;
    final dotOffset = Offset(
      center.dx + (stickX.clamp(-1.0, 1.0) * maxTravel),
      center.dy + (stickY.clamp(-1.0, 1.0) * maxTravel),
    );

    // Dot paint with glow
    final dotPaint = Paint()
      ..color = isClicked ? Colors.black : accentColor
      ..style = PaintingStyle.fill;
    canvas.drawCircle(dotOffset, 6, dotPaint);
  }

  @override
  bool shouldRepaint(covariant _RadarStickPainter oldDelegate) {
    return oldDelegate.stickX != stickX || oldDelegate.stickY != stickY || oldDelegate.isClicked != isClicked;
  }
}
