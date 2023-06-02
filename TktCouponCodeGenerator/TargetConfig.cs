using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TktCouponCodeGenerator
{
    record TargetConfig(string Email, string Password, int GroupId, int EventId, int ShowId, string TicketName);
}
