﻿using Business;
using Business.Attributes;
using Business.Result;
using Business.Utils;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;

namespace BenchmarkTest
{
    class Program
    {
        static BusinessMember Member = Bind.Create<BusinessMember>().UseType(typeof(Business.Auth.IToken));
        static CommandGroup Cmd = Member.Command;
        static Configer Cfg = Member.Configer;

        static void Main(string[] args)
        {
            //~preheat
            var t = Cmd.AsyncCallUse("Test001", null, new object[] { new Arg00 { A = 123 } }, new UseEntry("use01", "sss"), new Business.Auth.Token { Key = "a", Remote = "b" });
            t.Wait();
            //~end

            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

#if DEBUG
            var count = 1000;

#else
            var count = 100000;
#endif

            var results = new ConcurrentBag<int>();
            var tasks = new ConcurrentBag<Task>();

            Parallel.For(0, count, c =>
            {
                var result = Cmd.CallUse("Test000", null, new object[] { new Arg00 { A = c } }, new UseEntry("use01", "abc"), new Business.Auth.Token { Key = "a", Remote = "b" });

                results.Add(result.Data);

                var task = Cmd.AsyncCallUse("Test001", null, new object[] { new Arg00 { A = c } }, new UseEntry("use01", "abc"), new Business.Auth.Token { Key = "a", Remote = "b" }).ContinueWith(c2 =>
                {
                    results.Add(c2.Result.Data);
                });

                tasks.Add(task);

                result = Cmd.CallIResultUse("Test002", null, new object[] { new Arg00 { A = c } }, new UseEntry("use01", "abc"), new Business.Auth.Token { Key = "a", Remote = "b" });

                results.Add(result.Data);
            });

            Task.WaitAll(tasks.ToArray());

            watch.Stop();
            var total = Help.Scale(watch.Elapsed.TotalSeconds, 3);

            var dist = results.Distinct().OrderBy(c => c).ToList();

            Console.WriteLine($"ResultCount={results.Count} DistCount={dist.Count} Time={total}");
        }
    }

    /// <summary>
    /// Attr01
    /// </summary>
    public class Proces01 : ArgumentAttribute
    {
        public Proces01(int state = -110, string message = null, bool canNull = true) : base(state, message, canNull) { }

        public async override Task<IResult> Proces(dynamic value) => this.ResultCreate(data: value + 1);
    }

    public class Arg00
    {
        /// <summary>
        /// Child Property, Field Agr<> type is only applicable to JSON
        /// </summary>
        [CheckNull]
        [Proces01(-801, "{Nick} cannot be empty, please enter the correct {Nick}", Nick = "pas2")]
        public int A;
    }

    [Info("Business", CommandGroupDefault = "DEF")]
    public class BusinessMember : BusinessBase
    {
        public virtual dynamic Test000([Use(true)]dynamic use01, Arg<Arg00> arg00, Business.Auth.Token token) => this.ResultCreate(data: arg00.Out.A + 1);

        public virtual async Task<dynamic> Test001([Use(true)]dynamic use01, Arg<Arg00> arg00, Business.Auth.Token token) => this.ResultCreate(data: arg00.Out.A + 1);

        public virtual IResult Test002([Use(true)]dynamic use01, Arg<Arg00> arg00, Business.Auth.Token token) => this.ResultCreate(data: arg00.Out.A + 1);
    }
}
