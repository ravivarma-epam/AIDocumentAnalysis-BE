namespace AIDocumentAnalysis.Exceptions
{
    public class DocumentIntelligenceException : Exception
    {
        public DocumentIntelligenceException(string message)
            : base(message)
        {
        }

        public DocumentIntelligenceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
