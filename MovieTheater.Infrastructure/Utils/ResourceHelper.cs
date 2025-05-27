using System.Reflection;


namespace MovieTheater.Infrastructure.Utils
{
    public static class ResourceHelper
    {
        public static string ReadResource(string relativePath, Assembly fromAssembly)
        {
            var assembly = fromAssembly;
            if ((object)assembly == null)
                assembly = typeof(ResourceHelper).Assembly;
            var str = relativePath.Replace('/', '.').Replace('\\', '.');

            using (var manifestResourceStream = assembly.GetManifestResourceStream(assembly.GetName().Name + "." + str))
            {
                if (manifestResourceStream == null)
                    throw new IOException("Failed to read manifest resource.");
                using (var streamReader = new StreamReader(manifestResourceStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        public static string ReadJsonResource(
            string relativePath,
            Assembly fromAssembly,
            bool stripWhitespace = false)
        {
            return !stripWhitespace
                ? ReadResource(relativePath, fromAssembly)
                : ReadResource(relativePath, fromAssembly).StripJsonWhitespace();
        }

        public static int DateTimeValidate(DateTime startDate, DateTime endDate)
        {
            // Chỉ lấy phần ngày, bỏ qua phần giờ
            startDate = startDate.Date;
            endDate = endDate.Date;

            if (endDate < startDate)
                throw new Exception("Invalid date time input: EndDate cannot be earlier than StartDate.");

            var duration = endDate - startDate;

            // Trả về số ngày dưới dạng số nguyên và thêm 1 để bao gồm cả ngày bắt đầu
            return duration.Days + 1;
        }
    }
}
