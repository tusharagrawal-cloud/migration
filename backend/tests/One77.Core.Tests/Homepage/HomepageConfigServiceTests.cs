using System.IO;
using System.Threading.Tasks;
using One77.Core.Homepage;
using Xunit;
using Xunit.Abstractions;

namespace One77.Core.Tests.Homepage
{
    public class HomepageConfigServiceTests : HomepageTestBase
    {
        private readonly ITestOutputHelper _output;
        public HomepageConfigServiceTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public async Task GetAsync_WithNoImageEverSet_ReturnsNullHeroPath()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }
            await ResetSingletonRowAsync();

            var service = CreateService();
            var config = await service.GetAsync();

            Assert.Equal(1, config.Id);
            Assert.Null(config.HeroImagePath);
        }

        [Fact]
        public async Task SetHeroImageAsync_ValidImage_PersistsPathAndWritesFile()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }
            await ResetSingletonRowAsync();

            var service = CreateService();
            try
            {
                var config = await service.SetHeroImageAsync(MinimalJpegBytes());

                Assert.NotNull(config.HeroImagePath);
                Assert.StartsWith("/media/hero-", config.HeroImagePath);
                Assert.True(File.Exists(Path.Combine(MediaRoot, Path.GetFileName(config.HeroImagePath))));

                var reread = await service.GetAsync();
                Assert.Equal(config.HeroImagePath, reread.HeroImagePath);
            }
            finally
            {
                await ResetSingletonRowAsync();
            }
        }

        [Fact]
        public async Task SetHeroImageAsync_InvalidFile_RejectedAndConfigUnchanged()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }
            await ResetSingletonRowAsync();

            var service = CreateService();
            await Assert.ThrowsAsync<HomepageValidationException>(() => service.SetHeroImageAsync(NotAnImageBytes()));

            var config = await service.GetAsync();
            Assert.Null(config.HeroImagePath);
        }

        [Fact]
        public async Task SetHeroImageAsync_Replace_DeletesThePreviousFile()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }
            await ResetSingletonRowAsync();

            var service = CreateService();
            try
            {
                var first = await service.SetHeroImageAsync(MinimalJpegBytes());
                var firstPhysicalPath = Path.Combine(MediaRoot, Path.GetFileName(first.HeroImagePath!));
                Assert.True(File.Exists(firstPhysicalPath));

                var second = await service.SetHeroImageAsync(MinimalPngBytes());

                Assert.NotEqual(first.HeroImagePath, second.HeroImagePath);
                Assert.False(File.Exists(firstPhysicalPath), "Replacing the hero image must delete the abandoned file, not accumulate it.");
                Assert.True(File.Exists(Path.Combine(MediaRoot, Path.GetFileName(second.HeroImagePath!))));
            }
            finally
            {
                await ResetSingletonRowAsync();
            }
        }

        [Fact]
        public async Task RemoveHeroImageAsync_ClearsPathAndDeletesFile_RestoringFallbackState()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }
            await ResetSingletonRowAsync();

            var service = CreateService();
            var set = await service.SetHeroImageAsync(MinimalJpegBytes());
            var physicalPath = Path.Combine(MediaRoot, Path.GetFileName(set.HeroImagePath!));
            Assert.True(File.Exists(physicalPath));

            var removed = await service.RemoveHeroImageAsync();

            Assert.Null(removed.HeroImagePath);
            Assert.False(File.Exists(physicalPath));

            var reread = await service.GetAsync();
            Assert.Null(reread.HeroImagePath); // the storefront's own fallback hero takes over on a null path — frontend-owned, not asserted here
        }

        [Fact]
        public async Task RemoveHeroImageAsync_WhenAlreadyEmpty_IsSafeNoOp()
        {
            if (!HasDatabase) { _output.WriteLine("SKIPPED: no ONE77_TEST_CONNECTION_STRING"); return; }
            await ResetSingletonRowAsync();

            var service = CreateService();
            var removed = await service.RemoveHeroImageAsync();

            Assert.Null(removed.HeroImagePath);
        }
    }
}
