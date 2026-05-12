using Dapper;
using JiASsist.Models;
using JiASsist.Models.ProjectIssuesModule;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Npgsql;
using System.Data;

[ApiController]
[Route("api/issues")]
public class IssuesController : ControllerBase
{
    private readonly NpgsqlConnection _conn;
    private readonly IHubContext<IssueHub> _hub;

    public IssuesController(NpgsqlConnection conn, IHubContext<IssueHub> hub)
    {
        _conn = conn;
        _hub = hub;
    }

    [HttpGet("newIssue")]
    public async Task<IActionResult> GetNewIssueId(string projectId) {
        using var conn = _conn;
        await conn.OpenAsync();

        var issueId = await conn.QueryFirstOrDefaultAsync<string>(
             "select get_next_issue_id(@projectId)",
             new { projectId }
         );
        return Ok(new ApiResponse<string>
        {
            Success = true,
            Data = issueId
        });
    }

    [HttpGet("{issueId}")]
    public async Task<IActionResult> GetIssue(string issueId)
    {
        using var conn = _conn;
        await conn.OpenAsync();

        var issue = await conn.QueryFirstOrDefaultAsync<Issue>(@"
            SELECT *
            FROM issues
            WHERE issue_id = @IssueId
        ", new { IssueId = issueId });

        var issue_comments = await conn.QueryAsync<IssueComment>(@"
            SELECT *
            FROM issue_comments
            WHERE issue_id = @IssueId order by created_at asc;
        ", new { IssueId = issueId });

        var issue_attachments = await conn.QueryAsync<IssueAttachment>(@"
            SELECT *
            FROM issue_attachments
            WHERE issue_id = @IssueId order by created_at asc;
        ", new { IssueId = issueId });

        var issue_change_histories = await conn.QueryAsync<IssueChangeHistory>(@"
            SELECT *
            FROM issue_change_histories
            WHERE issue_id = @IssueId order by created_at asc;
        ", new { IssueId = issueId });
        if (issue == null || issue_comments ==null || issue_attachments == null || issue_change_histories == null)
            return NotFound();
        var result = new IssueDetailResponse
        {
            Issue = issue,
            Comments = issue_comments,
            Attachments = issue_attachments,
            Histories = issue_change_histories
        };
        return Ok(new ApiResponse<IssueDetailResponse>
        {
            Success = true,
            Data = result
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateIssue([FromBody] Issue model)
    {
        using var conn = _conn;
        await conn.OpenAsync();
        using var tran = conn.BeginTransaction();

        try
        {
            var sql = @"
            INSERT INTO issues(
                issue_id, issue_name, issue_status, project_id, issue_type, issue_priority_id,
                description, issue_attachment_id, list_issues, sprint_id, epic_id,
                issue_dev_rate, estimate_dev, estimate_reopen_dev,
                issue_test_rate, estimate_test, estimate_reopen_test,
                reporter_id, assignee_id, developer_id, tester_id, ba_id,
                cus_request_date, pm_request_date,
                deadline_dev, deadline_test,
                status, created_at, created_by, update_at, update_by
            )
            VALUES (
                @IssueId, @IssueName, @IssueStatus, @ProjectId, @IssueType, @IssuePriorityId,
                @Description, @IssueAttachmentId, @ListIssues, @SprintId, @EpicId,
                @IssueDevRate, @EstimateDev, @EstimateReopenDev,
                @IssueTestRate, @EstimateTest, @EstimateReopenTest,
                @ReporterId, @AssigneeId, @DeveloperId, @TesterId, @BaId,
                @CusRequestDate, @PmRequestDate,
                @DeadlineDev, @DeadlineTest,
                @Status, NOW(), @CreatedBy, NOW(), @UpdateBy
            )
        ";

            try
            {
                await conn.ExecuteAsync(sql, model, tran);
            }
            catch (PostgresException ex) when (ex.SqlState == "23505")
            {
                var newId = await conn.QueryFirstOrDefaultAsync<string>(
                    "select get_next_issue_id(@projectId)",
                    new { projectId = model.ProjectId }, tran
                );

                model.IssueId = newId;
                await conn.ExecuteAsync(sql, model, tran);
            }

            await tran.CommitAsync();

            await _hub.Clients.Group(model.ProjectId)
                .SendAsync("IssueCreated", model);

            return Ok(new ApiResponse<Issue>
            {
                Success = true,
                Data = model
            });
        }
        catch (Exception ex)
        {
            await tran.RollbackAsync();
            return BadRequest(new ApiResponse<string>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpPut("{issueId}")]
    public async Task<IActionResult> UpdateIssue(
    string issueId,
    [FromForm] Issue model,
    [FromForm] List<IFormFile>? files)
    {
        using var conn = _conn;
        await conn.OpenAsync();
        using var tran = conn.BeginTransaction();

        try
        {
            var oldIssue = await conn.QueryFirstOrDefaultAsync<Issue>(
                "SELECT * FROM issues WHERE issue_id = @IssueId",
                new { IssueId = issueId }, tran);

            if (oldIssue == null) return NotFound();

            var updateFields = new List<string>();
            var param = new DynamicParameters();
            param.Add("IssueId", issueId);

            var oldData = new Dictionary<string, object?>();
            var newData = new Dictionary<string, object?>();

            void AddField(string column, object? newVal, object? oldVal, string key)
            {
                if (newVal == null) return;

                if (newVal is string str && string.IsNullOrWhiteSpace(str))
                    return;

                if ((oldVal?.ToString() ?? "") == (newVal?.ToString() ?? ""))
                    return;

                updateFields.Add($"{column} = @{key}");
                param.Add(key, newVal);

                oldData[column] = oldVal;
                newData[column] = newVal;
            }

            // ===== BASIC =====
            AddField("issue_name", model.IssueName, oldIssue.IssueName, "IssueName");
            AddField("issue_status", model.IssueStatus, oldIssue.IssueStatus, "IssueStatus");
            AddField("project_id", model.ProjectId, oldIssue.ProjectId, "ProjectId");
            AddField("issue_type", model.IssueType, oldIssue.IssueType, "IssueType");
            AddField("issue_priority_id", model.IssuePriorityId, oldIssue.IssuePriorityId, "IssuePriorityId");

            AddField("description", model.Description, oldIssue.Description, "Description");
            AddField("issue_attachment_id", model.IssueAttachmentId, oldIssue.IssueAttachmentId, "IssueAttachmentId");
            AddField("list_issues", model.ListIssues, oldIssue.ListIssues, "ListIssues");

            AddField("sprint_id", model.SprintId, oldIssue.SprintId, "SprintId");
            AddField("epic_id", model.EpicId, oldIssue.EpicId, "EpicId");

            // ===== ESTIMATE & RATE =====
            AddField("issue_dev_rate", model.IssueDevRate, oldIssue.IssueDevRate, "IssueDevRate");
            AddField("estimate_dev", model.EstimateDev, oldIssue.EstimateDev, "EstimateDev");
            AddField("estimate_reopen_dev", model.EstimateReopenDev, oldIssue.EstimateReopenDev, "EstimateReopenDev");

            AddField("issue_test_rate", model.IssueTestRate, oldIssue.IssueTestRate, "IssueTestRate");
            AddField("estimate_test", model.EstimateTest, oldIssue.EstimateTest, "EstimateTest");
            AddField("estimate_reopen_test", model.EstimateReopenTest, oldIssue.EstimateReopenTest, "EstimateReopenTest");

            // ===== USER =====
            AddField("reporter_id", model.ReporterId, oldIssue.ReporterId, "ReporterId");
            AddField("assignee_id", model.AssigneeId, oldIssue.AssigneeId, "AssigneeId");
            AddField("developer_id", model.DeveloperId, oldIssue.DeveloperId, "DeveloperId");
            AddField("tester_id", model.TesterId, oldIssue.TesterId, "TesterId");
            AddField("ba_id", model.BaId, oldIssue.BaId, "BaId");

            // ===== DATE =====
            AddField("cus_request_date", model.CusRequestDate, oldIssue.CusRequestDate, "CusRequestDate");
            AddField("pm_request_date", model.PmRequestDate, oldIssue.PmRequestDate, "PmRequestDate");

            AddField("deadline_dev", model.DeadlineDev, oldIssue.DeadlineDev, "DeadlineDev");
            AddField("deadline_test", model.DeadlineTest, oldIssue.DeadlineTest, "DeadlineTest");

            // ===== STATUS =====
            AddField("status", model.Status, oldIssue.Status, "Status");

            // ===== EXEC UPDATE =====
            if (updateFields.Count > 0)
            {
                updateFields.Add("update_at = NOW()");
                updateFields.Add("update_by = @UpdateBy");
                param.Add("UpdateBy", model.UpdateBy);

                var sql = $@"
                UPDATE issues
                SET {string.Join(", ", updateFields)}
                WHERE issue_id = @IssueId
            ";

                await conn.ExecuteAsync(sql, param, tran);
            }

            // ===== FILE UPLOAD =====
            if (files != null && files.Count > 0)
            {
                var folder = Path.Combine("wwwroot/uploads",
                    DateTime.Now.Year.ToString(),
                    DateTime.Now.Month.ToString());

                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), folder);

                if (!Directory.Exists(fullPath))
                    Directory.CreateDirectory(fullPath);

                foreach (var file in files)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var savePath = Path.Combine(fullPath, fileName);

                    using var stream = new FileStream(savePath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    await conn.ExecuteAsync(@"
                    INSERT INTO issue_attachments(issue_id, user_id, file_name, file_path, status, created_at)
                    VALUES (@IssueId, @UserId, @FileName, @FilePath, true, NOW())
                ", new
                    {
                        IssueId = issueId,
                        UserId = model.UpdateBy,
                        FileName = file.FileName,
                        FilePath = $"/uploads/{DateTime.Now.Year}/{DateTime.Now.Month}/{fileName}"
                    }, tran);
                }
            }

            // ===== LOG =====
            if (oldData.Count > 0)
            {
                await conn.ExecuteAsync(@"
                INSERT INTO issue_change_histories(
                    issue_id, user_id, old_value, new_value, status, created_at, created_by
                )
                VALUES (
                    @IssueId, @UserId, @OldValue::jsonb, @NewValue::jsonb, true, NOW(), @UserId
                )
            ", new
                {
                    IssueId = issueId,
                    UserId = model.UpdateBy,
                    OldValue = System.Text.Json.JsonSerializer.Serialize(oldData),
                    NewValue = System.Text.Json.JsonSerializer.Serialize(newData)
                }, tran);
            }

            await tran.CommitAsync();

            var result = await conn.QueryFirstOrDefaultAsync<Issue>(
                "SELECT * FROM issues WHERE issue_id = @IssueId",
                new { IssueId = issueId });

            await _hub.Clients.Group(result.ProjectId)
                .SendAsync("IssueUpdated", result);

            return Ok(new ApiResponse<Issue>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            await tran.RollbackAsync();
            return BadRequest(ex.Message);
        }
    }
    [HttpPut("{issueId}/description")]
    public async Task<IActionResult> UpdateDescription(
    string issueId,
    [FromForm] string? description,
    [FromForm] List<IFormFile>? files,
    [FromForm] string userId)
    {
        using var conn = _conn;
        await conn.OpenAsync();
        using var tran = conn.BeginTransaction();

        try
        {
            var oldIssue = await conn.QueryFirstOrDefaultAsync<Issue>(@"
            SELECT * FROM issues WHERE issue_id = @IssueId
        ", new { IssueId = issueId }, tran);

            if (oldIssue == null)
                return NotFound();

            var oldDesc = oldIssue.Description ?? "";

            // 2. Update description n?u có thay d?i
            if (!string.IsNullOrWhiteSpace(description) && description != oldDesc)
            {
                await conn.ExecuteAsync(@"
                UPDATE issues
                SET description = @Description,
                    update_at = NOW(),
                    update_by = @UserId
                WHERE issue_id = @IssueId
            ", new
                {
                    IssueId = issueId,
                    Description = description,
                    UserId = userId
                }, tran);

                // 3. Log change
                await conn.ExecuteAsync(@"
                INSERT INTO issue_change_histories(
                    issue_id, user_id, old_value, new_value, status, created_at, created_by
                )
                VALUES (
                    @IssueId,
                    @UserId,
                    @OldValue::jsonb,
                    @NewValue::jsonb,
                    true,
                    NOW(),
                    @UserId
                )
            ", new
                {
                    IssueId = issueId,
                    UserId = userId,
                    OldValue = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        description = oldDesc
                    }),
                    NewValue = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        description = description
                    })
                }, tran);
            }

            // 4. Upload file (n?u có)
            if (files != null && files.Count > 0)
            {
                var folder = Path.Combine("wwwroot/uploads",
                    DateTime.Now.Year.ToString(),
                    DateTime.Now.Month.ToString());

                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), folder);

                if (!Directory.Exists(fullPath))
                    Directory.CreateDirectory(fullPath);

                foreach (var file in files)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var savePath = Path.Combine(fullPath, fileName);

                    using var stream = new FileStream(savePath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    await conn.ExecuteAsync(@"
                    INSERT INTO issue_attachments(
                        issue_id, user_id, file_name, file_path, status, created_at, created_by
                    )
                    VALUES (
                        @IssueId, @UserId, @FileName, @FilePath, true, NOW(), @UserId
                    )
                ", new
                    {
                        IssueId = issueId,
                        UserId = userId,
                        FileName = file.FileName,
                        FilePath = $"/uploads/{DateTime.Now.Year}/{DateTime.Now.Month}/{fileName}"
                    }, tran);
                }
            }

            await tran.CommitAsync();

            // 5. L?y l?i data
            var issue = await conn.QueryFirstOrDefaultAsync<Issue>(@"
            SELECT * FROM issues WHERE issue_id = @IssueId
        ", new { IssueId = issueId });

            var attachments = await conn.QueryAsync<IssueAttachment>(@"
            SELECT * FROM issue_attachments
            WHERE issue_id = @IssueId
            ORDER BY created_at ASC
        ", new { IssueId = issueId });

            // 6. SignalR
            await _hub.Clients.Group(issue.ProjectId)
                .SendAsync("IssueUpdated", new
                {
                    issueId,
                    description = issue.Description,
                    attachments
                });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new
                {
                    description = issue.Description,
                    attachments
                }
            });
        }
        catch (Exception ex)
        {
            await tran.RollbackAsync();
            return BadRequest(ex.Message);
        }
    }
    [HttpPost("{issueId}/comment")]
    public async Task<IActionResult> AddComment(
    string issueId,
    [FromForm] string? content,
    [FromForm] List<IFormFile>? files,
    [FromForm] string userId)
    {
        using var conn = _conn;
        await conn.OpenAsync();
        using var tran = conn.BeginTransaction();

        try
        {
            if (string.IsNullOrWhiteSpace(content) && (files == null || files.Count == 0))
                return BadRequest("Empty comment");

            var commentId = await conn.QuerySingleAsync<int>(@"
            INSERT INTO issue_comments(content, issue_id, user_id, status, created_at, created_by)
            VALUES (@Content, @IssueId, @UserId, true, NOW(), @UserId)
            RETURNING issue_comment_id
        ", new
            {
                Content = content,
                IssueId = issueId,
                UserId = userId
            }, tran);

            // Upload files
            if (files != null && files.Count > 0)
            {
                var folder = Path.Combine("wwwroot/uploads",
                    DateTime.Now.Year.ToString(),
                    DateTime.Now.Month.ToString());

                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), folder);

                if (!Directory.Exists(fullPath))
                    Directory.CreateDirectory(fullPath);

                foreach (var file in files)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                    var savePath = Path.Combine(fullPath, fileName);

                    using var stream = new FileStream(savePath, FileMode.Create);
                    await file.CopyToAsync(stream);

                    await conn.ExecuteAsync(@"
                    INSERT INTO issue_attachments(issue_id, user_id, file_name, file_path, status, created_at, created_by)
                    VALUES (@IssueId, @UserId, @FileName, @FilePath, true, NOW(), @UserId)
                ", new
                    {
                        IssueId = issueId,
                        UserId = userId,
                        FileName = file.FileName,
                        FilePath = $"/uploads/{DateTime.Now.Year}/{DateTime.Now.Month}/{fileName}"
                    }, tran);
                }
            }

            await tran.CommitAsync();

            // ?? SignalR
            await _hub.Clients.Group(issueId)
                .SendAsync("CommentAdded", new { issueId, commentId, content });

            return Ok(new ApiResponse<int>
            {
                Success = true,
                Data = commentId
            });
        }
        catch (Exception ex)
        {
            await tran.RollbackAsync();
            return BadRequest(ex.Message);
        }
    }
    [HttpPut("comment/{commentId}")]
    public async Task<IActionResult> UpdateComment(
    int commentId,
    [FromBody] string content,
    [FromQuery] string userId)
    {
        using var conn = _conn;
        await conn.OpenAsync();

        var old = await conn.QueryFirstOrDefaultAsync<string>(@"
        SELECT content FROM issue_comments WHERE issue_comment_id = @Id
    ", new { Id = commentId });

        if (old == null) return NotFound();

        if (old == content) return Ok();

        await conn.ExecuteAsync(@"
        UPDATE issue_comments
        SET content = @Content,
            update_at = NOW(),
            update_by = @UserId
        WHERE issue_comment_id = @Id
    ", new
        {
            Content = content,
            UserId = userId,
            Id = commentId
        });

        await _hub.Clients.All.SendAsync("CommentUpdated", new
        {
            commentId,
            content
        });

        return Ok();
    }
    [HttpDelete("comments/{commentId}")]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        using var conn = _conn;
        await conn.OpenAsync();

        var issueId = await conn.QueryFirstOrDefaultAsync<string>(@"
            SELECT issue_id FROM issue_comments WHERE issue_comment_id = @Id
        ", new { Id = commentId });

        if (issueId == null) return NotFound();

        await conn.ExecuteAsync(@"
            DELETE FROM issue_comments WHERE issue_comment_id = @Id
        ", new { Id = commentId });

        await _hub.Clients.All.SendAsync("CommentDeleted", new { commentId, issueId });

        return Ok(new ApiResponse<int> { Success = true, Data = commentId });
    }

    [HttpDelete("attachments/{attachmentId}")]
    public async Task<IActionResult> DeleteAttachment(int attachmentId)
    {
        using var conn = _conn;
        await conn.OpenAsync();

        var attachment = await conn.QueryFirstOrDefaultAsync<IssueAttachment>(@"
            SELECT * FROM issue_attachments WHERE issue_attachment_id = @Id
        ", new { Id = attachmentId });

        if (attachment == null) return NotFound();

        if (!string.IsNullOrEmpty(attachment.FilePath))
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        await conn.ExecuteAsync(@"
            DELETE FROM issue_attachments WHERE issue_attachment_id = @Id
        ", new { Id = attachmentId });

        await _hub.Clients.All.SendAsync("AttachmentDeleted", new { attachmentId, issueId = attachment.IssueId });

        return Ok(new ApiResponse<int> { Success = true, Data = attachmentId });
    }

    [HttpDelete("{issueId}")]
    public async Task<IActionResult> DeleteIssue(string issueId)
    {
        using var conn = _conn;
        await conn.OpenAsync();

        var projectId = await conn.QueryFirstOrDefaultAsync<string>(@"
            SELECT project_id FROM issues WHERE issue_id = @IssueId
        ", new { IssueId = issueId });

        await conn.ExecuteAsync(@"
            DELETE FROM issues WHERE issue_id = @IssueId
        ", new { IssueId = issueId });

        await _hub.Clients.Group(projectId)
            .SendAsync("IssueDeleted", issueId);

        return Ok(new ApiResponse<string>
        {
            Success = true,
            Data = issueId
        });
    }
}


