using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using RSSAIzer.Web.Options;

namespace RSSAIzer.Web.Tests
{
    public sealed class UrlBasePathAttributeTests
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("/")]
        [TestCase("/valid/path")]
        [TestCase("/valid_path")]
        [TestCase("/valid-path")]
        [TestCase("/valid/path/2")]
        [TestCase("/VALID/PATH")]
        [TestCase("/2test")]
        [TestCase("/test-")]
        [TestCase("/test_")]
        [TestCase("/a/b2/c_3/d-4")]
        [TestCase("/123/456")]
        [TestCase("/a")]
        [TestCase("/a-b_c")]
        public void TestValidPaths(string? value)
        {
            var attribute = new UrlBasePathAttribute("");
            var result = attribute.GetValidationResult(value, new(new()));
            result.Should().Be(ValidationResult.Success);
        }

        [TestCase("/invalid/path/", NOT_END_WITH_SLASH)]
        [TestCase("//invalid/path", NO_CONSECUTIVE_SLASHES)]
        [TestCase("/invalid/path/😎", ONLY_ASCII_CHARACTERS)]
        [TestCase("/_invalid/path", SEGMENT_START_WITH_LETTER_OR_DIGIT)]
        [TestCase("/invalid/path!", ONLY_LETTERS_NUMBERS_HYPHENS_UNDERSCORES)]
        [TestCase("/-invalid", SEGMENT_START_WITH_LETTER_OR_DIGIT)]
        [TestCase("/test//path", NO_CONSECUTIVE_SLASHES)]
        [TestCase("/ñaca/path", ONLY_ASCII_CHARACTERS)]
        [TestCase("/test.path", ONLY_LETTERS_NUMBERS_HYPHENS_UNDERSCORES)]
        [TestCase("/test path", NO_SPACES)]
        [TestCase("missing/leading/slash", START_WITH_SLASH)]
        [TestCase("/te$t", ONLY_LETTERS_NUMBERS_HYPHENS_UNDERSCORES)]
        [TestCase("/a//b", NO_CONSECUTIVE_SLASHES)]
        [TestCase("/a/_/b", SEGMENT_START_WITH_LETTER_OR_DIGIT)]
        [TestCase("/a/1./b", ONLY_LETTERS_NUMBERS_HYPHENS_UNDERSCORES)]
        [TestCase(" ", NO_SPACES)]
        [TestCase("/ ", NO_SPACES)]
        [TestCase("/ / ", NO_SPACES)]
        [TestCase("/x/ ", NO_SPACES)]
        [TestCase("/x /x", NO_SPACES)]
        public void TestInvalidPaths(string value, string message)
        {
            var attribute = new UrlBasePathAttribute("");
            (attribute.GetValidationResult(value, new(new()))?.ErrorMessage).Should().Be(message);
        }

        private const string START_WITH_SLASH = "Path must start with '/'";
        private const string NO_SPACES = "Path must not contain spaces";

        private const string SEGMENT_START_WITH_LETTER_OR_DIGIT =
            "Path segment must start with a letter or digit";

        private const string ONLY_LETTERS_NUMBERS_HYPHENS_UNDERSCORES =
            "Path can only contain letters, numbers, and /-_";

        private const string ONLY_ASCII_CHARACTERS = "Path can only contain ASCII characters";

        private const string NO_CONSECUTIVE_SLASHES = "Path must not contain consecutive slashes";

        private const string NOT_END_WITH_SLASH = "Path must not end with '/' (except single '/')";
    }
}
