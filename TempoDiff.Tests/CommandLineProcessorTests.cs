using Tempo;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tempo.Tests
{
    [TestClass]
    public class CommandLineProcessorTests
    {
        [TestMethod]
        public void NormalizedCommandArgs_SkipsExeName()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "/help" });
            Assert.AreEqual(1, processor.NormalizedCommandArgs.Length);
        }

        [TestMethod]
        public void NormalizedCommandArgs_NormalizesDoubleDash()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "--help" });
            Assert.AreEqual("/help", processor.NormalizedCommandArgs[0]);
        }

        [TestMethod]
        public void NormalizedCommandArgs_NormalizesSingleDash()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "-diff" });
            Assert.AreEqual("/diff", processor.NormalizedCommandArgs[0]);
        }

        [TestMethod]
        public void NormalizedCommandArgs_NormalizesSlash()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "/Diff" });
            Assert.AreEqual("/diff", processor.NormalizedCommandArgs[0]);
        }

        [TestMethod]
        public void NormalizedCommandArgs_PreservesCaseForFilenames()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "MyFile.dll" });
            Assert.AreEqual("MyFile.dll", processor.NormalizedCommandArgs[0]);
        }

        [TestMethod]
        public void NormalizedCommandArgs_EmptyArgs_ReturnsEmpty()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe" });
            Assert.AreEqual(0, processor.NormalizedCommandArgs.Length);
        }

        [TestMethod]
        public void NormalizedCommandArgs_NullArgs_ReturnsEmpty()
        {
            var processor = new CommandLineProcessor(null);
            Assert.AreEqual(0, processor.NormalizedCommandArgs.Length);
        }

        [TestMethod]
        public void NeedHelp_TrueForQuestionMark()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "/?" });
            Assert.IsTrue(processor.NeedHelp);
        }

        [TestMethod]
        public void NeedHelp_TrueForHelp()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "--help" });
            Assert.IsTrue(processor.NeedHelp);
        }

        [TestMethod]
        public void NeedHelp_FalseWhenNotPresent()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "file.dll" });
            Assert.IsFalse(processor.NeedHelp);
        }

        [TestMethod]
        public void NeedWaitForDebugger_True()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "/waitfordebugger" });
            Assert.IsTrue(processor.NeedWaitForDebugger);
        }

        [TestMethod]
        public void NeedWaitForDebugger_FalseWhenNotPresent()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "file.dll" });
            Assert.IsFalse(processor.NeedWaitForDebugger);
        }

        [TestMethod]
        public void ShouldAllowSingleInstance_True()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "/singleinstance" });
            Assert.IsTrue(processor.ShouldAllowSingleInstance);
        }

        [TestMethod]
        public void ShouldAllowSingleInstance_FalseWhenNotPresent()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "file.dll" });
            Assert.IsFalse(processor.ShouldAllowSingleInstance);
        }

        [TestMethod]
        public void PSFilenames_ReturnsFilesAfterPsFlag()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "/ps", "a.dll", "b.dll" });
            var filenames = processor.PSFilenames;
            Assert.IsNotNull(filenames);
            Assert.AreEqual(2, filenames.Count);
        }

        [TestMethod]
        public void PSFilenames_NullWhenNoPsFlag()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "file.dll" });
            Assert.IsNull(processor.PSFilenames);
        }

        [TestMethod]
        public void PSFilenames_StopsAtNextFlag()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "/ps", "a.dll", "/singleinstance" });
            var filenames = processor.PSFilenames;
            Assert.IsNotNull(filenames);
            Assert.AreEqual(1, filenames.Count);
        }

        [TestMethod]
        public void Diff_ValidDiffParsesCorrectly()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "/diff", "baseline.dll", "new.dll" });
            Assert.IsTrue(processor.IsDiffMode);
            Assert.IsNotNull(processor.DiffBaselineFilename);
            Assert.IsTrue(processor.DiffBaselineFilename.EndsWith("baseline.dll"));
            Assert.IsNotNull(processor.CustomFilenames);
            Assert.AreEqual(1, processor.CustomFilenames.Count);
            Assert.IsTrue(processor.CustomFilenames[0].EndsWith("new.dll"));
            Assert.IsFalse(processor.HasIncompleteDiff);
        }

        [TestMethod]
        public void Diff_IncompleteDiff_MissingBothFiles()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "/diff" });
            Assert.IsTrue(processor.HasIncompleteDiff);
            Assert.IsFalse(processor.IsDiffMode);
        }

        [TestMethod]
        public void Diff_IncompleteDiff_MissingSecondFile()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "/diff", "baseline.dll" });
            Assert.IsTrue(processor.HasIncompleteDiff);
            Assert.IsFalse(processor.IsDiffMode);
        }

        [TestMethod]
        public void CustomFilenames_BareFilenames()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "one.dll", "two.winmd" });
            Assert.IsNotNull(processor.CustomFilenames);
            Assert.AreEqual(2, processor.CustomFilenames.Count);
            Assert.IsTrue(processor.CustomFilenames[0].EndsWith("one.dll"));
            Assert.IsTrue(processor.CustomFilenames[1].EndsWith("two.winmd"));
        }

        [TestMethod]
        public void CustomFilenames_NullWhenNoFilenames()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "/singleinstance" });
            Assert.IsNull(processor.CustomFilenames);
        }

        [TestMethod]
        public void CustomFilenames_SkipsKnownFlags()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "/waitfordebugger", "/singleinstance", "file.dll" });
            Assert.IsNotNull(processor.CustomFilenames);
            Assert.AreEqual(1, processor.CustomFilenames.Count);
            Assert.IsTrue(processor.CustomFilenames[0].EndsWith("file.dll"));
        }

        [TestMethod]
        public void HasCommandLineFlag_CaseInsensitive()
        {
            var processor = new CommandLineProcessor(new[] { "tempo.exe", "/WaitForDebugger" });
            Assert.IsTrue(processor.HasCommandLineFlag("waitfordebugger"));
            Assert.IsTrue(processor.HasCommandLineFlag("WAITFORDEBUGGER"));
        }
    }
}
