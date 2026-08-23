import 'dart:async';
import 'dart:io';
import 'dart:typed_data';
import 'package:flutter/foundation.dart';
import '../models/controller_state.dart';
import '../models/network_metrics.dart';

enum ConnectionStatus {
  disconnected,
  connecting,
  connected,
  lost,
}

/// Service de réception UDP basse latence pour le flux de manette
class UdpReceiverService extends ChangeNotifier {
  RawDatagramSocket? _socket;
  String _serverHost = '192.168.1.100';
  int _serverPort = 5555;
  ConnectionStatus _status = ConnectionStatus.disconnected;

  MobileControllerState _latestState = MobileControllerState();
  final MobileNetworkMetrics _metrics = MobileNetworkMetrics();

  Timer? _heartbeatTimer;
  Timer? _watchdogTimer;
  int _windowPacketCount = 0;
  DateTime _lastWindowTime = DateTime.now();

  ConnectionStatus get status => _status;
  String get serverHost => _serverHost;
  int get serverPort => _serverPort;
  MobileControllerState get latestState => _latestState;
  MobileNetworkMetrics get metrics => _metrics;

  /// Démarre la connexion au serveur Desktop
  Future<void> connect(String host, int port) async {
    _serverHost = host.trim();
    _serverPort = port;

    await disconnect();

    _status = ConnectionStatus.connecting;
    _metrics.reset();
    notifyListeners();

    try {
      _socket = await RawDatagramSocket.bind(InternetAddress.anyIPv4, 0);
      _socket!.listen(
        _onSocketEvent,
        onError: (error) {
          debugPrint('Erreur socket UDP: $error');
          _setStatus(ConnectionStatus.lost);
        },
      );

      // Envoyer un message de handshake au serveur PC
      _sendHandshake();

      // Boucle de heartbeat (1 fois par seconde pour maintenir la route UDP active)
      _heartbeatTimer = Timer.periodic(const Duration(seconds: 1), (_) {
        if (_status == ConnectionStatus.connected || _status == ConnectionStatus.connecting) {
          _sendHandshake();
        }
      });

      // Watchdog de détection de perte de signal et calcul de Hz
      _watchdogTimer = Timer.periodic(const Duration(milliseconds: 500), (_) {
        _checkLiveness();
      });
    } catch (e) {
      debugPrint('Impossible de lier le socket UDP: $e');
      _setStatus(ConnectionStatus.disconnected);
    }
  }

  void _sendHandshake() {
    if (_socket == null) return;
    try {
      // Paquet de ping/handshake minimal (1 octet version=1, 1 octet type=1 HandshakeRequest)
      final pingBuffer = Uint8List(64);
      pingBuffer[0] = 0x01; // Version 1.0
      pingBuffer[1] = 0x01; // HandshakeRequest
      _socket!.send(pingBuffer, InternetAddress(_serverHost), _serverPort);
    } catch (e) {
      debugPrint('Erreur envoi handshake: $e');
    }
  }

  void _onSocketEvent(RawSocketEvent event) {
    if (event == RawSocketEvent.read && _socket != null) {
      final datagram = _socket!.receive();
      if (datagram != null) {
        _processPacket(datagram.data);
      }
    }
  }

  void _processPacket(Uint8List bytes) {
    try {
      final state = MobileControllerState.fromBinary(bytes);
      _latestState = state;
      _metrics.recordPacket(state.sequenceNumber, state.timestampMs);
      _windowPacketCount++;

      if (_status != ConnectionStatus.connected) {
        _setStatus(ConnectionStatus.connected);
      }

      notifyListeners();
    } catch (e) {
      debugPrint('Erreur décodage paquet: $e');
    }
  }

  void _checkLiveness() {
    final now = DateTime.now();
    final elapsedSec = now.difference(_lastWindowTime).inMilliseconds / 1000.0;
    if (elapsedSec >= 0.5) {
      _metrics.packetsPerSecond = _windowPacketCount / elapsedSec;
      _windowPacketCount = 0;
      _lastWindowTime = now;
    }

    if (_status == ConnectionStatus.connected) {
      if (now.difference(_metrics.lastPacketTime).inMilliseconds > 2000) {
        _setStatus(ConnectionStatus.lost);
      }
    }
    notifyListeners();
  }

  void _setStatus(ConnectionStatus newStatus) {
    if (_status != newStatus) {
      _status = newStatus;
      notifyListeners();
    }
  }

  /// Déconnecte et libère les ressources
  Future<void> disconnect() async {
    _heartbeatTimer?.cancel();
    _heartbeatTimer = null;
    _watchdogTimer?.cancel();
    _watchdogTimer = null;

    _socket?.close();
    _socket = null;
    _status = ConnectionStatus.disconnected;
    _metrics.reset();
    _latestState = MobileControllerState();
    notifyListeners();
  }

  @override
  void dispose() {
    disconnect();
    super.dispose();
  }
}
