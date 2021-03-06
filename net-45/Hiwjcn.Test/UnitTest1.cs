﻿using Autofac;
using Lib.core;
using Lib.distributed.redis;
using Lib.extension;
using Lib.helper;
using Lib.rpc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Hiwjcn.Core.Domain.User;

namespace Hiwjcn.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void fsdafds()
        {
            var steps = new List<string>();

            try
            {
                steps.Add("方法开始");
                //...
                steps.Add("获取用户");
                //...
                steps.Add("获取订单");
                //...
                steps.Add("获取商品");
                //...
                steps.Add("获取xx");
                //...
                steps.Add("方法结束");
            }
            catch (Exception e)
            {
                e.AddErrorLog();
                throw e;
            }
            finally
            {
                //方法开始=>获取用户=>获取订单=>获取商品=>获取xx=>方法结束
                steps.AsSteps().AddBusinessInfoLog();
            }
        }

        /// <summary>
        /// tolist也是引用
        /// </summary>
        [TestMethod]
        public void fsdafasdfasdfasfasdf()
        {
            var users = new List<UserEntity>() { new UserEntity() { IID = 2 }, new UserEntity() { IID = 1 } };
            var data = new { x = users.Where(x => x.IID == 2).ToList() };
            users.Where(x => x.IID == 2).ToList().ForEach(x => x.NickName = "12");

            var sections = new List<UserEntity>();


            var d = users.Where(x => x.Email.Contains("")).Join(sections.Where(x => x.NickName.Length > 0), x => x.UID, x => x.UID, (user, section) => new { });

            d = from user in users
                join section in sections
on user.UID equals section.UID
                where user.Email.Contains("") && section.NickName.Length > 0
                select new { };
        }

        [TestMethod]
        public void fasdfasdfasdfasdgasfgdf()
        {
            var list = new List<string>() { "pagetype", "page_classname" };
            list.Sort();
        }

        [TestMethod]
        public void tfasdfasfasf90()
        {
            var json = JsonHelper.ObjectToJson(new { time = DateTime.Now });
        }

        [TestMethod]
        public void genlkjlkjljkfa()
        {
            try
            {
                this.GetType().IsGenericType_(typeof(Task<>));
                this.GetType().IsGenericType_<string>();
            }
            catch (Exception e)
            { }
        }

        [TestMethod]
        public void fasdfasdfasdfads()
        {
            try
            {
                var s = false;
                s = "hiwjcn@live.com".IsEmail();
                s = "13915280232".IsMobilePhone();
                s = "www.baidu.com".IsDomain();
                s = "中文2".IsChineaseStr();
                s = "中文".IsChineaseStr();
                s = "中午3".IsChinese();
                s = "中午".IsChinese();
                s = "320623199201196215".IsIDCardNo();
                s = "127.0.0.1".IsIP();
                s = "168.0.0.8".IsIP();
                s = "jklkj4234g".IsNUMBER_OR_CHAR();
                s = "42134".IsNumber();
                s = "13915280232".IsPhone();
                s = "http://www.baidu.com".IsURL();
                s = "2016-09-08".IsDate();
                s = "08:09".IsTime();
                s = "1.234".IsFloat();
                s = "-43".IsInt();
                s = "323".LenInRange(2, 6);
            }
            catch (Exception e)
            {
                //
            }
        }

        class p
        {
            public virtual string name { get; set; }
            public virtual int age { get; set; }
        }

        [TestMethod]
        public void redistest()
        {
            try
            {
                var tttt = JsonConvert.SerializeObject(null);

                var d = new p() { name = "wj", age = 25 };
                using (var client = new RedisHelper(1))
                {
                    client.StringSet("p", d);
                    var dd = client.StringGet<p>("p");

                    client.StringIncrement("c");
                    client.StringIncrement("c");
                    client.StringIncrement("c");
                    var c = client.StringIncrement("c");
                    client.StringDecrement("c");
                    c = client.StringDecrement("c");

                    client.ListLeftPush("list", dd);
                    client.ListLeftPush("list", dd);
                    client.ListLeftPush("list", dd);
                    client.ListLeftPush("list", dd);
                    client.ListLeftPush("list", dd);
                    client.ListLeftPush("list", dd);
                    client.ListLeftPush("list", dd);
                    client.ListLeftPush("list", dd);
                    client.ListLeftPush("list", dd);
                    client.ListLeftPush("list", dd);
                    var len = client.ListLength("list");
                    //client.ListRemove("list", dd);
                    len = client.ListLength("list");
                    var listdata = client.ListRightPop<p>("list");
                    listdata = client.ListLeftPop<p>("list");
                    listdata = client.ListLeftPop<p>("list");
                    listdata = client.ListLeftPop<p>("list");
                    listdata = client.ListLeftPop<p>("list");
                    listdata = client.ListLeftPop<p>("list");
                    listdata = client.ListLeftPop<p>("list");
                    listdata = client.ListLeftPop<p>("list");
                    listdata = client.ListLeftPop<p>("list");
                    listdata = client.ListLeftPop<p>("list");
                    listdata = client.ListLeftPop<p>("list");
                    listdata = client.ListLeftPop<p>("list");
                    listdata = client.ListLeftPop<p>("list");

                    client.StringSet("n", "n");
                    client.KeyDelete("n");
                }
            }
            catch (Exception e)
            {
                //
            }
        }

        [TestMethod]
        public void mysqlconstr()
        {
            var b = new MySqlConnectionStringBuilder("");
            b.ConnectionTimeout = 10;
            var str = b.ToString();
        }

        [TestMethod]
        public void kjlkjalkjlkj()
        {
            var user = new UserEntity();
            var json = user.ToString();
        }

        [TestMethod]
        public void RedisPubSub()
        {
            var client = new RedisHelper();
            //client.Subscribe("", (channel, value) => { });
        }

        [TestMethod]
        public void OverIndex()
        {
            try
            {
                var t = Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8);


                int[] arr = null;
                var f = arr?[0];
                arr = new int[] { };
                f = arr?[0];
            }
            catch (Exception e)
            {
                //
            }
        }

        [TestMethod]
        public void RedisLockTest()
        {
            int count = 0;
            var tasklist = new List<Task>();
            for (var i = 0; i < 200; ++i)
            {
                tasklist.Add(Task.Run(() =>
                {
                    try
                    {
                        using (var @lock = new RedisDistributedLock(nameof(RedisLockTest)))
                        {
                            ++count;
                        }
                    }
                    catch (Exception e)
                    {
                        //
                    }
                }));
            }
            Task.WaitAll(tasklist.ToArray());

            count.ToString();
        }

        enum XXE : int
        {
            名字 = 1, 性别 = 2
        }

        [TestMethod]
        public void EnumTest()
        {
            var k = XXE.名字.ToString();
            var a = (int)XXE.性别;

            //
        }

        [TestMethod]
        public void fasdfsjdlfkaj()
        {
            try
            {
                var xx = Com.SimpleNumber(100);
                xx = Com.SimpleNumber(1000);
                xx = Com.SimpleNumber(3400);
                xx = Com.SimpleNumber(10000);
                xx = Com.SimpleNumber(53000);
                xx = Com.SimpleNumber(6005009);
            }
            catch (Exception e)
            {
                //
            }
        }

        [TestMethod]
        public void yibu()
        {
            Func<string, int> func = (s) =>
            {
                Thread.Sleep(3000);
                return s.Length;
            };
            func.BeginInvoke("123", (res) =>
            {
                while (!res.IsCompleted) { Thread.Sleep(100); }
                var len = func.EndInvoke(res);
            }, null);
        }

        [TestMethod]
        public void MsgException()
        {
            try
            {
                throw new MsgException("错误消息");
            }
            catch (MsgException e)
            {
                //
            }
            catch (Exception e)
            {
                //
            }
            finally
            { }
        }

        [TestMethod]
        public void TestMethod1()
        {
            try
            {
                var expiretime = Com.GetExpireTime(DateTime.Now, 60 * 60 * 6);

                var matchs = Com.FindTagsFromStr("邮件内容：#微博#上这几天的热门话题是#美国大选#");

                matchs = Com.FindAtFromStr("@王俊，提醒测试@douya @213 @douya123提示");

                //var str = Com.RunExec("d:", "dir");
            }
            catch (Exception e)
            {
                //
            }
        }
    }
}
