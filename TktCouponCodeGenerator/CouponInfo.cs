using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TktCouponCodeGenerator
{
    internal record CouponInfo(string CouponCode, int DiscountedFee, int Count);
}
