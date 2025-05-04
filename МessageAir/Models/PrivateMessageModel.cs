using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace МessageAir.Models
{
    public class PrivateMessageModel: MessageModel
    {
        public string ReceiverId { get; set; }
    }
}
