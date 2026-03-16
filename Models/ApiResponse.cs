namespace JiASsist.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public  string Message { get; set; }
        public  T? Data { get; set; }
        public string Error { get; set; }
        public ApiResponse() { }
        public ApiResponse(bool success, string message, T data, string error)
        {
            Success = success;
            Message = message;
            Data = data;
            Error = error;
        }
    }

}
