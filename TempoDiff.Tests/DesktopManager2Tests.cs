using System;
using Tempo;

namespace TempoDiff.Tests
{
    public class DesktopManager2Tests
    {
        [Fact]
        public void LoadWindowsTypes_ReturnsSomething()
        {
            DesktopManager2.Initialize(wpfApp: false, packagesCachePath: @"c:\temp\tests");

            var result = Tempo.DesktopManager2.LoadWindowsTypes(useWinRTProjections: false);

            Assert.NotNull(result);

            // 25h2, 26220.7752
            Assert.True(result.AssemblyLocations.Count >= 0x14);
            Assert.True(result.Types.Count >= 0x3959);
        }
    }
}
