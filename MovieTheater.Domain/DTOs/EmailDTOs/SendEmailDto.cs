using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieTheater.Domain.DTOs.EmailDTOs
{
    public class SendEmailDto
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string HtmlContent { get; set; }
    }
}
