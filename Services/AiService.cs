using System.Text;
using System.Text.Json;
using JiASsist.Models;
using Microsoft.Extensions.Options;

namespace JiASsist.Services
{
    public class GoogleAiOptions
    {
        public string ApiKey { get; set; } = string.Empty;
        public List<string> Endpoints { get; set; } = new List<string>();
    }

    public class AiService
    {
        private readonly HttpClient _httpClient;
        private readonly GoogleAiOptions _options;

        public AiService(HttpClient httpClient, IOptions<GoogleAiOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<string> EvaluateProgressAsync(IEnumerable<Issue> issues, string contextName, object? additionalContext = null)
        {
            if (string.IsNullOrEmpty(_options.ApiKey))
            {
                return "AI evaluation is not configured. Please provide an API key.";
            }

            var prompt = ConstructPrompt(issues, contextName, additionalContext);
            
            foreach (var endpoint in _options.Endpoints)
            {
                try
                {
                    var response = await CallGeminiAsync(endpoint, prompt);
                    if (!string.IsNullOrEmpty(response))
                    {
                        // Vẫn trả về JSON nguyên bản để Controller lưu vào DB
                        // Nhưng chúng ta có thể format nó ở đây nếu Controller chỉ cần string kết quả cuối
                        return response; 
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error calling Gemini endpoint {endpoint}: {ex.Message}");
                }
            }

            return "Tất cả các endpoint AI không phản hồi. Vui lòng thử lại sau.";
        }

        public string FormatEvaluationToText(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var sb = new StringBuilder();

                // 1. Chỉ số tổng quan
                if (root.TryGetProperty("metrics", out var metrics))
                {
                    sb.AppendLine("📊 **TỔNG QUAN TIẾN ĐỘ**");
                    sb.AppendLine($"- Tiến độ hoàn thành: {metrics.GetProperty("progress")}%");
                    sb.AppendLine($"- Tỷ lệ lỗi (Reopen): {metrics.GetProperty("reopen_rate")}%");
                    sb.AppendLine($"- Điểm rủi ro: {metrics.GetProperty("risk_score")} ({metrics.GetProperty("risk_level")})");
                    sb.AppendLine();
                }

                // 2. Các chỉ tiêu chính
                if (root.TryGetProperty("indicators", out var indicators))
                {
                    sb.AppendLine("📌 **CHỈ TIÊU CHÍNH:**");
                    foreach (var item in indicators.EnumerateArray())
                    {
                        sb.AppendLine($"- {item.GetString()}");
                    }
                    sb.AppendLine();
                }

                // 3. Công việc chậm trễ
                if (root.TryGetProperty("delayed_tasks", out var tasks) && tasks.GetArrayLength() > 0)
                {
                    sb.AppendLine("⚠️ **CÔNG VIỆC CHẬM TRỄ / RỦI RO:**");
                    foreach (var task in tasks.EnumerateArray())
                    {
                        sb.AppendLine($"- [{task.GetProperty("id")}] (Điểm: {task.GetProperty("score")}): {task.GetProperty("reason")}");
                    }
                    sb.AppendLine();
                }

                // 4. Hiệu suất
                if (root.TryGetProperty("productivity", out var prod))
                {
                    sb.AppendLine("📈 **HIỆU SUẤT NHÂN SỰ:**");
                    if (prod.TryGetProperty("dev", out var dev))
                    {
                        foreach (var prop in dev.EnumerateObject())
                            sb.AppendLine($"- Dev ({prop.Name}): {prop.Value}");
                    }
                    if (prod.TryGetProperty("test", out var test))
                    {
                        foreach (var prop in test.EnumerateObject())
                            sb.AppendLine($"- Test ({prop.Name}): {prop.Value}");
                    }
                    sb.AppendLine();
                }

                // 5. So sánh xu hướng
                if (root.TryGetProperty("comparison", out var comp))
                {
                    sb.AppendLine("🔄 **SO SÁNH XU HƯỚNG:**");
                    sb.AppendLine(comp.GetString());
                }

                return sb.ToString();
            }
            catch
            {
                return json;
            }
        }

        private string ConstructPrompt(IEnumerable<Issue> issues, string contextName, object? additionalContext)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SYSTEM: Bạn là hệ thống phân tích chỉ số dự án. Trả về kết quả JSON bằng TIẾNG VIỆT. KHÔNG văn vẻ. KHÔNG giải thích.");
            
            sb.AppendLine("\n1. Dữ liệu đầu vào:");
            sb.AppendLine($"- Bối cảnh: {contextName}");
            if (additionalContext != null)
            {
                sb.AppendLine($"- Thông tin bổ sung: {JsonSerializer.Serialize(additionalContext)}");
            }
            
            sb.AppendLine("\n- Danh sách Issues:");
            foreach (var issue in issues)
            {
                sb.AppendLine($"- {issue.IssueId}|Status:{issue.IssueStatus}|Rate:{issue.IssueDevRate}%|Est:{issue.EstimateDev}+{issue.EstimateTest}|Reop:{issue.EstimateReopenDev}+{issue.EstimateReopenTest}|DL:{issue.DeadlineDev:yyyyMMdd}|Upd:{issue.UpdateAt:yyyyMMdd}|Usr:{issue.AssigneeId}");
            }

            sb.AppendLine("\n2. Quy tắc phân tích:");
            sb.AppendLine("- Progress = (Tổng Est DONE) / (Tổng Est tất cả) * 100");
            sb.AppendLine("- Risk_Score (0-100) dựa trên: Trễ deadline, Tỷ lệ reopen, Task lâu không cập nhật (>3 ngày), Quá tải user.");
            sb.AppendLine("- Trạng thái: DONE khi status='done'.");

            sb.AppendLine("\n3. Yêu cầu Output JSON Tiếng Việt:");
            sb.AppendLine(@"{
  ""metrics"": {
    ""progress"": number,
    ""reopen_rate"": number,
    ""risk_score"": number,
    ""risk_level"": ""OK|CẢNH BÁO|RỦI RO CAO|NGUY CẤP""
  },
  ""indicators"": [
    ""Liệt kê 3-5 chỉ số quan trọng nhất (ví dụ: Tổng giờ đã xong, số task quá hạn, tỷ lệ lỗi...)""
  ],
  ""delayed_tasks"": [
    { ""id"": ""mã task"", ""score"": number, ""reason"": ""lý do ngắn gọn"" }
  ],
  ""productivity"": {
    ""dev"": { ""username"": ""done/total_est"" },
    ""test"": { ""username"": ""done/total_est"" }
  },
  ""comparison"": ""So sánh ngắn gọn với kết quả cũ (nếu có)""
}");
            sb.AppendLine("\nKHÔNG giải thích thêm ngoài JSON.");

            return sb.ToString();
        }

        private async Task<string?> CallGeminiAsync(string endpoint, string prompt)
        {
            var url = $"{endpoint}?key={_options.ApiKey}";
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();
                
                return text?.Replace("```json", "").Replace("```", "").Trim();
            }

            return null;
        }
    }
}
