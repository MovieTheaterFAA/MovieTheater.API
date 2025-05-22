namespace MovieTheater.Application.Utils;

public static class ErrorHelper
{
    public static Exception WithStatus(int statusCode, string message)
    {
        var ex = new Exception(message);
        ex.Data["StatusCode"] = statusCode;
        return ex;
    }

    // Call khi user chưa đăng nhập hoặc token không hợp lệ
    public static Exception Unauthorized(string message = "Không được phép.")
    {
        return WithStatus(401, message); // Unauthorized
    }

    // Call khi data (user, sản phẩm, đơn hàng, v.v.) không tồn tại
    public static Exception NotFound(string message = "Không tìm thấy tài nguyên.")
    {
        return WithStatus(404, message); // Not Found
    }

    // Gọi khi client gửi request sai form (validation fail, format không đúng, thiếu params)
    public static Exception BadRequest(string message = "Dữ liệu không hợp lệ.")
    {
        return WithStatus(400, message); // Bad Request
    }

    // Call khi user đã đăng nhập nhưng không có quyền thực hiện hành động
    public static Exception Forbidden(string message = "Truy cập bị từ chối.")
    {
        return WithStatus(403, message); // Forbidden
    }

    // Call khi data bị trùng (vd: email đã tồn tại, đăng ký 2 sản phẩm giống nhau)
    public static Exception Conflict(string message = "Xung đột dữ liệu.")
    {
        return WithStatus(409, message); // Conflict
    }

    // Call khi có lỗi system không xác định (null reference, lỗi database, v.v.)
    public static Exception Internal(string message = "Lỗi hệ thống.")
    {
        return WithStatus(500, message); // Internal Server Error
    }
}