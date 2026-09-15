using System;
using System.IO;
using One77.Core.Homepage;
using Xunit;

namespace One77.Core.Tests.Homepage
{
    /// <summary>
    /// Pure local-disk tests — no database, no ONE77_TEST_CONNECTION_STRING
    /// gate needed. Each test gets its own fresh temp directory (see
    /// HomepageTestBase), so these always run.
    /// </summary>
    public class HomepageMediaStorageTests : HomepageTestBase
    {
        [Fact]
        public void SaveHeroImage_ValidJpeg_WritesFileAndReturnsMediaUrl()
        {
            var storage = CreateStorage();
            var url = storage.SaveHeroImage(MinimalJpegBytes());

            Assert.StartsWith("/media/hero-", url);
            Assert.EndsWith(".jpg", url);
            Assert.True(File.Exists(Path.Combine(MediaRoot, Path.GetFileName(url))));
        }

        [Fact]
        public void SaveHeroImage_ValidPng_WritesFileAndReturnsMediaUrl()
        {
            var storage = CreateStorage();
            var url = storage.SaveHeroImage(MinimalPngBytes());

            Assert.EndsWith(".png", url);
            Assert.True(File.Exists(Path.Combine(MediaRoot, Path.GetFileName(url))));
        }

        [Fact]
        public void SaveHeroImage_NotAnImage_Rejected()
        {
            var storage = CreateStorage();
            var ex = Assert.Throws<HomepageValidationException>(() => storage.SaveHeroImage(NotAnImageBytes()));
            Assert.Contains("JPEG, PNG, or WebP", ex.Message);
        }

        [Fact]
        public void SaveHeroImage_Empty_Rejected()
        {
            var storage = CreateStorage();
            Assert.Throws<HomepageValidationException>(() => storage.SaveHeroImage(Array.Empty<byte>()));
        }

        [Fact]
        public void SaveHeroImage_Null_Rejected()
        {
            var storage = CreateStorage();
            Assert.Throws<HomepageValidationException>(() => storage.SaveHeroImage(null!));
        }

        [Fact]
        public void SaveHeroImage_OversizedFile_Rejected()
        {
            var storage = CreateStorage(maxBytes: 10);
            var oversized = new byte[20];
            Array.Copy(MinimalJpegBytes(), oversized, MinimalJpegBytes().Length);

            var ex = Assert.Throws<HomepageValidationException>(() => storage.SaveHeroImage(oversized));
            Assert.Contains("too large", ex.Message);
        }

        [Fact]
        public void SaveHeroImage_NeverTrustsClientFileName_AlwaysGeneratesServerSideName()
        {
            var storage = CreateStorage();
            var url = storage.SaveHeroImage(MinimalJpegBytes());

            // No path traversal, no original name, no client-controlled
            // segment anywhere in the generated URL.
            Assert.DoesNotContain("..", url);
            Assert.Matches(@"^/media/hero-[0-9a-f]{32}\.jpg$", url);
        }

        [Fact]
        public void Delete_RemovesTheStoredFile()
        {
            var storage = CreateStorage();
            var url = storage.SaveHeroImage(MinimalJpegBytes());
            var physicalPath = Path.Combine(MediaRoot, Path.GetFileName(url));
            Assert.True(File.Exists(physicalPath));

            storage.Delete(url);

            Assert.False(File.Exists(physicalPath));
        }

        [Fact]
        public void Delete_MissingFile_IsSafeNoOp()
        {
            var storage = CreateStorage();
            storage.Delete("/media/hero-doesnotexist.jpg"); // must not throw
        }

        [Fact]
        public void Delete_NullOrEmptyPath_IsSafeNoOp()
        {
            var storage = CreateStorage();
            storage.Delete(null!);
            storage.Delete("");
        }

        [Fact]
        public void Delete_PathTraversalAttempt_NeverEscapesMediaRoot()
        {
            // A sentinel file just outside the media root — if Delete ever
            // escaped, this would be the file it destroyed.
            var sentinelDir = Directory.GetParent(MediaRoot)!.FullName;
            var sentinelPath = Path.Combine(sentinelDir, "one77-sentinel-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(sentinelPath, "must survive");
            try
            {
                var storage = CreateStorage();
                storage.Delete("../" + Path.GetFileName(sentinelPath));
                storage.Delete("/media/../../" + Path.GetFileName(sentinelPath));

                Assert.True(File.Exists(sentinelPath));
            }
            finally
            {
                File.Delete(sentinelPath);
            }
        }
    }
}
