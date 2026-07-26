using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class StudioProgramCompletionsTests
{
  private static CalcExplorerSession CreateHp65Session()
  {
    CalcExplorerSession session = new(TeoCalcPaths.ResourcePath("Engine"));
    int idx = Array.FindIndex(session.Models, id => id.Contains("65", StringComparison.Ordinal));
    Assert.IsTrue(idx >= 0, "HP-65 / T-65 model missing");
    session.LoadModel(idx);
    session.PowerOnResume();
    return session;
  }

  [TestMethod]
  public void ExtractToken_FindsTokenUnderCursor()
  {
    const string text = "LBL\nRC";
    Assert.IsTrue(StudioProgramCompletions.TryExtractToken(text, text.Length, out var token));
    Assert.AreEqual("RC", token.Text);
    Assert.AreEqual(4, token.Start);
  }

  [TestMethod]
  public void ExtractToken_OnWhitespace_UsesPriorToken()
  {
    const string text = "ENTER ";
    Assert.IsTrue(StudioProgramCompletions.TryExtractToken(text, text.Length, out var token));
    Assert.AreEqual("ENTER", token.Text);
  }

  [TestMethod]
  public void Filter_EmptyPrefix_ReturnsNone()
  {
    Assert.AreEqual(0, StudioProgramCompletions.Filter(["LBL", "LBL A"], "").Count);
  }

  [TestMethod]
  public void Filter_Lb_IncludesLbl()
  {
    using CalcExplorerSession session = CreateHp65Session();
    IReadOnlyList<string> mnemonics = session.EnumerateProgramMnemonics();
    Assert.IsTrue(mnemonics.Count > 0);
    IReadOnlyList<string> matches = StudioProgramCompletions.Filter(mnemonics, "LB");
    Assert.IsTrue(
      matches.Any(m => m.StartsWith("LBL", StringComparison.OrdinalIgnoreCase)),
      string.Join(", ", matches));
  }

  [TestMethod]
  public void ReplaceToken_SubstitutesSpan()
  {
    const string text = "g\nRC";
    Assert.IsTrue(StudioProgramCompletions.TryExtractToken(text, text.Length, out var token));
    string replaced = StudioProgramCompletions.ReplaceToken(text, token, "RCL 1");
    Assert.AreEqual("g\nRCL 1", replaced);
  }

  [TestMethod]
  public void EnumerateMachineTokens_IncludesMuseumPrefix()
  {
    using CalcExplorerSession session = CreateHp65Session();
    IReadOnlyList<string> tokens = session.EnumerateMachineCompletionTokens();
    Assert.IsTrue(tokens.Count > 0);
    Assert.IsTrue(
      tokens.Any(t => t.StartsWith('2') || t.StartsWith('3')),
      string.Join(", ", tokens.Take(20)));
  }
}
