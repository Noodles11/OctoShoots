using Godot;

namespace OctoShoots.Game.Controls;

/// <summary>
/// A controller in the menus, the same way the keyboard works them. While a control has focus, A, the D-pad and the left
/// stick become Godot's own ui actions (ui_accept, ui_up/down/left/right), so buttons press, sliders and spin boxes
/// change, check boxes toggle and focus moves in all four directions, through exactly the path the keyboard takes.
/// (Relying on the pad events reaching the GUI directly left only up/down working.) B is left to the screens (it goes
/// back). Nothing happens without a focused control, so play is never touched.
/// </summary>
public partial class PadMenus : Node
{
	/// <summary>Settings is capturing a new binding: the pad is left alone.</summary>
	public static bool Capturing { get; set; }

	/// <summary>The left stick moves focus once per push (pushed past Press, re-armed below Release).</summary>
	const float Press = 0.6f, Release = 0.3f;
	bool _stickX, _stickY;

	public override void _Ready() => ProcessMode = ProcessModeEnum.Always;

	public override void _Input(InputEvent e)
	{
		if (Capturing) return;
		if (e is not (InputEventJoypadButton or InputEventJoypadMotion)) return;
		var focus = GetViewport().GuiGetFocusOwner();
		if (focus is null || !focus.IsVisibleInTree()) return;

		switch (e)
		{
			case InputEventJoypadButton b:
				string? action = b.ButtonIndex switch
				{
					JoyButton.A => "ui_accept",
					JoyButton.DpadUp => "ui_up",
					JoyButton.DpadDown => "ui_down",
					JoyButton.DpadLeft => "ui_left",
					JoyButton.DpadRight => "ui_right",
					_ => null,
				};
				if (action is null) return;
				Send(action, b.Pressed);
				GetViewport().SetInputAsHandled();
				break;
			case InputEventJoypadMotion m when m.Axis is JoyAxis.LeftX or JoyAxis.LeftY:
				bool x = m.Axis == JoyAxis.LeftX;
				float v = m.AxisValue;
				ref bool held = ref x ? ref _stickX : ref _stickY;
				if (!held && Mathf.Abs(v) > Press)
				{
					held = true;
					string dir = x ? (v < 0f ? "ui_left" : "ui_right") : (v < 0f ? "ui_up" : "ui_down");
					Send(dir, true);
					Send(dir, false);
				}
				else if (held && Mathf.Abs(v) < Release) held = false;
				GetViewport().SetInputAsHandled();
				break;
		}
	}

	/// <summary>Sent after this event is done with, so the GUI takes it as a fresh event of its own.</summary>
	static void Send(string action, bool pressed) =>
		Callable.From(() => Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = pressed, Strength = pressed ? 1f : 0f })).CallDeferred();
}
