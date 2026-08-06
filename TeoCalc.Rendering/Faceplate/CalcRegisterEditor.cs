using System.Globalization;
using System.Numerics;
using ImGuiNET;
using Teo.Surface.Dialogs;
using Teo.Surface.Immediate;

namespace TeoCalc.Rendering.Faceplate;

/// <summary>Modal editor for Classic DATA registers R1–R9 (OK/Enter, Cancel/Esc).</summary>
public static class CalcRegisterEditor
{
  private static bool s_open;
  private static bool s_requestOpen;
  private static readonly string[] s_fields = new string[10];
  private static int s_focusRegister = -1;

  public static bool IsOpen => s_open || s_requestOpen;

  public static void Open(IReadOnlyList<double> registers, int focusRegister = -1)
  {
    for (int i = 0; i < s_fields.Length; i++)
    {
      double v = i < registers.Count ? registers[i] : 0d;
      s_fields[i] = Format(v);
    }

    s_focusRegister = focusRegister;
    s_requestOpen = true;
  }

  public static bool Draw(out double[]? committed)
  {
    committed = null;
    if (s_requestOpen)
    {
      s_open = true;
      s_requestOpen = false;
      ImGui.OpenPopup("##studio-reg-editor");
    }

    if (!s_open)
    {
      return false;
    }

    CalcAppTheme.EnsureInitialized();
    bool open = s_open;
    ImGui.SetNextWindowSize(new Vector2(360f, 0f), ImGuiCond.Appearing);
    if (!ImGuiModalHost.Begin(
          "##studio-reg-editor",
          DialogStyles.DataRegistersTitle,
          CalcAppTheme.Current,
          ref open,
          minContentWidth: 320f))
    {
      s_open = open;
      return false;
    }

    bool apply = false;
    bool cancel = false;
    for (int r = 1; r <= 9; r++)
    {
      ImGui.SetNextItemWidth(160f);
      if (s_focusRegister == r)
      {
        ImGui.SetKeyboardFocusHere();
        s_focusRegister = -1;
      }

      ImGui.InputText($"R{r}", ref s_fields[r], 32);
    }

    ImGui.Spacing();
    if (ImGuiModalHost.OkButton(new Vector2(90f, 0f)))
    {
      apply = true;
    }

    ImGui.SameLine();
    if (ImGuiModalHost.CancelButton(new Vector2(90f, 0f)))
    {
      cancel = true;
    }

    if (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter))
    {
      apply = true;
    }

    if (apply)
    {
      double[] regs = new double[10];
      for (int i = 0; i < regs.Length; i++)
      {
        if (!double.TryParse(
              s_fields[i],
              NumberStyles.Float,
              CultureInfo.InvariantCulture,
              out regs[i]))
        {
          _ = double.TryParse(s_fields[i], NumberStyles.Float, CultureInfo.CurrentCulture, out regs[i]);
        }
      }

      committed = regs;
      open = false;
      ImGui.CloseCurrentPopup();
    }
    else if (cancel)
    {
      open = false;
      ImGui.CloseCurrentPopup();
    }

    ImGuiModalHost.End();
    s_open = open;
    return committed is not null;
  }

  private static string Format(double value) =>
    value.ToString("G15", CultureInfo.InvariantCulture);
}
