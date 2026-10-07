using System.Collections.Generic;

namespace GYM_Management_System.DTOs
{
    /// <summary>
    /// Wrapper chuẩn cho MỌI response trả về từ API.
    /// FE dựa vào <c>Success</c> + <c>Data</c> + <c>Message</c> + <c>Errors</c>
    /// để hiển thị banner / toast / form error.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; } = true;
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();

        /// <summary>Tạo response thành công nhanh cho controller.</summary>
        public static ApiResponse<T> Ok(T data, string? message = null) => new()
        {
            Success = true,
            Message = message,
            Data = data
        };

        /// <summary>Tạo response thất bại nhanh (thường dùng cho validate thủ công trong controller).</summary>
        public static ApiResponse<T> Fail(string message, params string[] errors) => new()
        {
            Success = false,
            Message = message,
            Errors = errors.Length > 0 ? new List<string>(errors) : new List<string> { message }
        };
    }

    /// <summary>
    /// Wrapper phân trang chuẩn cho các danh sách có Items + TotalCount.
    /// </summary>
    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }

        public static PaginatedResponse<T> Create(List<T> items, int totalCount, int pageNumber, int pageSize)
        {
            var totalPages = pageSize <= 0 ? 0 : (int)System.Math.Ceiling(totalCount / (double)pageSize);

            return new PaginatedResponse<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = totalPages,
                HasNextPage = pageNumber < totalPages,
                HasPreviousPage = pageNumber > 1
            };
        }
    }
}
