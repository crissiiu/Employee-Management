using System.Text.Json.Serialization;

namespace BaseLibrary.Entities
{
    public class BaseEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        //Relationship: One to many

        /// <summary>
        /// JsonIgnore bỏ qua hoàn toàn khi chuyển đổi đối uotnwjg Object sang Json. Được dùng để:
        /// 1. Bảo mật: Không muốn lộ thông tin nhạy cảm trong quá trình chuyển đổi như Password hay SecretKey.
        /// 2. Tiết kiệm băng thông: Loại bỏ các trường dữ liệu tính toán trung gian không cần thiết cho phía client.
        /// 3. Tránh lỗi vòng lặp. Khi class A chứa class B và B lại chứa class A. Nếu khôgn ngắt bằng JsonIgnore trình chuyển đổi sẽ bị treo.
        /// </summary>
        [JsonIgnore]
        public List<Employee>? Employees { get; set; }
    }
}
