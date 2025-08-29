namespace McdfExporter.Data
{
    public class ExportProgress
    {
        public string Message { get; set; } = "Starting Export...";
        public int FilesProcessed = 0; 
        public int TotalFiles { get; set; } = 1;
        public bool IsFinished = false;
        public bool IsError { get; set; } = false;

        public float ProgressFraction => TotalFiles > 0 ? (float)FilesProcessed / TotalFiles : 0;
    }
}
