namespace ControllerBridge.Desktop.ViewModels;

using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControllerBridge.Acquisition;
using ControllerBridge.Core.Domain;
using ControllerBridge.Core.Mapping;
using ControllerBridge.Desktop.Services;
using ControllerBridge.Transport.WiFi;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly BridgeEngine _engine;

    // Navigation Tabs
    [ObservableProperty] private string _selectedTab = "Controller"; // Controller, Guide, Mapping, Profiles, Settings
    [ObservableProperty] private bool _isControllerTabSelected = true;
    [ObservableProperty] private bool _isGuideTabSelected = false;
    [ObservableProperty] private bool _isMappingTabSelected = false;
    [ObservableProperty] private bool _isProfilesTabSelected = false;
    [ObservableProperty] private bool _isSettingsTabSelected = false;
    [ObservableProperty] private string _guideActiveSection = "pc"; // "pc", "mobile", "mapping", "conflicts"

    // Operating Modes: Network Bridge (Mobile UDP) vs Xbox 360 Local Emulation (PC Direct)
    [ObservableProperty] private bool _isXboxEmulationMode = false;
    [ObservableProperty] private string _operatingModeName = "📡 Wi-Fi UDP Bridge (Mobile)";
    [ObservableProperty] private string _virtualControllerStatus = "ViGEmBus: Ready";
    [ObservableProperty] private bool _isViGEmAvailable = false;

    // Controller Status & Device Info
    [ObservableProperty] private string _controllerName = "Xbox Wireless Controller";
    [ObservableProperty] private string _controllerStatus = "Controller Connected";
    [ObservableProperty] private bool _isControllerConnected = true;
    [ObservableProperty] private bool _isSimulationMode = false;
    [ObservableProperty] private int _batteryPercent = 87;
    [ObservableProperty] private double _latencyMs = 2.1;
    [ObservableProperty] private string _inputMode = "Windows (XInput)";

    // Live Stick Positions [-1.0, 1.0]
    [ObservableProperty] private float _leftStickX = 0.12f;
    [ObservableProperty] private float _leftStickY = -0.08f;
    [ObservableProperty] private float _rightStickX = -0.07f;
    [ObservableProperty] private float _rightStickY = 0.15f;

    // Formatted Position Strings for Radar Cards
    [ObservableProperty] private string _leftStickPositionText = "(0.12, -0.08)";
    [ObservableProperty] private string _rightStickPositionText = "(-0.07, 0.15)";

    // Canvas Offset Values for Radar Dot (Center is 65,65 in a 130x130 box, radius 45)
    [ObservableProperty] private double _leftStickDotLeft = 65 + (0.12 * 45) - 6;
    [ObservableProperty] private double _leftStickDotTop = 65 + (-0.08 * 45) - 6;
    [ObservableProperty] private double _rightStickDotLeft = 65 + (-0.07 * 45) - 6;
    [ObservableProperty] private double _rightStickDotTop = 65 + (0.15 * 45) - 6;

    // Live Triggers [0.0, 1.0]
    [ObservableProperty] private float _leftTrigger = 0f;
    [ObservableProperty] private float _rightTrigger = 0f;

    // D-Pad
    [ObservableProperty] private int _dPadX = 0;
    [ObservableProperty] private int _dPadY = 0;

    // Active Pressed Buttons for Visual Glow
    [ObservableProperty] private bool _isButtonPressed_A;
    [ObservableProperty] private bool _isButtonPressed_B;
    [ObservableProperty] private bool _isButtonPressed_X;
    [ObservableProperty] private bool _isButtonPressed_Y;
    [ObservableProperty] private bool _isButtonPressed_LB;
    [ObservableProperty] private bool _isButtonPressed_RB;
    [ObservableProperty] private bool _isButtonPressed_LT;
    [ObservableProperty] private bool _isButtonPressed_RT;
    [ObservableProperty] private bool _isButtonPressed_LS;
    [ObservableProperty] private bool _isButtonPressed_RS;
    [ObservableProperty] private bool _isButtonPressed_Start;
    [ObservableProperty] private bool _isButtonPressed_Back;
    [ObservableProperty] private bool _isButtonPressed_Guide;
    [ObservableProperty] private bool _isButtonPressed_DPadUp;
    [ObservableProperty] private bool _isButtonPressed_DPadDown;
    [ObservableProperty] private bool _isButtonPressed_DPadLeft;
    [ObservableProperty] private bool _isButtonPressed_DPadRight;

    // Visual Joystick Offsets on Gamepad Controller Model [-14px, +14px]
    [ObservableProperty] private double _leftStickVisualOffsetX = 0;
    [ObservableProperty] private double _leftStickVisualOffsetY = 0;
    [ObservableProperty] private double _rightStickVisualOffsetX = 0;
    [ObservableProperty] private double _rightStickVisualOffsetY = 0;

    // Real-time Trigger Percentages [0, 100]
    [ObservableProperty] private int _leftTriggerPercent = 0;
    [ObservableProperty] private int _rightTriggerPercent = 0;

    // Real-time Input Monitor Status
    [ObservableProperty] private string _activePressedButtonsSummary = "Ready / Idle";
    [ObservableProperty] private bool _hasActiveInputs = false;

    // Remapping / "Listening..." State (Default is FALSE: only appears when user initiates mapping)
    [ObservableProperty] private bool _isListeningForBinding = false;
    [ObservableProperty] private string _listeningTargetName = "";
    [ObservableProperty] private string _listeningInstruction = "";
    [ObservableProperty] private ControllerButton? _activeListeningButton = null;
    [ObservableProperty] private string _lastRemapSuccessMessage = "";

    // Dynamic Positions for Floating Annotation Bubble & Line Anchor
    [ObservableProperty] private double _bubbleAnchorX = 405;
    [ObservableProperty] private double _bubbleAnchorY = 152;
    [ObservableProperty] private double _bubbleLineEndX = 480;
    [ObservableProperty] private double _bubbleLineEndY = 205;
    [ObservableProperty] private double _bubbleLeft = 470;
    [ObservableProperty] private double _bubbleTop = 185;
    [ObservableProperty] private Point _bubbleStartPoint = new Point(412, 159);
    [ObservableProperty] private Point _bubbleEndPoint = new Point(480, 205);

    // Full Step-by-Step Auto Remapping Wizard Mode
    [ObservableProperty] private bool _isWizardActive = false;
    [ObservableProperty] private int _wizardCurrentStep = 0;
    [ObservableProperty] private int _wizardTotalSteps = 16;
    [ObservableProperty] private string _wizardProgressText = "";

    private readonly ControllerButton[] _wizardSequence = new[]
    {
        ControllerButton.A,
        ControllerButton.B,
        ControllerButton.X,
        ControllerButton.Y,
        ControllerButton.LB,
        ControllerButton.RB,
        ControllerButton.LT_Button,
        ControllerButton.RT_Button,
        ControllerButton.DPadUp,
        ControllerButton.DPadDown,
        ControllerButton.DPadLeft,
        ControllerButton.DPadRight,
        ControllerButton.LS,
        ControllerButton.RS,
        ControllerButton.Back,
        ControllerButton.Start
    };

    // Game Profiles Management
    [ObservableProperty] private string _selectedProfileName = "COD_Mobile.json";
    [ObservableProperty] private bool _isProfileDropdownOpen = false;
    public ObservableCollection<GameProfileItem> AvailableProfiles { get; } = new();

    // Stick Settings Sliders
    [ObservableProperty] private double _deadzone = 0.15;
    [ObservableProperty] private double _sensitivity = 1.35;

    // Network & Streaming
    [ObservableProperty] private string _serverStatus = "UDP Server Active (Port 5555)";
    [ObservableProperty] private string _localIpAddress = "192.168.1.100";
    [ObservableProperty] private int _serverPort = 5555;
    [ObservableProperty] private int _connectedClientCount = 1;
    [ObservableProperty] private double _packetsPerSecond = 120.0;
    [ObservableProperty] private ulong _totalPacketsSent = 14520;
    [ObservableProperty] private double _packetLossPercent = 0.0;

    public ObservableCollection<string> ConnectedControllersList { get; } = new();

    private DateTime _lastButtonPressHandled = DateTime.MinValue;

    public MainWindowViewModel(BridgeEngine engine)
    {
        _engine = engine;
        LocalIpAddress = QrCodeGenerator.GetLocalIpAddress();
        ServerPort = _engine.UdpServer.Port;

        IsViGEmAvailable = _engine.VirtualXbox.IsViGEmAvailable;
        UpdateModeProperties(_engine.OperatingMode);

        InitializeProfiles();
        HookEngineEvents();
        UpdateControllerList();
    }

    private void UpdateModeProperties(BridgeOperatingMode mode)
    {
        IsXboxEmulationMode = (mode == BridgeOperatingMode.XboxEmulation);
        OperatingModeName = IsXboxEmulationMode 
            ? "🎮 Xbox 360 Emulation (PC Direct)" 
            : "📡 Wi-Fi UDP Bridge (Mobile)";
        VirtualControllerStatus = _engine.VirtualXbox.StatusDescription;
    }

    private void InitializeProfiles()
    {
        AvailableProfiles.Clear();
        AvailableProfiles.Add(new GameProfileItem { Name = "COD_Mobile.json", DisplayName = "COD_Mobile.json", IsSelected = true, Profile = MappingProfile.CreateCodMobileProfile() });
        AvailableProfiles.Add(new GameProfileItem { Name = "Delta_Force.json", DisplayName = "Delta_Force.json", IsSelected = false, Profile = MappingProfile.CreateDeltaForceProfile() });
        AvailableProfiles.Add(new GameProfileItem { Name = "Default_Ucom.json", DisplayName = "Default_Ucom.json", IsSelected = false, Profile = MappingProfile.CreateUcomProfile() });

        SelectProfile("COD_Mobile.json");
    }

    private void HookEngineEvents()
    {
        _engine.ControllerConnected += (s, info) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                UpdateControllerList();
                ControllerName = info.Name;
                ControllerStatus = "Controller Connected";
                IsControllerConnected = true;
            });
        };

        _engine.ControllerDisconnected += (s, id) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                UpdateControllerList();
                if (ConnectedControllersList.Count == 0)
                {
                    ControllerName = "No Controller Detected";
                    ControllerStatus = "Waiting for Gamepad...";
                    IsControllerConnected = false;
                }
            });
        };

        _engine.StateUpdated += (s, state) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                // Handle interactive listening & wizard step advance
                if (IsListeningForBinding && ActiveListeningButton.HasValue)
                {
                    var now = DateTime.UtcNow;
                    if ((now - _lastButtonPressHandled).TotalMilliseconds > 300)
                    {
                        var pressedList = state.GetPressedButtons();
                        if (pressedList.Count > 0)
                        {
                            var physicalPressed = pressedList.First();
                            _lastButtonPressHandled = now;

                            // Apply mapping in active profile
                            _engine.ActiveProfile.RemapButton(ActiveListeningButton.Value, physicalPressed);
                            LastRemapSuccessMessage = $"Mapped '{ActiveListeningButton.Value}' to '{physicalPressed}'";

                            if (IsWizardActive)
                            {
                                AdvanceWizard();
                            }
                            else
                            {
                                // Single button mapping finished
                                IsListeningForBinding = false;
                                ActiveListeningButton = null;
                            }
                        }
                    }
                }

                // Update stick values
                LeftStickX = state.LeftStickX.NormalizedValue;
                LeftStickY = state.LeftStickY.NormalizedValue;
                RightStickX = state.RightStickX.NormalizedValue;
                RightStickY = state.RightStickY.NormalizedValue;

                LeftStickPositionText = $"({LeftStickX:F2}, {LeftStickY:F2})";
                RightStickPositionText = $"({RightStickX:F2}, {RightStickY:F2})";

                // Radar coordinate calculation (Center 65, radius 45)
                LeftStickDotLeft = 65 + (LeftStickX * 45) - 6;
                LeftStickDotTop = 65 + (LeftStickY * 45) - 6;
                RightStickDotLeft = 65 + (RightStickX * 45) - 6;
                RightStickDotTop = 65 + (RightStickY * 45) - 6;

                // Visual stick displacements on the controller gamepad model
                LeftStickVisualOffsetX = Math.Clamp(LeftStickX * 12.0, -12.0, 12.0);
                LeftStickVisualOffsetY = Math.Clamp(LeftStickY * 12.0, -12.0, 12.0);
                RightStickVisualOffsetX = Math.Clamp(RightStickX * 12.0, -12.0, 12.0);
                RightStickVisualOffsetY = Math.Clamp(RightStickY * 12.0, -12.0, 12.0);

                LeftTrigger = state.LeftTrigger;
                RightTrigger = state.RightTrigger;
                LeftTriggerPercent = (int)Math.Clamp(Math.Round(LeftTrigger * 100f), 0, 100);
                RightTriggerPercent = (int)Math.Clamp(Math.Round(RightTrigger * 100f), 0, 100);

                DPadX = state.DPadX;
                DPadY = state.DPadY;

                // Button Glows & Pressed States
                IsButtonPressed_A = state.GetButtonState(ControllerButton.A) == ButtonState.Pressed;
                IsButtonPressed_B = state.GetButtonState(ControllerButton.B) == ButtonState.Pressed;
                IsButtonPressed_X = state.GetButtonState(ControllerButton.X) == ButtonState.Pressed;
                IsButtonPressed_Y = state.GetButtonState(ControllerButton.Y) == ButtonState.Pressed;
                IsButtonPressed_LB = state.GetButtonState(ControllerButton.LB) == ButtonState.Pressed;
                IsButtonPressed_RB = state.GetButtonState(ControllerButton.RB) == ButtonState.Pressed;
                IsButtonPressed_LT = state.LeftTrigger > 0.1f || state.GetButtonState(ControllerButton.LT_Button) == ButtonState.Pressed;
                IsButtonPressed_RT = state.RightTrigger > 0.1f || state.GetButtonState(ControllerButton.RT_Button) == ButtonState.Pressed;
                IsButtonPressed_LS = state.GetButtonState(ControllerButton.LS) == ButtonState.Pressed;
                IsButtonPressed_RS = state.GetButtonState(ControllerButton.RS) == ButtonState.Pressed;
                IsButtonPressed_Start = state.GetButtonState(ControllerButton.Start) == ButtonState.Pressed;
                IsButtonPressed_Back = state.GetButtonState(ControllerButton.Back) == ButtonState.Pressed;
                IsButtonPressed_Guide = state.GetButtonState(ControllerButton.Guide) == ButtonState.Pressed;

                IsButtonPressed_DPadUp = state.DPadY > 0 || state.GetButtonState(ControllerButton.DPadUp) == ButtonState.Pressed;
                IsButtonPressed_DPadDown = state.DPadY < 0 || state.GetButtonState(ControllerButton.DPadDown) == ButtonState.Pressed;
                IsButtonPressed_DPadLeft = state.DPadX < 0 || state.GetButtonState(ControllerButton.DPadLeft) == ButtonState.Pressed;
                IsButtonPressed_DPadRight = state.DPadX > 0 || state.GetButtonState(ControllerButton.DPadRight) == ButtonState.Pressed;

                // Live Input Monitor Summary
                var activeList = new List<string>();
                if (IsButtonPressed_A) activeList.Add("A");
                if (IsButtonPressed_B) activeList.Add("B");
                if (IsButtonPressed_X) activeList.Add("X");
                if (IsButtonPressed_Y) activeList.Add("Y");
                if (IsButtonPressed_LB) activeList.Add("LB");
                if (IsButtonPressed_RB) activeList.Add("RB");
                if (LeftTriggerPercent > 5) activeList.Add($"LT: {LeftTriggerPercent}%");
                if (RightTriggerPercent > 5) activeList.Add($"RT: {RightTriggerPercent}%");
                if (IsButtonPressed_DPadUp) activeList.Add("D-Pad ▲");
                if (IsButtonPressed_DPadDown) activeList.Add("D-Pad ▼");
                if (IsButtonPressed_DPadLeft) activeList.Add("D-Pad ◀");
                if (IsButtonPressed_DPadRight) activeList.Add("D-Pad ▶");
                if (IsButtonPressed_LS) activeList.Add("LS Click");
                if (IsButtonPressed_RS) activeList.Add("RS Click");
                if (IsButtonPressed_Start) activeList.Add("Start");
                if (IsButtonPressed_Back) activeList.Add("Back");
                if (IsButtonPressed_Guide) activeList.Add("Guide");
                if (Math.Abs(LeftStickX) > 0.2f || Math.Abs(LeftStickY) > 0.2f) activeList.Add($"LS {LeftStickPositionText}");
                if (Math.Abs(RightStickX) > 0.2f || Math.Abs(RightStickY) > 0.2f) activeList.Add($"RS {RightStickPositionText}");

                HasActiveInputs = activeList.Count > 0;
                ActivePressedButtonsSummary = activeList.Count > 0 ? string.Join(" • ", activeList) : "Ready / Idle";

                PacketsPerSecond = _engine.Metrics.PacketsPerSecond > 0 ? _engine.Metrics.PacketsPerSecond : 120.0;
                TotalPacketsSent = _engine.Metrics.TotalPacketsSent;
                PacketLossPercent = _engine.Metrics.PacketLossPercent;
                LatencyMs = 2.1;
            });
        };

        _engine.OperatingModeChanged += (s, mode) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                UpdateModeProperties(mode);
            });
        };
    }

    private void UpdateControllerList()
    {
        ConnectedControllersList.Clear();
        var controllers = _engine.BackendManager.GetConnectedControllers();
        foreach (var c in controllers)
        {
            ConnectedControllersList.Add($"{c.Name} ({c.Id})");
        }

        if (controllers.Count > 0)
        {
            var first = controllers[0];
            ControllerName = first.Name;
            ControllerStatus = "Controller Connected";
            IsControllerConnected = true;
        }
    }

    /// <summary>
    /// Starts single button remapping when clicking a specific button on screen.
    /// </summary>
    [RelayCommand]
    public void StartListeningForButton(string buttonName)
    {
        if (Enum.TryParse<ControllerButton>(buttonName, out var targetButton))
        {
            IsWizardActive = false;
            SetupListeningForButton(targetButton);
        }
    }

    /// <summary>
    /// Starts the full automated step-by-step controller setup wizard.
    /// Asks button by button without requiring mouse interaction!
    /// </summary>
    [RelayCommand]
    public void StartFullMappingWizard()
    {
        IsWizardActive = true;
        WizardCurrentStep = 0;
        WizardTotalSteps = _wizardSequence.Length;
        SetWizardButtonStep(WizardCurrentStep);
    }

    private void SetWizardButtonStep(int stepIndex)
    {
        if (stepIndex < _wizardSequence.Length)
        {
            var targetBtn = _wizardSequence[stepIndex];
            WizardProgressText = $"Step {stepIndex + 1}/{WizardTotalSteps}";
            SetupListeningForButton(targetBtn, isWizard: true);
        }
        else
        {
            // All buttons configured!
            IsWizardActive = false;
            IsListeningForBinding = false;
            ActiveListeningButton = null;
            LastRemapSuccessMessage = "✓ Full Controller Mapping Complete!";
        }
    }

    private void AdvanceWizard()
    {
        WizardCurrentStep++;
        if (WizardCurrentStep < _wizardSequence.Length)
        {
            SetWizardButtonStep(WizardCurrentStep);
        }
        else
        {
            IsWizardActive = false;
            IsListeningForBinding = false;
            ActiveListeningButton = null;
            LastRemapSuccessMessage = "✓ Full Controller Mapping Complete!";
        }
    }

    private void SetupListeningForButton(ControllerButton targetButton, bool isWizard = false)
    {
        ActiveListeningButton = targetButton;
        ListeningTargetName = GetFriendlyButtonName(targetButton);
        ListeningInstruction = isWizard 
            ? $"[{WizardProgressText}] Press {ListeningTargetName} on your controller." 
            : $"Press {ListeningTargetName} on your controller.";

        CalculateBubbleCoordinates(targetButton);
        IsListeningForBinding = true;
    }

    private string GetFriendlyButtonName(ControllerButton btn) => btn switch
    {
        ControllerButton.LT_Button => "LT (Left Trigger)",
        ControllerButton.RT_Button => "RT (Right Trigger)",
        ControllerButton.DPadUp => "D-Pad Up ▲",
        ControllerButton.DPadDown => "D-Pad Down ▼",
        ControllerButton.DPadLeft => "D-Pad Left ◀",
        ControllerButton.DPadRight => "D-Pad Right ▶",
        ControllerButton.LS => "Left Stick Click (LS)",
        ControllerButton.RS => "Right Stick Click (RS)",
        ControllerButton.Back => "Back / Select ⧉",
        ControllerButton.Start => "Start / Menu ☰",
        _ => btn.ToString()
    };

    private void CalculateBubbleCoordinates(ControllerButton btn)
    {
        switch (btn)
        {
            case ControllerButton.A:
                BubbleAnchorX = 405; BubbleAnchorY = 152;
                BubbleLineEndX = 480; BubbleLineEndY = 205;
                BubbleLeft = 470; BubbleTop = 185;
                break;
            case ControllerButton.B:
                BubbleAnchorX = 425; BubbleAnchorY = 112;
                BubbleLineEndX = 480; BubbleLineEndY = 150;
                BubbleLeft = 470; BubbleTop = 130;
                break;
            case ControllerButton.X:
                BubbleAnchorX = 365; BubbleAnchorY = 112;
                BubbleLineEndX = 450; BubbleLineEndY = 120;
                BubbleLeft = 440; BubbleTop = 100;
                break;
            case ControllerButton.Y:
                BubbleAnchorX = 395; BubbleAnchorY = 85;
                BubbleLineEndX = 470; BubbleLineEndY = 80;
                BubbleLeft = 460; BubbleTop = 60;
                break;
            case ControllerButton.LB:
                BubbleAnchorX = 100; BubbleAnchorY = 25;
                BubbleLineEndX = 100; BubbleLineEndY = 70;
                BubbleLeft = 50; BubbleTop = 70;
                break;
            case ControllerButton.RB:
                BubbleAnchorX = 440; BubbleAnchorY = 25;
                BubbleLineEndX = 480; BubbleLineEndY = 60;
                BubbleLeft = 470; BubbleTop = 40;
                break;
            case ControllerButton.LT_Button:
                BubbleAnchorX = 80; BubbleAnchorY = 15;
                BubbleLineEndX = 80; BubbleLineEndY = 60;
                BubbleLeft = 40; BubbleTop = 60;
                break;
            case ControllerButton.RT_Button:
                BubbleAnchorX = 460; BubbleAnchorY = 15;
                BubbleLineEndX = 490; BubbleLineEndY = 50;
                BubbleLeft = 480; BubbleTop = 30;
                break;
            case ControllerButton.DPadUp:
            case ControllerButton.DPadDown:
            case ControllerButton.DPadLeft:
            case ControllerButton.DPadRight:
                BubbleAnchorX = 190; BubbleAnchorY = 175;
                BubbleLineEndX = 120; BubbleLineEndY = 230;
                BubbleLeft = 60; BubbleTop = 220;
                break;
            case ControllerButton.LS:
                BubbleAnchorX = 140; BubbleAnchorY = 110;
                BubbleLineEndX = 80; BubbleLineEndY = 160;
                BubbleLeft = 40; BubbleTop = 150;
                break;
            case ControllerButton.RS:
                BubbleAnchorX = 270; BubbleAnchorY = 170;
                BubbleLineEndX = 270; BubbleLineEndY = 230;
                BubbleLeft = 210; BubbleTop = 230;
                break;
            case ControllerButton.Back:
                BubbleAnchorX = 227; BubbleAnchorY = 110;
                BubbleLineEndX = 180; BubbleLineEndY = 60;
                BubbleLeft = 110; BubbleTop = 50;
                break;
            case ControllerButton.Start:
                BubbleAnchorX = 313; BubbleAnchorY = 110;
                BubbleLineEndX = 370; BubbleLineEndY = 60;
                BubbleLeft = 360; BubbleTop = 50;
                break;
            default:
                BubbleAnchorX = 405; BubbleAnchorY = 152;
                BubbleLineEndX = 480; BubbleLineEndY = 205;
                BubbleLeft = 470; BubbleTop = 185;
                break;
        }

        BubbleStartPoint = new Point(BubbleAnchorX + 7, BubbleAnchorY + 7);
        BubbleEndPoint = new Point(BubbleLineEndX, BubbleLineEndY);
    }

    [RelayCommand]
    public void CancelListening()
    {
        IsListeningForBinding = false;
        IsWizardActive = false;
        ActiveListeningButton = null;
        ListeningInstruction = "";
    }

    [RelayCommand]
    public void OpenProfileDropdown()
    {
        IsProfileDropdownOpen = !IsProfileDropdownOpen;
    }

    [RelayCommand]
    public void SelectProfile(string profileFileName)
    {
        SelectedProfileName = profileFileName;
        foreach (var p in AvailableProfiles)
        {
            p.IsSelected = (p.Name == profileFileName);
        }

        var selected = AvailableProfiles.FirstOrDefault(p => p.Name == profileFileName);
        if (selected != null)
        {
            _engine.ActiveProfile = selected.Profile;
            Deadzone = selected.Profile.GlobalDeadzone;
            Sensitivity = selected.Profile.GlobalSensitivity;
        }
    }

    [RelayCommand]
    public void ResetToDefaults()
    {
        Deadzone = 0.15;
        Sensitivity = 1.35;
        _engine.ActiveProfile.GlobalDeadzone = (float)Deadzone;
        _engine.ActiveProfile.GlobalSensitivity = (float)Sensitivity;
    }

    partial void OnDeadzoneChanged(double value)
    {
        if (_engine?.ActiveProfile != null)
        {
            _engine.ActiveProfile.GlobalDeadzone = (float)value;
        }
    }

    partial void OnSensitivityChanged(double value)
    {
        if (_engine?.ActiveProfile != null)
        {
            _engine.ActiveProfile.GlobalSensitivity = (float)value;
        }
    }

    [RelayCommand]
    public async Task ToggleSimulationMode()
    {
        IsSimulationMode = !IsSimulationMode;
        await _engine.BackendManager.SetSimulationModeAsync(IsSimulationMode);
        UpdateControllerList();
    }

    [RelayCommand]
    public async Task ToggleOperatingMode()
    {
        var newMode = IsXboxEmulationMode ? BridgeOperatingMode.NetworkBridge : BridgeOperatingMode.XboxEmulation;
        await _engine.SetOperatingModeAsync(newMode);
        UpdateModeProperties(newMode);
    }

    [RelayCommand]
    public async Task SetOperatingMode(string modeName)
    {
        var targetMode = modeName.Equals("Xbox", StringComparison.OrdinalIgnoreCase) 
            ? BridgeOperatingMode.XboxEmulation 
            : BridgeOperatingMode.NetworkBridge;
        await _engine.SetOperatingModeAsync(targetMode);
        UpdateModeProperties(targetMode);
    }

    [RelayCommand]
    public void SelectTab(string tabName)
    {
        SelectedTab = tabName;
        IsControllerTabSelected = tabName.Equals("Controller", StringComparison.OrdinalIgnoreCase);
        IsGuideTabSelected = tabName.Equals("Guide", StringComparison.OrdinalIgnoreCase);
        IsMappingTabSelected = tabName.Equals("Mapping", StringComparison.OrdinalIgnoreCase);
        IsProfilesTabSelected = tabName.Equals("Profiles", StringComparison.OrdinalIgnoreCase);
        IsSettingsTabSelected = tabName.Equals("Settings", StringComparison.OrdinalIgnoreCase);

        if (IsMappingTabSelected)
        {
            StartFullMappingWizard();
        }
    }

    [RelayCommand]
    public void SelectGuideSection(string section)
    {
        GuideActiveSection = section;
    }
}

public partial class GameProfileItem : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _displayName = "";
    [ObservableProperty] private bool _isSelected;
    public MappingProfile Profile { get; set; } = null!;
}
