using AngleSharp.Html.Dom;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;

namespace TktCouponCodeGenerator
{
    internal static class SeleniumRunner
    {
        private static readonly int TrialCountMax = 20;

        public static async void Run(TargetConfig config, IEnumerable<CouponInfo> couponInfos)
        {
            var Wait = async (double sec) => await Task.Delay((int)sec * 1000);

            new DriverManager().SetUpDriver(new ChromeConfig(), VersionResolveStrategy.Latest);
            var c = new ChromeDriver();

            // イベントの管理画面を開く
            c.Navigate().GoToUrl($"https://teket.jp/group/{config.GroupId}/event/ticket-setting/{config.EventId}/{config.ShowId}");

            await Wait(2);

            // cookie 使用許可
            var cookieNotice = c.FindElement(By.Id("ulccwidparent"));
            cookieNotice.FindElement(By.ClassName("cc-allow")).Click();

            // email
            c.FindElement(By.Id("mail")).SendKeys(config.Email);

            // password
            c.FindElement(By.Id("pw")).SendKeys(config.Password);

            // submit
            c.FindElement(By.Id("login-email")).Submit();

            await Wait(2);

            if (c.Url.Contains("https://teket.jp/login"))
            {
                throw new Exception("認証に失敗しました。メールアドレスとパスワードを確認してください。");
            }

            // ネットワークが遅いと失敗するかもしれないので何回か試す
            IWebElement targetTicketArea;
            for (int i = 1; ; i++)
            {
                try
                {
                    // クーポンを追加する券種を探す
                    var tickets = c
                        .FindElement(By.Id("control-panel"))
                        .FindElement(By.ClassName("lists"))
                        .FindElements(By.CssSelector("li"));
                    targetTicketArea = tickets
                        .Where(x =>
                        {
                            var ticketType = new SelectElement(x
                                .FindElement(By.CssSelector(".ticket-form__ticket-type select")))
                                .SelectedOption
                                .Text;

                            var ticketName = x
                                .FindElement(By.CssSelector(".ticket-form__ticket-name input"))
                                .GetAttribute("value");
                            return ticketType == config.TicketType && ticketName == config.TicketName;
                        })
                        .First();

                    // 詳細設定をクリック
                    var detailButton = targetTicketArea
                        .FindElements(By.CssSelector("button"))
                        .Where(x => x.GetAttribute("textContent") == "詳細設定")
                        .First();
                    detailButton.Click();
                    break;
                }
                catch
                {
                    Console.WriteLine($"Trial {i} failed...");
                    if (i >= TrialCountMax)
                    {
                        throw;
                    }
                    await Wait(2);
                }
            };

            await Wait(2);

            var GetCouponItems = () => c
                .FindElements(By.CssSelector(".ticket-coupon__list > .coupon-item"));
            var GetCouponCodeInput = (IWebElement e) => e
                .FindElement(By.ClassName("detail-upper__code"))
                .FindElement(By.CssSelector("input"));
            var GetFeeInput = (IWebElement e) => e
                .FindElement(By.ClassName("detail-upper__price"))
                .FindElement(By.CssSelector("input"));
            var GetCountInput = (IWebElement e) => e
                .FindElement(By.ClassName("detail-upper__limit-num"))
                .FindElement(By.CssSelector("input"));

            // 登録済みのクーポンを取得
            var currentCoupons = GetCouponItems()
                .Select(GetCouponCodeInput)
                .Select(x => x.GetAttribute("value"));
            var registeredCoupons = new HashSet<string>(currentCoupons);

            foreach (var info in couponInfos)
            {
                if (registeredCoupons.Contains(info.CouponCode))
                {
                    Console.WriteLine($"Coupon code {info.CouponCode} is already registered");
                    continue;
                }

                await Wait(1);

                for (int i = 1; ; i++)
                {
                    try
                    {
                        // 入力欄を追加
                        if (!registeredCoupons.Any())
                        {
                            // クーポンが未登録の場合「設定する」をクリック
                            c.FindElement(By.CssSelector(".ticket-coupon__none > button")).Click();
                        }
                        else
                        {
                            targetTicketArea
                                .FindElements(By.CssSelector("button"))
                                .Where(x => x.GetAttribute("textContent") == "追加")
                                .First()
                                .Click();
                        }
                        break;
                    }
                    catch
                    {
                        Console.WriteLine($"Trial {i} failed...");
                        if (i >= TrialCountMax)
                        {
                            throw;
                        }
                        await Wait(0.5);
                    }
                }

                for (int i = 1; ; i++)
                {
                    try
                    {
                        var addedCouponItem = GetCouponItems().Last();
                        var couponCodeInput = GetCouponCodeInput(addedCouponItem);
                        couponCodeInput.Clear();
                        couponCodeInput.SendKeys(info.CouponCode);
                        var feeInput = GetFeeInput(addedCouponItem);
                        feeInput.Clear();
                        feeInput.SendKeys(info.DiscountedFee.ToString());
                        var countInput = GetCountInput(addedCouponItem);
                        countInput.Clear();
                        countInput.SendKeys(info.Count.ToString());

                        // チェック
                        if (couponCodeInput.GetAttribute("value") != info.CouponCode ||
                            feeInput.GetAttribute("value") != info.DiscountedFee.ToString() ||
                            countInput.GetAttribute("value") != info.Count.ToString())
                        {
                            throw new Exception("入力に失敗しました。");
                        }

                        registeredCoupons.Add(info.CouponCode);
                        break;
                    }
                    catch
                    {
                        Console.WriteLine($"Trial {i} failed...");
                        if (i >= TrialCountMax)
                        {
                            throw;
                        }
                        await Wait(0.5);
                    }
                }
            }
        }
    }
}
