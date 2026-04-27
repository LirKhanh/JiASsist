namespace JiASsist.Models.ProjectIssuesModule
{
    public class IssueDetailResponse
    {
        public Issue? Issue { get; set; }
        public IEnumerable<IssueComment>? Comments { get; set; }
        public IEnumerable<IssueAttachment>? Attachments { get; set; }
        public IEnumerable<IssueChangeHistory>? Histories { get; set; }
    }
}
