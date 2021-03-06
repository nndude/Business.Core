using Business;
using Business.Attributes;
using Business.Result;
using Business.Utils;
using Business.Auth;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Linq;

[assembly: JsonArg(Group = "G01")]
[assembly: Logger(LoggerType.Record)]
[assembly: Command(Group = "G01")]
[assembly: Command(Group = "DEF")]
namespace UnitTest
{
     /// <summary>
    /// Attr01
    /// </summary>
    public class Proces01 : ArgumentAttribute
    {
        public Proces01(int state = -110, string message = null, bool canNull = true) : base(state, message, canNull) { }

        public async override Task<IResult> Proces(dynamic value) => this.ResultCreate($"{value}.1234567890");
    }

    /// <summary>
    /// This is Arg01.
    /// </summary>
    public struct Arg01
    {
        /// <summary>
        /// Child Property, Field Agr<> type is only applicable to JSON
        /// </summary>
        [CheckNull]
        [AES("18dc5b9d92a843a8a178069b600fca47", Nick = "pas", Group = "DEF")]
        [Proces01(113, "{Nick} cannot be empty, please enter the correct {Nick}", Nick = "pas2", Group = "DEF")]
        public Arg<dynamic> A;
    }

    [Info("Business", CommandGroupDefault = "DEF")]
    public class BusinessMember : BusinessBase<ResultObject<object>>
    {
        /// <summary>
        /// This is Test001.
        /// </summary>
        /// <param name="use01">This is use01.</param>
        /// <param name="arg01"></param>
        /// <param name="b">This is b.</param>
        /// <param name="c">This is c.</param>
        /// <param name="token">This is token.</param>
        /// <returns></returns>
        [Command(Group = "G01", OnlyName = "G01Test001")]
        [Command(Group = "G01", OnlyName = "G01Test002")]
        [Command(OnlyName = "DEFTest001")]
        [Command(OnlyName = "Test001")]
        public virtual async Task<dynamic> Test001(
            [Use(true)]dynamic use01,

            Arg<Arg01> arg01,

            [Business.Attributes.Ignore(IgnoreMode.BusinessArg)]
            [Size(Min = 2, Max = 32, MinMsg = "{Nick} minimum range {Min}", MaxMsg = "{Nick} maximum range {Max}", State = 113)]
            [Nick("arg.b")]
            decimal b = 0.0234m,

            [Business.Attributes.Ignore(IgnoreMode.BusinessArg)]
            [Size(Min = 2, Max = 32, MinMsg = "{Nick} minimum range {Min}", MaxMsg = "{Nick} maximum range {Max}", State = 113, Nick = "arg.c", Group = "DEF")]
            [Size(Min = 2, Max = 32, MinMsg = "{Nick} minimum range {Min}", MaxMsg = "{Nick} maximum range {Max}", State = 114, Nick = "G01arg.c", Group = "G01")]
            decimal c = 0.0234m,

            [Logger(LoggerType.Record, Group = "DEF")]Token token = default)
            =>
            this.ResultCreate(arg01.Out.A.Out);

        [Command(Group = "G02", OnlyName = "G02Test002")]
        public virtual async Task<dynamic> Test002() => this.ResultCreate(200);

        public virtual async Task<dynamic> Test003() => this.ResultCreate(-200);

        public virtual async Task<dynamic> Test004() => this.ResultCreate(new { a = "aaa" });

        public virtual async Task<dynamic> Test005()
        {
            dynamic a = new { a = "aaa" };
            return this.ResultCreate(a);
        }

        public virtual async Task<dynamic> Test006()
        {
            string a = "aaa";
            return this.ResultCreate(a);
        }

        public virtual async Task<dynamic> Test007() => this.ResultCreate(-200m);

        public virtual async Task<dynamic> Test008() => this.ResultCreate(data: -200);

        public virtual async Task<dynamic> Test009()
        {
            dynamic a = null;
            return this.ResultCreate(a);
        }

        public virtual async Task<dynamic> Test010() => this.ResultCreate(null);

        public virtual async Task<dynamic> Test011()
        {
            int? a = null;
            return this.ResultCreate(a);
        }

        public virtual async Task<dynamic> Test012()
        {
            int? a = 111;
            return this.ResultCreate(a);
        }

        public virtual async Task<dynamic> TestUse01([Use(true)]dynamic use01) => this.ResultCreate(use01);

        public virtual async Task<dynamic> TestUse02(IToken token = default) => this.ResultCreate(token);

        public virtual async Task<dynamic> TestUse03(dynamic a, [Use(true)]dynamic use01) => this.ResultCreate($"{a}{use01}");
    }

    [TestClass]
    public class TestBusinessMember
    {
        static BusinessMember Member = Bind.Create<BusinessMember>().UseType(typeof(IToken)).UseDoc();
        static CommandGroup Cmd = Member.Command;
        static Configer Cfg = Member.Configer;

