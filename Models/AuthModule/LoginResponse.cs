namespace JiASsist.Models.AuthModule
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public User? User { get; set; }
        public IEnumerable<IssuePriority>? IssuePriorities { get; set; }
        public IEnumerable<WorkflowStep>? WorkflowStep { get; set; }
        public IEnumerable<IssueType>? IssueTypes { get; set; }
    }
}
