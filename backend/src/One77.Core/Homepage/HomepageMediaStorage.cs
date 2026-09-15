using System;
using System.IO;

namespace One77.Core.Homepage
{
    /// <summary>
    /// Saves/deletes the homepage hero photograph on the local file system.
    /// Deliberately synchronous — a single write of a small (a few MB) file
    /// to local disk on an infrequent admin action does not need async I/O,
    /// and netstandard2.0 has no async File.WriteAllBytes overload anyway.
    ///
    /// Filenames are always server-generated (a GUID plus an extension
    /// detected from the file's own bytes, never from the client), so path
    /// traversal via the upload path is structurally impossible; deletion
    /// additionally strips to a bare file name before touching disk, so even
    /// a tampered stored path could not escape the media root.
    ///
    /// No image resizing/compression here by design — that would require a
    /// substantial new dependency (System.Drawing.Common / ImageSharp) for a
    /// V1 feature the CSS `object-fit: cover` treatment already handles
    /// safely for reasonable photograph variation. Flagged, not added.
    /// </summary>
    public sealed class HomepageMediaStorage
    {
        private readonly string _physicalRoot;
        private readonly string _urlPrefix;
        private readonly long _maxBytes;

        public HomepageMediaStorage(string physicalRoot, string urlPrefix, long maxBytes)
        {
            if (string.IsNullOrWhiteSpace(physicalRoot))
            {
                throw new ArgumentException("Media physical root must not be empty.", nameof(physicalRoot));
            }
            if (string.IsNullOrWhiteSpace(urlPrefix))
            {
                throw new ArgumentException("Media URL prefix must not be empty.", nameof(urlPrefix));
            }

            _physicalRoot = physicalRoot;
            _urlPrefix = urlPrefix.TrimEnd('/');
            _maxBytes = maxBytes;
        }

        /// <summary>Validates, writes the file under a fresh server-generated name, and returns its URL-relative path (e.g. "/media/hero-&lt;guid&gt;.jpg").</summary>
        public string SaveHeroImage(byte[] content)
        {
            if (content == null || content.Length == 0)
            {
                throw new HomepageValidationException("Please choose an image to upload.");
            }
            if (content.Length > _maxBytes)
            {
                var maxMb = _maxBytes / (1024 * 1024);
                throw new HomepageValidationException($"That image is too large. Please choose a file under {maxMb} MB.");
            }

            var extension = DetectImageExtension(content);

            if (!Directory.Exists(_physicalRoot))
            {
                Directory.CreateDirectory(_physicalRoot);
            }

            var fileName = $"hero-{Guid.NewGuid():N}{extension}";
            var physicalPath = Path.Combine(_physicalRoot, fileName);

            // Defense in depth: fileName is always our own GUID, so this can
            // never actually fire, but it costs nothing to assert it.
            var resolvedRoot = Path.GetFullPath(_physicalRoot);
            var resolvedPath = Path.GetFullPath(physicalPath);
            if (!resolvedPath.StartsWith(resolvedRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new HomepageValidationException("Unable to save this file.");
            }

            File.WriteAllBytes(resolvedPath, content);
            return $"{_urlPrefix}/{fileName}";
        }

        /// <summary>Removes the file behind a previously-returned URL-relative path. A no-op if the path is empty or the file is already gone.</summary>
        public void Delete(string urlRelativePath)
        {
            if (string.IsNullOrWhiteSpace(urlRelativePath))
            {
                return;
            }

            // Strip to a bare file name — even if the stored value were ever
            // tampered with, this prevents it from addressing anything
            // outside the media root.
            var fileName = Path.GetFileName(urlRelativePath);
            if (string.IsNullOrEmpty(fileName))
            {
                return;
            }

            var physicalPath = Path.Combine(_physicalRoot, fileName);
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }

        private static string DetectImageExtension(byte[] content)
        {
            if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF)
            {
                return ".jpg";
            }
            if (content.Length >= 8
                && content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47
                && content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A)
            {
                return ".png";
            }
            if (content.Length >= 12
                && content[0] == 0x52 && content[1] == 0x49 && content[2] == 0x46 && content[3] == 0x46
                && content[8] == 0x57 && content[9] == 0x45 && content[10] == 0x42 && content[11] == 0x50)
            {
                return ".webp";
            }

            throw new HomepageValidationException("Please upload a JPEG, PNG, or WebP image.");
        }
    }
}
