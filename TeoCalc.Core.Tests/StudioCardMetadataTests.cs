using TeoCalc.Formats;
using TeoCalc.Rendering;
using TeoCalc.Rendering.Faceplate;

namespace TeoCalc.Core.Tests;

[TestClass]
public sealed class StudioCardMetadataTests
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
  public void ApplyCardMetadata_MarksDirty_AndSaveRoundTripsFields()
  {
    using CalcExplorerSession session = CreateHp65Session();
    string sample = Path.Combine(
      CalcCardPanelComponent.SampleCardsDirectory(),
      CalcCardPanelComponent.SampleHp65T65FileName);
    Assert.IsTrue(session.TryLoadCardProgram(sample, out string? loadError), loadError);

    CardMetadataFields fields = session.GetCardMetadataFields();
    fields.Title = "Studio Meta RoundTrip";
    fields.Category = "Tests";
    fields.Author = "TeoCalc";
    fields.Profile = "T-65-Print";
    fields.Description = "Description from Studio Card tab.";
    fields.Usage = "Usage from Studio.";
    fields.RunHint = "RUN: A";
    fields.CodeEncoding = CardCodeEncoding.Mnemonic;
    fields.Labels = ["Σ+", "mean", "", "", "undo"];
    fields.LabelHints = ["Accumulate", "Mean(x)", "", "", "Remove"];
    fields.Created = "1974-01-01T00:00:00Z";

    Assert.IsTrue(session.TryApplyCardMetadata(fields, out string? applyError), applyError);
    Assert.IsTrue(session.IsCardMetadataDirty);
    Assert.IsTrue(session.IsProgramDirty);
    Assert.AreEqual("Studio Meta RoundTrip", session.CardTitle);
    Assert.AreEqual("TeoCalc", session.CardAuthor);
    Assert.AreEqual("Σ+", session.CardStripLabels![0]);

    string path = Path.Combine(Path.GetTempPath(), $"teocalc-meta-{Guid.NewGuid():N}.t65");
    try
    {
      Assert.IsTrue(session.TrySaveCardProgram(path, out string? saveError), saveError);
      Assert.IsFalse(session.IsCardMetadataDirty);

      T6xDocument written = T6xCardFormat.ReadFile(path);
      Assert.AreEqual("Studio Meta RoundTrip", written.Title);
      Assert.AreEqual("Tests", written.Category);
      Assert.AreEqual("TeoCalc", written.Author);
      Assert.AreEqual("T-65-Print", written.Profile);
      Assert.AreEqual("Description from Studio Card tab.", written.Description);
      Assert.AreEqual("Usage from Studio.", written.Usage);
      Assert.AreEqual("RUN: A", written.RunHint);
      Assert.AreEqual(CardCodeEncoding.Mnemonic, written.CodeEncoding);
      Assert.AreEqual(
        new DateTimeOffset(1974, 1, 1, 0, 0, 0, TimeSpan.Zero),
        written.Created);
      T6xLabelEntry a = written.Labels.Single(l => l.Key == "A");
      Assert.AreEqual("Σ+", a.Caption);
      Assert.AreEqual("Accumulate", a.Hint);
      T6xLabelEntry b = written.Labels.Single(l => l.Key == "B");
      Assert.AreEqual("mean", b.Caption);
      Assert.AreEqual("Mean(x)", b.Hint);

      using CalcExplorerSession reload = CreateHp65Session();
      Assert.IsTrue(reload.TryLoadCardProgram(path, out string? reloadError), reloadError);
      Assert.AreEqual("Studio Meta RoundTrip", reload.CardTitle);
      Assert.AreEqual("TeoCalc", reload.CardAuthor);
      Assert.AreEqual("T-65-Print", reload.CardProfile);
      Assert.AreEqual("Description from Studio Card tab.", reload.CardDescription);
      Assert.AreEqual("Usage from Studio.", reload.CardUsage);
      Assert.AreEqual("RUN: A", reload.CardRunHint);
      Assert.AreEqual("Σ+", reload.CardStripLabels![0]);
      Assert.AreEqual("mean", reload.CardStripLabels[1]);
    }
    finally
    {
      if (File.Exists(path))
      {
        File.Delete(path);
      }
    }
  }

  [TestMethod]
  public void TeoCard_AuthorAndProfile_SurviveT6xRoundTrip()
  {
    TeoCardDocument teo = new()
    {
      Format = TeoCardDocument.FormatId,
      SchemaVersion = TeoCardDocument.CurrentSchemaVersion,
      Model = "T-65",
      Profile = "T-65-Custom",
      Author = "RoundTrip Author",
      Title = "Authored",
      Labels = ["A1", "", "", "", ""],
      LabelHints = ["hint-a", "", "", "", ""],
      Program = new TeoCardProgramSection
      {
        CodeEncoding = CardCodeEncoding.Mnemonic,
        Steps = ["1", "ENTER"],
      },
    };

    T6xDocument t6x = T6xCardFormat.FromTeoCardDocument(teo);
    Assert.AreEqual("RoundTrip Author", t6x.Author);
    Assert.AreEqual("T-65-Custom", t6x.Profile);

    TeoCardDocument back = T6xCardFormat.ToTeoCardDocument(t6x);
    Assert.AreEqual("RoundTrip Author", back.Author);
    Assert.AreEqual("T-65-Custom", back.Profile);
    Assert.AreEqual("hint-a", back.LabelHints[0]);
  }
}