        //static dynamic Call(string cmd, params object[] args)
        //{
        //    var t = Cmd.AsyncCallGroup(cmd, null, args);
        //    t.Wait();
        //    return t.Result;
        //}

        //static dynamic CallGroup(string cmd, string group, params object[] args)
        //{
        //    var t = Cmd.AsyncCallGroup(cmd, group, args);
        //    t.Wait();
        //    return t.Result;
        //}

        static dynamic CallUse(string cmd, string group = null, object[] args = null, params object[] useObj)
        {
            var t = Cmd.AsyncCallUse(cmd, group, args, useObj);
            t.Wait();
            return t.Result;
        }

        [TestMethod]
        public void TestCfgInfo()
        {
            Assert.AreEqual(Cfg.Info.CommandGroupDefault, "DEF");
            Assert.AreEqual(Cfg.Info.Source, AttributeBase.SourceType.Class);
            Assert.AreEqual(Cfg.Info.BusinessName, "Business");
        }

        [TestMethod]
        public void TestCfgResultType()
        {
            Assert.AreEqual(Cfg.ResultType, typeof(ResultObject<>).GetGenericTypeDefinition());
            Assert.AreEqual(typeof(IResult).IsAssignableFrom(Cfg.ResultType), true);
        }

        [TestMethod]
        public void TestCfgUseTypes()
        {
            Assert.AreEqual(Cfg.UseTypes.Count, 1);
            Assert.AreEqual(Cfg.UseTypes.Contains(typeof(IToken).FullName), true);

            var meta = Cmd.GetCommand("Test001").Meta;
            Assert.AreEqual(meta.Args.First(c => c.Type == typeof(Token)).UseType, true);
        }

        [TestMethod]
        public void TestCfgDoc()
        {
            Assert.AreNotEqual(Cfg.Doc, null);
            Assert.AreEqual(Cfg.Doc.Members.Count, 3);
            Assert.AreEqual(Cfg.Doc.Members.ContainsKey("DEF"), true);
            Assert.AreEqual(Cfg.Doc.Members.ContainsKey("G01"), true);
            Assert.AreEqual(Cfg.Doc.Members.ContainsKey("G02"), true);

            Assert.AreEqual(Cfg.Doc.Members["DEF"].ContainsKey("Test001"), true);
            Assert.AreEqual(Cfg.Doc.Members["G01"].ContainsKey("G01Test001"), true);
            Assert.AreEqual(Cfg.Doc.Members["G01"].ContainsKey("G01Test002"), true);

            Assert.AreEqual(Cfg.Doc.Members["DEF"]["Test001"].Summary, "This is Test001.");
            Assert.AreEqual(Cfg.Doc.Members["DEF"]["Test001"].Args.ElementAt(0).Summary, "This is Arg01.");
            Assert.AreEqual(Cfg.Doc.Members["DEF"]["Test001"].Args.ElementAt(1).Summary, "This is b.");
        }

        [TestMethod]
        public void TestCfgAttributes()
        {
            Assert.AreEqual(Cfg.Attributes.Count, 5);

            var json = Cfg.Attributes.FirstOrDefault(c => c.Type == typeof(JsonArgAttribute));
            Assert.AreNotEqual(json, null);
            Assert.AreEqual(json.Source, AttributeBase.SourceType.Assembly);

            var logger = Cfg.Attributes.FirstOrDefault(c => c.Type == typeof(LoggerAttribute));
            Assert.AreNotEqual(logger, null);
            Assert.AreEqual(logger.Source, AttributeBase.SourceType.Assembly);

            var command = Cfg.Attributes.FirstOrDefault(c => c.Type == typeof(CommandAttribute));
            Assert.AreNotEqual(command, null);
            Assert.AreEqual(command.Source, AttributeBase.SourceType.Assembly);

            var info = Cfg.Attributes.FirstOrDefault(c => c.Type == typeof(Info));
            Assert.AreNotEqual(info, null);
            Assert.AreEqual(info.Source, AttributeBase.SourceType.Class);
        }

        [TestMethod]
        public void TestCmd()
        {
            Assert.AreEqual(Cmd.Count, 3);
            Assert.AreEqual(Cmd.ContainsKey("DEF"), true);
            Assert.AreEqual(Cmd.ContainsKey("G01"), true);
            Assert.AreEqual(Cmd.ContainsKey("G02"), true);

            Assert.AreEqual(Cmd["DEF"].Values.Any(c => c.Key == "DEF.Test001"), true);
            Assert.AreEqual(Cmd["DEF"].Values.Any(c => c.Key == "DEF.DEFTest001"), true);
            Assert.AreEqual(Cmd["DEF"].Values.Any(c => c.Key == "DEF.Test002"), true);

            Assert.AreEqual(Cmd["G01"].Values.Any(c => c.Key == "G01.G01Test001"), true);
            Assert.AreEqual(Cmd["G01"].Values.Any(c => c.Key == "G01.G01Test002"), true);
            Assert.AreEqual(Cmd["G01"].Values.Any(c => c.Key == "G01.Test002"), true);

            Assert.AreEqual(Cmd["G02"].Values.Any(c => c.Key == "G02.G02Test002"), true);
        }

