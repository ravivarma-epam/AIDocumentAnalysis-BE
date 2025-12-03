using System.Text.RegularExpressions;

using LanguageExt;
using LanguageExt.Common;

namespace AIDocumentAnalysis.Configurations
{
    public class TextFileService
    {
        public bool DoesFileExists(FilePathString filePath)
        {
            ArgumentNullException.ThrowIfNull(filePath);
            return File.Exists(filePath.ToString());
        }
    }

    public class FilePathString : NewType<FilePathString, string>
    {
        private static readonly Regex SpecialCharacterRegex = new Regex("[!@#$%\\^&*()?\\\"\\'{}|<>+=`·:'\\[\\]]");

        public FilePathString(string rawPath)
            : base(rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath) || rawPath.StartsWith(" ", StringComparison.InvariantCultureIgnoreCase) || SpecialCharacterRegex.IsMatch(rawPath))
            {
                throw new ArgumentException("Provide a non-empty string. " + rawPath + " is null or whitespace or starts with a white space.");
            }
        }

        public static Validation<Error, FilePathString> NewValid(string value)
        {
            try
            {
                return new FilePathString(value);
            }
            catch (Exception ex) when (ex is ArgumentException)
            {
                return Error.New(ex);
            }
        }

        public static Either<Exception, FilePathString> NewEither(string value)
        {
            try
            {
                return new FilePathString(value);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is ArgumentNullException)
            {
                return ex;
            }
        }
    }

}