        [TestMethod]
        public void TestAttrSort()
        {
            var meta = Cmd.GetCommand("Test001").Meta;

            var attr = meta.Args[1].ArgAttrChild[0].Group[meta.GroupDefault].Attrs.First;

            Assert.AreEqual(attr.Value.State, -113);
            attr = attr.Next;
            Assert.AreEqual(attr.Value.State, -800);
            attr = attr.Next;
            Assert.AreEqual(attr.Value.State, -821);
        }

        [TestMethod]
        public void TestArgNick()
        {
            var meta = Cmd.GetCommand("Test001").Meta;

            var attr = meta.Args[2].Group[meta.GroupDefault].Attrs.First;
            Assert.AreEqual(attr.Value.Nick, "arg.b");

            attr = meta.Args[3].Group[meta.GroupDefault].Attrs.First;
            Assert.AreEqual(attr.Value.Nick, "arg.c");

            attr = meta.Args[3].Group["G01.G01Test001"].Attrs.First;
            Assert.AreEqual(attr.Value.Nick, "G01arg.c");

            attr = meta.Args[3].Group["G01.G01Test002"].Attrs.First;
            Assert.AreEqual(attr.Value.Nick, "G01arg.c");
        }

        [TestMethod]
        public void TestResult01()
        {
            var t0 = Member.Test001(null, new Arg01 { A = "abc" });
            t0.Wait();
            Assert.AreEqual(typeof(IResult).IsAssignableFrom(t0.Result.GetType()), true);
            Assert.AreEqual(t0.Result.State, -113);
            Assert.AreEqual(t0.Result.Message, "arg.b minimum range 2");

            var t1 = Member.Test001(null, new Arg01 { A = "abc" }, 2, 2);
            t1.Wait();
            Assert.AreEqual(t1.Result.State, 1);
            Assert.AreEqual(t1.Result.HasData, true);

            var t2 = CallUse("Test001", null, new object[] { new Arg01 { A = "abc" }, 2, 2 });
            Assert.AreEqual(t2.State, t1.Result.State);
            Assert.AreEqual(t2.HasData, t1.Result.HasData);

            var t3 = CallUse("DEFTest001", null, new object[] { new Arg01 { A = "abc" }, 2, 2 });
            Assert.AreEqual(t3.State, t1.Result.State);
            Assert.AreEqual(t3.HasData, t1.Result.HasData);

            var t4 = CallUse("G01Test001", "G01",
                //args
                new object[] { new Arg01 { A = "abc" }.JsonSerialize(), 2, 2 },
                //useObj
                new UseEntry("use01", "sss"), new Token { Key = "a", Remote = "b" });
            Assert.AreEqual(t4.Message, null);
            Assert.AreEqual(t4.State, t1.Result.State);
            Assert.AreEqual(t4.HasData, false);

            var t5 = CallUse("Test002");
            Assert.AreEqual(t5.State, 200);

            var t6 = CallUse("Test003");
            Assert.AreEqual(t6.State, -200);

            var t7 = CallUse("Test004");
            Assert.AreEqual(t7.Data.a, "aaa");

            var t8 = CallUse("Test005");
            Assert.AreEqual(t8.Data.a, "aaa");

            var t9 = CallUse("Test006");
            Assert.AreEqual(t9.Data, "aaa");

            var t10 = CallUse("Test007");
            Assert.AreEqual(t10.Data, -200m);

            var t11 = CallUse("Test008");
            Assert.AreEqual(t11.Data, -200);

            var t12 = CallUse("Test009");
            Assert.AreEqual(t12.Data, null);

            var t13 = CallUse("Test010");
            Assert.AreEqual(t13.Data, null);

            var t14 = CallUse("Test011");
            Assert.AreEqual(t14.Data, null);

            var t15 = CallUse("Test012");
            Assert.AreEqual(t15.Data, 111);

            var t16 = CallUse("TestUse01", null, null, new UseEntry("use01", "sss"));
            Assert.AreEqual(t16.Data, "sss");

            var token = new Token { Key = "a", Remote = "b" };
            var t17 = CallUse("TestUse02", null, null, token);
            Assert.AreEqual(t17.Data, token);

            var t18 = CallUse("TestUse03", null, new object[] { "abc" }, new UseEntry("use01", "sss"));
            Assert.AreEqual(t18.Data, "abcsss");
        }
    }
}
