﻿/*==================================
             ########
            ##########

             ########
            ##########
          ##############
         #######  #######
        ######      ######
        #####        #####
        ####          ####
        ####   ####   ####
        #####  ####  #####
         ################
          ##############
==================================*/

namespace Business
{
    using System.Reflection;
    using System.Linq;
    using Business.Utils;
    using Business.Utils.Emit;
    using Business.Result;
    using Business.Meta;
    using Business.Attributes;

    public partial class Bind
    {
        internal static IResult CmdError(System.Type resultType, string cmd) => ResultFactory.ResultCreate(resultType, -1, $"Without this Cmd {cmd}");

        ///// <summary>
        ///// Default
        ///// </summary>
        //public const string CommandGroupDefault = "Default";

        #region Internal

        //internal static System.Func<string, string> GetCommandGroupDefault = name => GetCommandGroup(CommandGroupDefault, name);

        //internal static System.Func<string, string, string> GetCommandGroup = (group, name) => string.Format("{0}.{1}", group, name);

        internal static System.Collections.Generic.Dictionary<int, IArg> GetIArgs(System.Collections.Generic.IReadOnlyList<Args> iArgs, object[] argsObj, string defaultCommandKey)
        {
            var result = new System.Collections.Generic.Dictionary<int, IArg>();

            if (0 < iArgs?.Count)
            {
                foreach (var item in iArgs)
                {
                    var iArg = (IArg)(argsObj[item.Position] ?? System.Activator.CreateInstance(item.Type));

                    if (string.IsNullOrWhiteSpace(iArg.Group)) { iArg.Group = defaultCommandKey; }

                    //iArg.Log = item.IArgLog;

                    result.Add(item.Position, iArg);
                }
            }

            return result;
        }

        internal static object GetReturnValue(int state, string message, MetaData meta, System.Type resultType) => (meta.HasIResult || meta.HasObject) ? ResultFactory.ResultCreate(resultType, state, message) : meta.HasReturn && meta.ReturnType.IsValueType ? System.Activator.CreateInstance(meta.ReturnType) : null;

        // : meta.ReturnType.Equals(typeof(string)) ? message
        internal static object GetReturnValue(IResult result, MetaData meta)
        {
            var result2 = (meta.HasIResult || meta.HasObject) ? result : meta.HasReturn && meta.ReturnType.IsValueType ? System.Activator.CreateInstance(meta.ReturnType) : null;

            //if (meta.HasAsync)
            //{
            //    return meta.HasIResult ? System.Threading.Tasks.Task.Run(() => (IResult)result2) : meta.HasReturn ? System.Threading.Tasks.Task.Run(() => result2) : System.Threading.Tasks.Task.Run(() => { });
            //}
            if (meta.HasAsync)
            {
                return meta.HasIResult ? System.Threading.Tasks.Task.FromResult((IResult)result2) : meta.HasReturn ? System.Threading.Tasks.Task.FromResult(result2) : System.Threading.Tasks.Task.Run(() => { });
            }

            return result2;
        }

        //public static ConcurrentReadOnlyDictionary<string, IBusiness> BusinessList = new ConcurrentReadOnlyDictionary<string, IBusiness>(new System.Collections.Concurrent.ConcurrentDictionary<string, IBusiness>(System.StringComparer.InvariantCultureIgnoreCase));

        //public static ConcurrentReadOnlyDictionary<string, Xml> Xmls = new ConcurrentReadOnlyDictionary<string, Xml>();

        #endregion

        #region Create

        /// <summary>
        /// Initialize a Generic proxy class
        /// </summary>
        /// <typeparam name="Business"></typeparam>
        /// <param name="constructorArguments"></param>
        /// <returns></returns>
        public static Business Create<Business>(params object[] constructorArguments)
            where Business : class
        {
            return (Business)Create(typeof(Business), null, constructorArguments);
        }

        /// <summary>
        /// Initialize a Generic proxy class
        /// </summary>
        /// <typeparam name="Business"></typeparam>
        /// <param name="interceptor"></param>
        /// <param name="constructorArguments"></param>
        /// <returns></returns>
        public static Business Create<Business>(Auth.IInterceptor interceptor = null, params object[] constructorArguments)
            where Business : class
        {
            return (Business)Create(typeof(Business), interceptor, constructorArguments);
        }

        /// <summary>
        /// Initialize a Type proxy class
        /// </summary>
        /// <param name="type"></param>
        /// <param name="constructorArguments"></param>
        /// <returns></returns>
        public static object Create(System.Type type, params object[] constructorArguments)
        {
            return Create(type, null, constructorArguments);
        }

        /// <summary>
        /// Initialize a Type proxy class
        /// </summary>
        /// <param name="type"></param>
        /// <param name="interceptor"></param>
        /// <param name="constructorArguments"></param>
        /// <returns></returns>
        public static object Create(System.Type type, Auth.IInterceptor interceptor = null, params object[] constructorArguments)
        {
            return new Bind(type, interceptor ?? new Auth.Interceptor(), constructorArguments).Instance;
        }

        //public static void UseType(params System.Type[] type)
        //{
        //    foreach (var item in BusinessList.Values)
        //    {
        //        item.UseType(type);
        //    }
        //}

        //public static void UseDoc()
        //{
        //    foreach (var item in BusinessList.Values)
        //    {
        //        item.UseDoc();
        //    }
        //}

        #endregion
    }

    class BusinessAllMethodsHook : Castle.DynamicProxy.AllMethodsHook
    {
        readonly MethodInfo[] ignoreMethods;

        public BusinessAllMethodsHook(params MethodInfo[] method)
            : base() { ignoreMethods = method; }

        public override bool ShouldInterceptMethod(System.Type type, MethodInfo methodInfo)
        {
            if (System.Array.Exists(ignoreMethods, c => string.Equals(c.GetMethodFullName(), methodInfo.GetMethodFullName()))) { return false; }

            return base.ShouldInterceptMethod(type, methodInfo);
        }
    }

    public partial class Bind : System.IDisposable
    {
        public object Instance { get; private set; }

        public Bind(System.Type type, Auth.IInterceptor interceptor, params object[] constructorArguments)
        {
            var typeInfo = type.GetTypeInfo();

            var methods = GetMethods(typeInfo);

            //var options = new Castle.DynamicProxy.ProxyGenerationOptions(new BusinessAllMethodsHook(methods.Item1));
            var proxy = new Castle.DynamicProxy.ProxyGenerator();

            try
            {
                //Castle.DynamicProxy.ProxyUtil.IsAccessible();
                //Instance = proxy.CreateClassProxy(type, options, constructorArguments, interceptor);
                Instance = proxy.CreateClassProxy(type, constructorArguments, interceptor);
            }
            catch (System.Exception ex)
            {
                throw ex.ExceptionWrite(true, true);
            }

            //container.Intercept(c => true, async invocationInfo =>
            //{
            //    return await System.Threading.Tasks.Task.FromResult(invocationInfo.Proceed());
            //});


            var generics = typeof(IBusiness<>).IsAssignableFrom(type.GetTypeInfo(), out System.Type[] businessArguments);

            //var requestType = generics ? businessArguments[0].GetGenericTypeDefinition() : typeof(RequestObject<string>).GetGenericTypeDefinition();
            var resultType = generics ? businessArguments[0].GetGenericTypeDefinition() : typeof(ResultObject<string>).GetGenericTypeDefinition();
            //var token = generics ? ConstructorInvokerGenerator.CreateDelegate<Auth.IToken>(businessArguments[1]) : () => new Auth.Token();

            //var resultType = ((typeof(IResult<>).GetTypeInfo().IsAssignableFrom(typeof(Result).GetTypeInfo(), out _) ? typeof(Result) : typeof(ResultObject<>)).GetGenericTypeDefinition()).GetTypeInfo();

            //var requestType = ((typeof(IRequest<>).GetTypeInfo().IsAssignableFrom(typeof(Request).GetTypeInfo(), out _) ? typeof(Request) : typeof(RequestObject<>)).GetGenericTypeDefinition()).GetTypeInfo();

            //var attributes = typeInfo.GetAttributes().Distinct();

            var attributes = AttributeBase.GetAttributes(typeInfo);//GetArgAttr(typeInfo);

            //Help.GetAttr(attributes, Equality<Attributes.LoggerAttribute>.CreateComparer(c => c.LogType));
            //Help.GetAttr(attributes, Equality<Attributes.RouteAttribute>.CreateComparer(c => new { c.Path, c.Verbs, c.Group }));

            #region LoggerAttribute

            //var loggerBase = attributes.GetAttr<LoggerAttribute>();//AssemblyAttr<LoggerAttribute>(typeInfo.Assembly, GropuAttribute.Comparer);

            //foreach (var item in loggerBase)
            //{
            //    if (!attributes.Any(c => c is LoggerAttribute && ((LoggerAttribute)c).LogType == item.LogType))
            //    {
            //        attributes.Add(item.Clone());
            //    }
            //}

            #endregion
            /*
#region RouteAttribute

            var routeBase = AssemblyAttr(typeInfo.Assembly, routeComparer);

            foreach (var item in routeBase)
            {
                if (!attributes.Any(c => c is RouteAttribute && System.String.Equals(((RouteAttribute)c).GetKey(true), item.GetKey(true), System.StringComparison.CurrentCultureIgnoreCase)))
                {
                    attributes.Add(item.Clone<RouteAttribute>());
                }
            }

#endregion
            */

            //var info = typeInfo.GetAttribute<Info>() ?? new Info(type.Name);
            var info = attributes.GetAttr<Info>() ?? new Info(type.Name);

            if (string.IsNullOrWhiteSpace(info.BusinessName))
            {
                info.BusinessName = type.Name;
            }

            //var info = typeInfo.GetAttribute<Attributes.InfoAttribute>();
            //if (null != info)
            //{
            //    if (System.String.IsNullOrWhiteSpace(info.BusinessName))
            //    {
            //        info.BusinessName = type.Name;
            //    }
            //}
            //else
            //{
            //    info = new Attributes.InfoAttribute(type.Name);
            //}

            //if (routes.Values.Any(c => System.String.Equals(c.Path, info.BusinessName, System.StringComparison.InvariantCultureIgnoreCase) && c.Root))
            //{
            //    throw new System.Exception(string.Format("Route path exists \"{0}\"", info.BusinessName));
            //}


            var business = typeof(IBusiness).IsAssignableFrom(type) ? (IBusiness)Instance : null;
#if !Mobile
            //var cfg = new Configer.Configuration(info, resultType, attributes, typeInfo.GetAttributes<Attributes.EnableWatcherAttribute>().Exists());
#else
            
#endif
            var cfg = new Configer(info, resultType, attributes);

            business?.BindBefore?.Invoke(cfg);

            interceptor.MetaData = GetInterceptorMetaData(cfg, methods, Instance);

            interceptor.ResultType = cfg.ResultType;

            if (null != business)
            {
                //var info = typeInfo.GetAttribute<Attributes.InfoAttribute>();
                //if (null != info)
                //{
                //    if (System.String.IsNullOrWhiteSpace(info.BusinessName))
                //    {
                //        info.BusinessName = type.Name;
                //    }
                //}
                //else
                //{
                //    info = new Attributes.InfoAttribute(type.Name);
                //}

                //if (routes.Values.Any(c => System.String.Equals(c.Path, info.BusinessName, System.StringComparison.InvariantCultureIgnoreCase) && c.Root))
                //{
                //    throw new System.Exception(string.Format("Route path exists \"{0}\"", info.BusinessName));
                //}

                //var requestDefault = (IRequest)System.Activator.CreateInstance(requestType.MakeGenericType(typeof(object)));
                //requestDefault.Business = business;
                cfg.MetaData = interceptor.MetaData;

                business.Configer = cfg;

                business.Command = GetBusinessCommand(business);

                interceptor.Logger = business.Logger;

                Configer.BusinessList.dictionary.TryAdd(business.Configer.Info.BusinessName, business);
                //Bind.BusinessList.dictionary.TryAdd(business.Configuration.Info.BusinessName, new ConcurrentReadOnlyCollection<IBusiness> { business });
                /*
#region HttpAttribute

                var httpBase = typeInfo.Assembly.GetCustomAttribute<HttpAttribute>();
                if (null != httpBase)
                {
                    var http = attributes.FirstOrDefault(c => c is HttpAttribute) as HttpAttribute;
                    if (null == http)
                    {
                        attributes.Add(httpBase.Clone<HttpAttribute>());
                    }
                    else if (!http.defaultConstructor)
                    {
                        http.Set(httpBase.Host, httpBase.AllowedOrigins, httpBase.AllowedMethods, httpBase.AllowedHeaders, httpBase.AllowCredentials, httpBase.ResponseContentType, httpBase.Description);
                    }
                }

#endregion
                */
                business.BindAfter?.Invoke();
            }

            /*
#region Config

            var cfgSection = Configer.Config.Instance;
            if (null != cfgSection)
            {
                var sections = Configer.Config.GetGroup(cfgSection);
                var loggerSections = sections.Item1;
                var attributeSections = sections.Item2;

                var loggerGroup = loggerSections.FirstOrDefault(c => type.FullName.Equals(c.Key));
                var attributeGroup = attributeSections.FirstOrDefault(c => type.FullName.Equals(c.Key));

                if (null != loggerGroup)
                {
                    Configer.Config.Logger(interceptor.MetaData, loggerGroup);
                }

                if (null != attributeGroup)
                {
                    Configer.Config.Attribute(interceptor.MetaData, attributeGroup);
                }
            }

#endregion
            */
        }

        public void Dispose()
        {
            var type = Instance.GetType();

            if (typeof(IBusiness).IsAssignableFrom(type))
            {
                var business = (IBusiness)Instance;

                Configer.BusinessList.dictionary.TryRemove(business.Configer.Info.BusinessName, out _);
                /*
#if !Mobile
                if (!Bind.BusinessList.Values.Any(c => c.Configuration.EnableWatcher) && Configer.Configuration.CfgWatcher.EnableRaisingEvents)
                {
                    Configer.Configuration.CfgWatcher.EnableRaisingEvents = false;
                }
#endif
                */
            }

            if (typeof(System.IDisposable).IsAssignableFrom(type))
            {
                ((System.IDisposable)Instance).Dispose();
            }
        }

        #region

        /*
        static (MethodInfo[], System.Collections.Generic.Dictionary<int, MethodMeta>) GetMethods2(TypeInfo type)
        {
            var ignoreList = new System.Collections.Generic.List<MethodInfo>();
            var list = new System.Collections.Generic.Dictionary<string, MethodMeta>();

            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(c => c.IsVirtual && !c.IsFinal);

            //var methods = type.DeclaredMethods.Where(c => c.IsVirtual && !c.IsFinal && c.IsPublic);

            foreach (var item in methods)
            {
                var ignore = item.GetAttribute<Attributes.Ignore>();
                if (null != ignore && ignore.Contains(Attributes.IgnoreMode.Method) && string.IsNullOrWhiteSpace(ignore.Group))
                {
                    ignoreList.Add(item);
                }
                else if (item.DeclaringType.Equals(type))
                {
                    list.Add(item.Name, new MethodMeta { Ignore = ignore, Method = item });
                }
            }

            //Property
            foreach (var item in type.DeclaredProperties)
            {
                var ignore = item.GetAttribute<Attributes.Ignore>();
                if (null != ignore && ignore.Contains(Attributes.IgnoreMode.Method) && string.IsNullOrWhiteSpace(ignore.Group))
                {
                    var set = item.GetSetMethod(true);
                    if (null != set)
                    {
                        ignoreList.Add(set);
                        if (list.ContainsKey(set.Name))
                        {
                            list.Remove(set.Name);
                        }
                    }

                    var get = item.GetGetMethod(true);
                    if (null != get)
                    {
                        ignoreList.Add(get);
                        if (list.ContainsKey(get.Name))
                        {
                            list.Remove(get.Name);
                        }
                    }
                }
            }

            var i = 0;
            //return (ignoreList.ToArray(), list.ToDictionary(c => i++, c => c));
            return (ignoreList.ToArray(), list.ToDictionary(c => i++, c => c.Value));
        }
        */

        static System.Collections.Generic.Dictionary<int, MethodInfo> GetMethods(TypeInfo type)
        {
            var list = new System.Collections.Generic.List<MethodInfo>();

            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public).Where(c => c.IsVirtual && !c.IsFinal);

            //var methods = type.DeclaredMethods.Where(c => c.IsVirtual && !c.IsFinal && c.IsPublic);

            foreach (var item in methods)
            {
                if (item.DeclaringType.Equals(type))
                {
                    //list.Add(new MethodMeta { Ignore = item.GetAttributes<Attributes.Ignore>(), Method = item });
                    list.Add(item);
                }
            }

            var i = 0;
            //return (ignoreList.ToArray(), list.ToDictionary(c => i++, c => c));
            return list.ToDictionary(c => i++, c => c);
        }

        static System.Collections.Generic.List<T> AssemblyAttr<T>(Assembly member, System.Collections.Generic.IEqualityComparer<T> comparer) where T : System.Attribute => member.GetCustomAttributes<T>().Distinct(comparer).ToList();

        static MetaLogger GetMetaLogger(System.Collections.Generic.List<LoggerAttribute> loggers, string group)
        {
            if (null == loggers) { return default; }

            var metaLogger = new MetaLogger();

            var loggers2 = loggers.Where(c => GroupEquals(c, group));

            foreach (var item in loggers2)
            {
                switch (item.LogType)
                {
                    case LoggerType.Record: metaLogger.Record = item; break;
                    case LoggerType.Error: metaLogger.Error = item; break;
                    case LoggerType.Exception: metaLogger.Exception = item; break;
                }
            }

            var all = loggers2.FirstOrDefault(c => c.LogType == LoggerType.All);

            if (null == metaLogger.Record)
            {
                metaLogger.Record = null == all ? new LoggerAttribute(LoggerType.Record, false) : all.Clone().SetType(LoggerType.Record);
            }
            if (null == metaLogger.Error)
            {
                metaLogger.Error = null == all ? new LoggerAttribute(LoggerType.Error, false) : all.Clone().SetType(LoggerType.Error);
            }
            if (null == metaLogger.Exception)
            {
                metaLogger.Exception = null == all ? new LoggerAttribute(LoggerType.Exception, false) : all.Clone().SetType(LoggerType.Exception);
            }

            return metaLogger;
        }

        /*
        static System.Collections.Generic.List<Attributes.AttributeBase> GetRoute(string methodName, System.Collections.Generic.List<RouteAttribute> businessRouteAttr, System.Collections.Generic.List<Attributes.AttributeBase> attributes, System.Collections.Generic.List<Attributes.CommandAttribute> commands)
        {
            var all = attributes.FindAll(c => c is RouteAttribute).Cast<RouteAttribute>().ToList();

            var notGroup = all.Where(c => !commands.Exists(c2 => System.String.Equals(c2.Group, c.Group, System.StringComparison.CurrentCultureIgnoreCase))).ToList();

            foreach (var item in notGroup)
            {
                all.Remove(item);
                attributes.Remove(item);
            }

            foreach (var item in businessRouteAttr)
            {
                if (!commands.Exists(c => System.String.Equals(c.Group, item.Group, System.StringComparison.CurrentCultureIgnoreCase)))
                {
                    continue;
                }

                if (!all.Any(c => System.String.Equals(c.GetKey(true), item.GetKey(true), System.StringComparison.CurrentCultureIgnoreCase)))
                {
                    var route = item.Clone<RouteAttribute>();
                    all.Add(route);
                    attributes.Add(route);
                }
            }

            foreach (var item in all)
            {
                if (System.String.IsNullOrWhiteSpace(item.Path))
                {
                    item.Path = methodName;
                }

                if (System.String.IsNullOrWhiteSpace(item.Group))
                {
                    item.Group = Bind.CommandGroupDefault;
                }

                item.MethodName = methodName;
            }

            return attributes;
        }
        */
        static bool IsClass(System.Type type)
        {
            //return type.IsClass || (type.IsValueType && !type.IsPrimitive && !type.IsEnum && !type.IsArray);
            return !type.FullName.StartsWith("System.") && (type.IsClass || (type.IsValueType && !type.IsPrimitive && !type.IsEnum && !type.IsArray));
            //return !type.IsPrimitive && !type.IsEnum && !type.IsArray && !type.IsSecurityTransparent;
        }

        static object[] GetDefaultValue(System.Collections.Generic.IList<Args> args)
        {
            if (null == args) { return new object[0]; }

            var argsObj = new object[args.Count];

            for (int i = 0; i < args.Count; i++)
            {
                var arg = args[i];

                if (arg.HasIArg) { continue; }

                if (arg.Type.GetTypeInfo().IsValueType && null == arg.DefaultValue)
                {
                    argsObj[i] = System.Activator.CreateInstance(arg.Type);
                }
                else if (arg.HasDefaultValue)
                {
                    argsObj[i] = arg.DefaultValue;
                }
            }

            return argsObj;
        }

        static object[] GetArgsObj(object[] defaultObj, object[] argsObj, System.Collections.Generic.IReadOnlyList<Args> iArgs, string group, System.Collections.Generic.IList<Args> args)
        {
            var defaultObj2 = new object[defaultObj.Length];
            System.Array.Copy(defaultObj, defaultObj2, defaultObj2.Length);

            if (null != argsObj)
            {
                for (int i = 0; i < argsObj.Length; i++)
                {
                    if (!Equals(null, argsObj[i]) && i < defaultObj2.Length)
                    {
                        if (!Equals(defaultObj2[i], argsObj[i]))
                        {
                            //int/long
                            //defaultObj2[i] = args[i].HasIArg ? argsObj[i] : Help.ChangeType(argsObj[i], args[i].Type);
                            defaultObj2[i] = args[i].UseType || args[i].HasIArg ? argsObj[i] : Help.ChangeType(argsObj[i], args[i].Type);
                        }
                    }
                }
            }

            foreach (var item in iArgs)
            {
                if (null == defaultObj2[item.Position] && !item.Type.IsValueType)
                {
                    continue;
                }

                var iArg = (IArg)System.Activator.CreateInstance(item.Type);

                //Not entry for value type
                if (!(null == defaultObj2[item.Position] && item.IArgInType.IsValueType))
                {
                    iArg.In = defaultObj2[item.Position];
                }

                //iArg.In = defaultObj2[item.Position];
                iArg.Group = group;

                defaultObj2[item.Position] = iArg;
            }

            //foreach (var item in iArgs)
            //{
            //    var iArg = (IArg)System.Activator.CreateInstance(item.Type);

            //    if (!(null == defaultObj2[item.Position] && item.IArgInType.IsValueType))
            //    {
            //        iArg.In = defaultObj2[item.Position];
            //    }

            //    //iArg.In = defaultObj2[item.Position];
            //    iArg.Group = group;

            //    defaultObj2[item.Position] = iArg;
            //}

            return defaultObj2;
        }

        static ConcurrentReadOnlyDictionary<string, CommandAttribute> CmdAttrGroup(Configer cfg, string methodName, System.Collections.Generic.List<AttributeBase> attributes, string groupDefault, System.Collections.Generic.List<Ignore> ignore)
        {
            var group = new ConcurrentReadOnlyDictionary<string, CommandAttribute>();

            //ignore
            var ignores = ignore.Where(c => c.Mode == IgnoreMode.Method).ToList();

            var ignoreAll = ignores.Exists(c => string.IsNullOrWhiteSpace(c.Group));

            //ignore all
            if (ignoreAll)
            {
                attributes.FindAll(c => c is CommandAttribute).ForEach(c => attributes.Remove(c));

                return group;
            }

            var notMethods = new System.Collections.Generic.List<CommandAttribute>();

            var isDef = false;

            for (int i = attributes.Count - 1; i >= 0; i--)
            {
                var item = attributes[i];

                if (item is CommandAttribute)
                {
                    var item2 = item as CommandAttribute;
                    if (string.IsNullOrWhiteSpace(item2.Group)) { item2.Group = groupDefault; }
                    if (string.IsNullOrWhiteSpace(item2.OnlyName)) { item2.OnlyName = methodName; }

                    //ignore
                    if (0 < ignores.Count && ignores.Exists(c => c.Group == item2.Group))
                    {
                        attributes.Remove(item);
                    }
                    else
                    {
                        if (!isDef && item2.Group == groupDefault) { isDef = true; }

                        if (item2.Source != AttributeBase.SourceType.Method)
                        {
                            notMethods.Add(item2);

                            //if (!group.Any(c => c.Value.Source == AttributeBase.SourceType.Method && c.Value.Group == clone.Group))
                            //{
                            //    group.dictionary.TryAdd(cfg.GetCommandGroup(clone.Group, clone.OnlyName), clone);
                            //}
                            //group.dictionary.TryAdd(cfg.GetCommandGroup(clone.Group, clone.OnlyName), clone);
                        }
                        else
                        {
                            //group.dictionary.AddOrUpdate(cfg.GetCommandGroup(item2.Group, item2.OnlyName), item2, (key, oldValue) => oldValue.Source != AttributeBase.SourceType.Method ? item2 : oldValue);
                            group.dictionary.TryAdd(cfg.GetCommandGroup(item2.Group, item2.OnlyName), item2);

                            //var command = group.FirstOrDefault(c => c.Value.Source != AttributeBase.SourceType.Method && c.Value.Group == item2.Group);
                            //if (!default(System.Collections.Generic.KeyValuePair<string, CommandAttribute>).Equals(command))
                            //{
                            //    attributes.Remove(command.Value);
                            //    group.dictionary.TryRemove(command.Key, out _);
                            //}
                        }
                    }
                }
            }

            foreach (var item in notMethods)
            {
                if (!group.Any(c => c.Value.Group == item.Group))
                {
                    var clone = item.Clone();
                    //clone.Source = AttributeBase.SourceType.Method;
                    clone.OnlyName = methodName;

                    group.dictionary.TryAdd(cfg.GetCommandGroup(clone.Group, clone.OnlyName), clone);
                }
            }

            //foreach (var item in group)
            //{
            //    if (item.Value.Source != AttributeBase.SourceType.Method)
            //    {
            //        if (group.Any(c => c.Value.Source == AttributeBase.SourceType.Method && c.Value.Group == item.Value.Group))
            //        {
            //            attributes.Remove(item.Value);
            //            group.dictionary.TryRemove(item.Key, out _);
            //        }
            //    }
            //}

            //add default group
            /*if (!group.ContainsKey(groupDefault))*/// && methodName == c.OnlyName
            if (!isDef) //(!group.Values.Any(c => groupDefault == c.Group))
            {
                group.dictionary.TryAdd(cfg.GetCommandGroup(groupDefault, methodName), new CommandAttribute(methodName) { Group = groupDefault });
            }

            return group;
        }

        public static CommandGroup GetBusinessGroup(IBusiness business, ConcurrentReadOnlyDictionary<string, MetaData> metaData, System.Func<string, MethodInfo, MetaData, Command> action)
        {
            var group = new CommandGroup(business.Configer.ResultType, business.Configer.Info.CommandGroupDefault);

            //========================================//

            var proxyType = business.GetType();

#if DEBUG
            foreach (var item in metaData)
#else
            System.Threading.Tasks.Parallel.ForEach(metaData, item =>
#endif
            {
                var meta = item.Value;

                var method2 = proxyType.GetMethod(meta.Name);

                //set all
                foreach (var item2 in meta.CommandGroup)
                {
                    var groups = group.dictionary.GetOrAdd(item2.Value.Group, key => new ConcurrentReadOnlyDictionary<string, Command>());

                    if (!groups.dictionary.TryAdd(item2.Value.OnlyName, action(item2.Key, method2, meta)))
                    {
                        throw new System.Exception($"Command \"{item2.Key}\" member \"{item2.Value.OnlyName}\" name exists");
                    }
                }
#if DEBUG
            };
#else
            });
#endif

            //========================================//

            return group;
        }

        static DynamicMethodBuilder dynamicMethodBuilder = new DynamicMethodBuilder();

        static CommandGroup GetBusinessCommand(IBusiness business)
        {
            //var routeValues = business.Configuration.Routes.Values;

            return GetBusinessGroup(business, business.Configer.MetaData, (key, method, meta) =>
            {
                //var key = business.Configer.GetCommandGroup(item.Group, item.OnlyName);//item.GetKey();//

                //var call = !meta.HasReturn && !meta.HasAsync ? (p, p1) =>
                //{
                //    MethodInvokerGenerator.CreateDelegate2(method, false, key)(p, p1); return null;
                //}
                //:
                //MethodInvokerGenerator.CreateDelegate<dynamic>(method, false, key);

                var call = dynamicMethodBuilder.GetDelegate(method) as System.Func<object, object[], dynamic>;
                /*
#region Routes

                if (null != routeValues)
                {
                    var values = routeValues.Where(c => c.MethodName == meta.Name && System.String.Equals(c.Group, item.Group, System.StringComparison.CurrentCultureIgnoreCase));
                    foreach (var item2 in values)
                    {
                        item2.Cmd = item.OnlyName;
                    }
                }

#endregion
                */

                //return new Command(arguments => call(business, GetArgsObj(meta.DefaultValue, arguments, meta.IArgs, key, meta.ArgAttrs[meta.GroupDefault].Args)), meta.Name, meta.HasReturn, meta.HasIResult, meta.ReturnType, meta.HasAsync, meta);
                return new Command(arguments => call(business, GetArgsObj(meta.DefaultValue, arguments, meta.IArgs, key, meta.Args)), meta, key);
                //, meta.ArgAttrs[Bind.GetDefaultCommandGroup(method.Name)].CommandArgs
            });
        }

        /*
        static System.Collections.Generic.IEqualityComparer<RouteAttribute> routeComparer = Equality<RouteAttribute>.CreateComparer(c => c.GetKey(true), System.StringComparer.CurrentCultureIgnoreCase);
        */

        static string GetMethodTypeFullName(string fullName, System.Collections.Generic.IList<Args> args) => string.Format("{0}{1}", fullName, (null == args || 0 == args.Count) ? null : string.Format("({0})", null == args ? null : string.Join(",", args.Select(c => c.MethodTypeFullName))));

        static string GetMethodTypeFullName(System.Type type)
        {
            if (null == type) { throw new System.ArgumentNullException(nameof(type)); }

            var name = type.FullName.Split('`')[0].Replace("+", ".");

            if (type.IsConstructedGenericType)
            {
                name = $"{name}{{{string.Join(", ", type.GenericTypeArguments.Select(c => GetMethodTypeFullName(c)))}}}";
            }

            return name;
        }

        static System.Collections.Generic.List<Ignore> GetIgnore(System.Collections.Generic.List<AttributeBase> attributes)
        {
            var ignores = attributes.Where(c => c is Ignore).Cast<Ignore>().ToList();

            return ignores;
            //var group = ignores.GroupBy(c => new { c.Mode, c.Group });

            //group.
        }

        static ConcurrentReadOnlyDictionary<string, MetaData> GetInterceptorMetaData(Configer cfg, System.Collections.Generic.Dictionary<int, MethodInfo> methods, object instance)
        {
            var metaData = new ConcurrentReadOnlyDictionary<string, MetaData>();

#if DEBUG
            foreach (var methodMeta in methods)
#else
            System.Threading.Tasks.Parallel.ForEach(methods, methodMeta =>
#endif
            {
                var method = methodMeta.Value;
                var space = method.DeclaringType.FullName;
                var attributes2 = AttributeBase.GetAttributes(method).Distinct(cfg.Attributes);

                var argAttrs = attributes2.GetAttrs<ArgumentAttribute>();

                //======LogAttribute======//
                var loggers = attributes2.GetAttrs<LoggerAttribute>();

                var ignores = attributes2.GetAttrs<Ignore>();

                //======CmdAttrGroup======//
                var commandGroup = CmdAttrGroup(cfg, method.Name, attributes2, cfg.Info.CommandGroupDefault, ignores);

                var parameters = method.GetParameters();

                var loggerGroup = new ConcurrentReadOnlyDictionary<string, MetaLogger>();

                var tokenPosition = new System.Collections.Generic.List<int>(parameters.Length);
                //var httpRequestPosition = new System.Collections.Generic.List<int>(parameters.Length);
                var useTypePosition = new ConcurrentReadOnlyDictionary<int, System.Type>();

                foreach (var item in commandGroup)
                {
                    loggerGroup.dictionary.TryAdd(item.Key, GetMetaLogger(loggers, item.Value.Group));
                }

                var args = new ReadOnlyCollection<Args>(parameters.Length);

                foreach (var argInfo in parameters)
                {
                    var path = argInfo.Name;
                    var parameterType = argInfo.ParameterType.GetTypeInfo();
                    //==================================//
                    var current = GetCurrentType(argInfo, parameterType);
                    var currentType = current.outType;

                    var argAttrAll = AttributeBase.GetAttributes(argInfo, currentType);

                    var use = current.hasIArg ? current.inType.GetAttribute<UseAttribute>() : argAttrAll.GetAttr<UseAttribute>();
                    var hasUse = null != use || (current.hasIArg ? cfg.UseTypes.Contains(current.inType.FullName) : false);
                    var nick = argAttrAll.GetAttr<NickAttribute>();

                    argAttrAll = argAttrAll.Distinct(!hasUse ? argAttrs : null);

                    //==================================//
                    var logAttrArg = argAttrAll.GetAttrs<LoggerAttribute>();
                    var inLogAttrArg = current.hasIArg ? AttributeBase.GetAttributes<LoggerAttribute>(current.inType, AttributeBase.SourceType.Parameter, GropuAttribute.Comparer) : null;

                    var hasDefinition = IsClass(parameterType);

                    var definitions = hasDefinition ? new System.Collections.Generic.List<System.Type> { currentType } : new System.Collections.Generic.List<System.Type>();

                    args.collection.Add(new Args(argInfo.Name,
                    argInfo.ParameterType,
                    argInfo.Position,
                    argInfo.HasDefaultValue ? argInfo.DefaultValue : default,
                    argInfo.HasDefaultValue,
                    default,
                    GetArgGroup(argAttrAll, current, path, default, commandGroup, cfg.ResultType, instance, hasUse, logAttrArg, inLogAttrArg),
                    hasDefinition ? GetArgAttr(currentType, path, commandGroup, ref definitions, cfg.ResultType, instance, cfg.UseTypes) : new ReadOnlyCollection<Args>(0),
                    hasDefinition,
                    current.hasIArg,
                    current.hasIArg ? current.outType : default,
                    current.hasIArg ? current.inType : default,
                    //path,
                    use,
                    hasUse,
                    //item.Value.CommandAttr.OnlyName,
                    GetMethodTypeFullName(parameterType),
                    currentType.FullName.Replace('+', '.'),
                    hasDefinition ? Args.ArgTypeCode.Definition : Args.ArgTypeCode.No));

                    if (hasUse)
                    {
                        useTypePosition.dictionary.TryAdd(argInfo.Position, current.inType);
                    }
                }

                //var groupDefault = cfg.GetCommandGroup(cfg.Info.CommandGroupDefault, method.Name);
                //var args = argAttrGroup.FirstOrDefault().Value.Args;//[groupDefault].Args;
                var fullName = method.GetMethodFullName();

                var meta = new MetaData(commandGroup, args, args?.Where(c => c.HasIArg).ToReadOnly(), loggerGroup, $"{space}.{method.Name}", method.Name, fullName, method.ReturnType.GetTypeInfo(), cfg.ResultType, GetDefaultValue(args), attributes2, methodMeta.Key, cfg.GetCommandGroup(cfg.Info.CommandGroupDefault, method.Name), useTypePosition, GetMethodTypeFullName(fullName, args));

                if (!metaData.dictionary.TryAdd(method.Name, meta))
                {
                    throw new System.Exception($"MetaData name exists \"{method.Name}\"");
                }
#if DEBUG
            };
#else
            });
#endif

            return metaData;
        }

        struct CurrentType { public bool hasIArg; public System.Type outType; public System.Type inType; }

        static CurrentType GetCurrentType(ICustomAttributeProvider member, System.Type type)
        {
            var hasIArg = typeof(IArg<,>).GetTypeInfo().IsAssignableFrom(type, out System.Type[] iArgOutType);

            return new CurrentType { hasIArg = hasIArg, outType = hasIArg ? iArgOutType[0] : type, inType = hasIArg ? iArgOutType[1] : type };
        }

        #region GetArgAttr        

        internal static System.Collections.Generic.List<ArgumentAttribute> GetArgAttr(System.Collections.Generic.List<AttributeBase> attributes, System.Type memberType, System.Type resultType, dynamic business, string path, string groupDefault = null)
        {
            var argAttr = attributes.Where(c => c is ArgumentAttribute).Cast<ArgumentAttribute>().ToList();
            GetArgAttrSort(argAttr);
            return Bind.GetArgAttr(argAttr, resultType, business, path, memberType, groupDefault);
        }

        internal static System.Collections.Generic.List<ArgumentAttribute> GetArgAttr(System.Collections.Generic.List<ArgumentAttribute> argAttr, System.Type resultType, dynamic business, string path, System.Type memberType, string groupDefault = null)
        {
            //argAttr = argAttr.FindAll(c => c.Enable);
            //argAttr.Sort(ComparisonHelper<Attributes.ArgumentAttribute>.CreateComparer(c =>c.State.ConvertErrorState()));
            //argAttr.Reverse();

            var procesTypes = new System.Type[] { typeof(object), typeof(IArg) };
            var argumentAttributeFullName = typeof(ArgumentAttribute).FullName;

            foreach (var item in argAttr)
            {
                //if (string.IsNullOrWhiteSpace(item.Group)) { item.Group = groupDefault; }

                item.Meta.resultType = resultType;
                item.Meta.Business = business;
                item.Meta.Member = path;
                item.Meta.MemberType = memberType;
                item.Meta.HasProcesIArg = !item.Type.GetMethod("Proces", BindingFlags.Public | BindingFlags.Instance, null, procesTypes, null).DeclaringType.FullName.Equals(argumentAttributeFullName);

                //if (string.IsNullOrWhiteSpace(item.Nick) && !string.IsNullOrWhiteSpace(nick))
                //{
                //    item.Nick = nick;
                //}

                // replace
                //item.Message = Attributes.ArgumentAttribute.MemberReplace(item, item.Message);
                //item.Description = Attributes.ArgumentAttribute.MemberReplace(item, item.Description);

                //item.BindAfter?.Invoke();
            }

            return argAttr;
        }

        internal static void GetArgAttrSort(System.Collections.Generic.List<ArgumentAttribute> argAttr)
        {
            argAttr.Sort(ComparisonHelper<ArgumentAttribute>.CreateComparer(c => c.State.ConvertErrorState()));
            //argAttr.Reverse();
        }

        #endregion

        static ReadOnlyCollection<Args> GetArgAttr(System.Type type, string path, ConcurrentReadOnlyDictionary<string, CommandAttribute> commands, ref System.Collections.Generic.List<System.Type> definitions, System.Type resultType, object business, System.Collections.Generic.IList<string> useTypes)
        {
            var args = new ReadOnlyCollection<Args>();

            var position = 0;

            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.GetField | BindingFlags.GetProperty);

            foreach (var item in members)
            {
                System.Type memberType = null;
                Accessor accessor = default;
                Args.ArgTypeCode argType = Args.ArgTypeCode.No;

                switch (item.MemberType)
                {
                    case MemberTypes.Field:
                        {
                            var member = item as FieldInfo;
                            accessor = member.GetAccessor();
                            if (null == accessor.Getter || null == accessor.Setter) { continue; }
                            memberType = member.FieldType;
                            argType = Args.ArgTypeCode.Field;
                        }
                        break;
                    case MemberTypes.Property:
                        {
                            var member = item as PropertyInfo;
                            accessor = member.GetAccessor();
                            if (null == accessor.Getter || null == accessor.Setter) { continue; }
                            memberType = member.PropertyType;
                            argType = Args.ArgTypeCode.Property;
                        }
                        break;
                    default: continue;
                }

                var current = GetCurrentType(item, memberType);

                var hasDefinition = IsClass(current.outType);

                if (definitions.Contains(current.outType)) { continue; }
                else if (hasDefinition) { definitions.Add(current.outType); }

                var path2 = $"{path}.{item.Name}";

                var argAttrAll = AttributeBase.GetAttributes(item, current.outType);
                var use = argAttrAll.GetAttr<UseAttribute>();
                var hasUse = null != use || (current.hasIArg ? useTypes.Contains(current.inType.FullName) : false);

                args.collection.Add(new Args(item.Name,
                    memberType,
                    position++,
                    default,
                    default,
                    accessor,
                    GetArgGroup(argAttrAll, current, path2, path, commands, resultType, business, hasUse),
                    hasDefinition ? GetArgAttr(current.outType, path2, commands, ref definitions, resultType, business, useTypes) : new ReadOnlyCollection<Args>(0),
                    hasDefinition,
                    current.hasIArg,
                    current.hasIArg ? current.outType : default,
                    current.hasIArg ? current.inType : default,
                    //path2,
                    use,
                    hasUse,
                    GetMethodTypeFullName(memberType),
                    $"{type.FullName.Replace('+', '.')}.{item.Name}",
                    argType));
            }

            return args;
        }

        /// <summary>
        /// string.IsNullOrWhiteSpace(x.Group) || x.Group == group
        /// </summary>
        /// <param name="x"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        static bool GroupEquals(GropuAttribute x, string group) => string.IsNullOrWhiteSpace(x.Group) || x.Group == group;

        static ConcurrentReadOnlyDictionary<string, ArgGroup> GetArgGroup(System.Collections.Generic.List<AttributeBase> argAttrAll, CurrentType current, string path, string owner, ConcurrentReadOnlyDictionary<string, CommandAttribute> commands, System.Type resultType, object business, bool hasUse, System.Collections.Generic.List<LoggerAttribute> log = null, System.Collections.Generic.List<LoggerAttribute> inLog = null)
        {
            var argAttrs = GetArgAttr(argAttrAll, current.outType, resultType, business, path);

            var nick = argAttrAll.GetAttr<NickAttribute>();

            var argGroup = new ConcurrentReadOnlyDictionary<string, ArgGroup>();

            foreach (var item in commands)
            {
                var ignores = argAttrAll.GetAttrs<Ignore>(c => GroupEquals(c, item.Value.Group));

                var ignoreBusinessArg = ignores.Any(c => c.Mode == IgnoreMode.BusinessArg);
                // || (item.Group == c.Group || string.IsNullOrWhiteSpace(c.Group))
                var argAttrChild = (hasUse || item.Value.IgnoreBusinessArg || ignoreBusinessArg) ?
                    argAttrs.FindAll(c => GroupEquals(c, item.Value.Group) && c.Source == AttributeBase.SourceType.Parameter) :
                    argAttrs.FindAll(c => GroupEquals(c, item.Value.Group));

                var nickValue = string.IsNullOrWhiteSpace(nick?.Nick) ? argAttrChild.Where(c => !string.IsNullOrWhiteSpace(c.Nick) && GroupEquals(c, item.Value.Group)).GroupBy(c => c.Nick, System.StringComparer.InvariantCultureIgnoreCase).FirstOrDefault()?.Key : nick.Nick;

                var argAttr = new ConcurrentLinkedList<ArgumentAttribute>();//argAttrChild.Count

                var path2 = $"{item.Value.OnlyName}.{path}";

                argAttrChild.ForEach(c =>
                {
                    var attr = string.IsNullOrWhiteSpace(c.Group) ? c.Clone() : c;

                    attr.Meta.Method = item.Value.OnlyName;
                    attr.Meta.Member = path2;

                    attr.Meta.Business = c.Meta.Business;
                    attr.Meta.resultType = c.Meta.resultType;
                    attr.Meta.MemberType = c.Meta.MemberType;
                    attr.Meta.HasProcesIArg = c.Meta.HasProcesIArg;

                    if (string.IsNullOrWhiteSpace(attr.Nick) && !string.IsNullOrWhiteSpace(nickValue))
                    {
                        attr.Nick = nickValue;
                    }

                    attr.BindAfter?.Invoke();

                    argAttr.TryAdd(attr);
                });

                if (default == owner)
                {
                    //add default convert
                    //if (current.hasIArg && 0 == argAttr.Count)
                    if (current.hasIArg && null == argAttr.First.Value)
                    {
                        argAttr.TryAdd(new ArgumentDefaultAttribute(resultType) { Source = AttributeBase.SourceType.Parameter });
                    }
                }

                var group = new ArgGroup(ignores.ToReadOnly(), argAttr, nickValue, path2, default == owner ? item.Value.OnlyName : $"{item.Value.OnlyName}.{owner}");

                if (null != log)
                {
                    group.Logger = GetMetaLogger(log, item.Value.Group);
                }

                if (null != inLog)
                {
                    group.IArgInLogger = GetMetaLogger(inLog, item.Value.Group);
                }

                argGroup.dictionary.TryAdd(item.Key, group);
            }

            return argGroup;
        }

        #endregion
    }

    public class CommandGroup : ConcurrentReadOnlyDictionary<string, ConcurrentReadOnlyDictionary<string, Command>>
    {
        readonly System.Type resultType;

        readonly string groupDefault;

        public CommandGroup(System.Type resultType, string groupDefault)
        {
            this.resultType = resultType;
            this.groupDefault = groupDefault;
        }

        /*
        public virtual IResult Get2(string cmd, string group = null)
        {
            if (System.String.IsNullOrWhiteSpace(cmd))
            {
                return resultType.ResultCreate(0, System.Convert.ToString(Utils.Help.ExceptionWrite(new System.ArgumentException(nameof(cmd)))));
            }

            if (!this.TryGetValue(System.String.IsNullOrWhiteSpace(group) ? Bind.CommandGroupDefault : group, out ConcurrentReadOnlyDictionary<string, Command> cmdGroup))
            {
                return resultType.ResultCreate((int)Request.Mark.MarkItem.Business_GroupError, string.Format(Request.Mark.GroupError, group));
            }

            if (!cmdGroup.TryGetValue(cmd, out Command command))
            {
                return resultType.ResultCreate((int)Request.Mark.MarkItem.Business_CmdError, string.Format(Request.Mark.CmdError, cmd));
            }

            return resultType.ResultCreate(command);
        }
        */

        public virtual Command GetCommand(string cmd, string group = null)
        {
            if (string.IsNullOrEmpty(cmd))
            {
                return null;
            }

            return !TryGetValue(string.IsNullOrWhiteSpace(group) ? groupDefault : group, out ConcurrentReadOnlyDictionary<string, Command> cmdGroup) || !cmdGroup.TryGetValue(cmd, out Command command) ? null : command;

            //if (string.IsNullOrWhiteSpace(group))
            //{
            //    return !this[Bind.CommandGroupDefault].TryGetValue(cmd, out Command command) ? null : command;
            //}
            //else
            //{
            //    return !TryGetValue(group, out ConcurrentReadOnlyDictionary<string, Command> cmdGroup) || !cmdGroup.TryGetValue(cmd, out Command command) ? null : command;
            //}
        }

        #region IRequest

        //public virtual async System.Threading.Tasks.Task<dynamic> AsyncCallUse(IRequest request, object[] useObj, System.Action<dynamic> callback = null)
        //{
        //    return await AsyncCallUse(request.Cmd, request.Group, request.Data, useObj).ContinueWith(c =>
        //    {
        //        callback?.Invoke(c.Result);

        //        return c.Result;
        //    });
        //}

        //public virtual async System.Threading.Tasks.Task<Result> AsyncCallUse<Result>(IRequest request, object[] useObj, System.Action<Result> callback = null) => await AsyncCallUse(request, useObj, c => callback?.Invoke(c));

        //public virtual async System.Threading.Tasks.Task<IResult> AsyncIResultUse(IRequest request, object[] useObj, System.Action<IResult> callback = null) => await AsyncCallUse(request, useObj, c => callback?.Invoke(c));

        #endregion

        #region AsyncCallUse
        //, System.Action<Result> callback = null
        //, c => callback?.Invoke(c)
        public virtual async System.Threading.Tasks.Task<Result> AsyncCallUse<Result>(string cmd, string group = null, object[] args = null, params object[] useObj) => await AsyncCallUse(cmd, group, args, useObj);

        public virtual async System.Threading.Tasks.Task<IResult> AsyncIResultUse(string cmd, string group = null, object[] args = null, params object[] useObj) => await AsyncCallUse(cmd, group, args, useObj);

        public virtual async System.Threading.Tasks.Task<dynamic> AsyncCallUse(string cmd, string group = null, object[] args = null, params object[] useObj)
        {
            var command = GetCommand(cmd, group);

            return null == command ? await System.Threading.Tasks.Task.FromResult(Bind.CmdError(resultType, cmd)) : await command.AsyncCallUse(args, useObj);

            //return await command.AsyncCallUse(args, useObj).ContinueWith(c =>
            //{
            //    callback?.Invoke(c.Result);

            //    return c.Result;
            //});
        }

        #endregion

        #region AsyncCallGroup

        public virtual async System.Threading.Tasks.Task<dynamic> AsyncCallGroup(string cmd, string group, params object[] args)
        {
            var command = GetCommand(cmd, group);

            if (null == command)
            {
                return await System.Threading.Tasks.Task.FromResult(Bind.CmdError(resultType, cmd));
            }

            return await command.AsyncCall(args);
        }

        public virtual async System.Threading.Tasks.Task<Result> AsyncCallGroup<Result>(string cmd, string group, params object[] args) => await AsyncCallGroup(cmd, group, args);

        public virtual async System.Threading.Tasks.Task<IResult> AsyncIResultGroup(string cmd, string group, params object[] args) => await AsyncCallGroup(cmd, group, args);

        #endregion

        #region AsyncCall

        public virtual async System.Threading.Tasks.Task<dynamic> AsyncCall(string cmd, params object[] args) => await AsyncCallGroup(cmd, null, args);

        public virtual async System.Threading.Tasks.Task<Result> AsyncCall<Result>(string cmd, params object[] args) => await AsyncCall(cmd, args);

        public virtual async System.Threading.Tasks.Task<IResult> AsyncIResult(string cmd, params object[] args) => await AsyncCall(cmd, args);

        #endregion

        #region CallUse

        public virtual Result CallUse<Result>(string cmd, string group = null, object[] args = null, params object[] useObj) => CallUse(cmd, group, args, useObj);

        public virtual IResult CallIResultUse(string cmd, string group = null, object[] args = null, params object[] useObj) => CallUse(cmd, group, args, useObj);

        public virtual dynamic CallUse(string cmd, string group = null, object[] args = null, params object[] useObj)
        {
            var command = GetCommand(cmd, group);

            return null == command ? Bind.CmdError(resultType, cmd) : command.CallUse(args, useObj);
        }

        #endregion

        #region CallGroup

        public virtual dynamic CallGroup(string cmd, string group, params object[] args)
        {
            var command = GetCommand(cmd, group);

            return null == command ? Bind.CmdError(resultType, cmd) : command.Call(args);
        }

        public virtual Result CallGroup<Result>(string cmd, string group, params object[] args) => CallGroup(cmd, group, args);

        public virtual IResult CallIResultGroup(string cmd, string group, params object[] args) => CallGroup(cmd, group, args);

        #endregion

        #region Call

        public virtual dynamic Call(string cmd, params object[] args) => CallGroup(cmd, null, args);

        public virtual Result Call<Result>(string cmd, params object[] args) => Call(cmd, args);

        public virtual IResult CallIResult(string cmd, params object[] args) => Call(cmd, args);

        #endregion
    }

    public class Command
    {
        public Command(System.Func<object[], dynamic> call, MetaData meta, string key)
        {
            this.call = call;
            this.Meta = meta;
            this.Key = key;
        }

        //===============member==================//
        readonly System.Func<object[], dynamic> call;

        #region CallUse

        public virtual dynamic CallUse(object[] args, params object[] useObj) => Call(GetAgs(args, useObj));

        public virtual Result CallUse<Result>(object[] args, params object[] useObj) => CallUse(args, useObj);

        public virtual IResult CallIResultUse(object[] args, params object[] useObj) => CallUse(args, useObj);

        #endregion

        public virtual dynamic Call(params object[] args)
        {
            try
            {
                return call(args);
            }
            catch (System.Exception ex)
            {
                return ResultFactory.ResultCreate(Meta.ResultType, 0, System.Convert.ToString(Help.ExceptionWrite(ex)));
            }
        }
        public virtual Result Call<Result>(params object[] args) => Call(args);
        public virtual IResult CallIResult(params object[] args) => Call(args);

        //#if Standard
        //        public virtual async System.Threading.Tasks.Task<dynamic> AsyncCall(params object[] args) => await System.Threading.Tasks.Task.Factory.StartNew(obj => { var obj2 = (dynamic)obj; return obj2.call(obj2.args); }, new { call, args });
        //#else

        public virtual object[] GetAgs(object[] args, params object[] useObj)
        {
            var args2 = new object[Meta.Args.Count];

            if (0 < args2.Length)
            {
                int l = 0;
                for (int i = 0; i < args2.Length; i++)
                {
                    if (Meta.UseTypePosition.ContainsKey(i))
                    {
                        if (null != useObj && 0 < useObj.Length)
                        {
                            var arg = Meta.Args[i];

                            if (arg.Use?.ParameterName ?? false)
                            {
                                var item = useObj.FirstOrDefault(c => c.GetType().Equals(UseEntry.Type) && ((UseEntry)c).Name == arg.Name);

                                if (!Equals(null, item))
                                {
                                    args2[i] = ((UseEntry)item).Value;
                                }
                            }
                            else
                            {
                                var item = useObj.FirstOrDefault(c => Meta.UseTypePosition[i].IsAssignableFrom(c.GetType()));

                                if (!Equals(null, item))
                                {
                                    args2[i] = item;
                                }
                            }
                        }

                        continue;
                    }

                    if (null != args && 0 < args.Length)
                    {
                        if (args.Length < l++)
                        {
                            break;
                        }

                        if (l - 1 < args.Length)
                        {
                            args2[i] = args[l - 1];
                        }
                    }
                }
            }

            return args2;
        }

        #region AsyncCallUse

        public virtual async System.Threading.Tasks.Task<dynamic> AsyncCallUse(object[] args, params object[] useObj) => await AsyncCall(GetAgs(args, useObj));

        public virtual async System.Threading.Tasks.Task<Result> AsyncCallUse<Result>(object[] args, params object[] useObj) => await AsyncCallUse(args, useObj);

        public virtual async System.Threading.Tasks.Task<IResult> AsyncIResultUse(object[] args, params object[] useObj) => await AsyncCallUse(args, useObj);

        #endregion

        public virtual async System.Threading.Tasks.Task<dynamic> AsyncCall(params object[] args)
        {
            try
            {
                if (Meta.HasAsync)
                {
                    //return this.HasIResult ? await (call(args) as System.Threading.Tasks.Task<IResult>) : await (call(args) as System.Threading.Tasks.Task<dynamic>);
                    return await call(args);
                }
                else
                {
                    using (var task = System.Threading.Tasks.Task.Factory.StartNew(obj => { var obj2 = (dynamic)obj; return obj2.call(obj2.args); }, new { call, args })) { return await task; }
                }
            }
            catch (System.Exception ex)
            {
                return await System.Threading.Tasks.Task.FromResult(ResultFactory.ResultCreate(Meta.ResultType, 0, System.Convert.ToString(Help.ExceptionWrite(ex))));
            }
        }

        public virtual async System.Threading.Tasks.Task<Result> AsyncCall<Result>(params object[] args) => await AsyncCall(args);

        public virtual async System.Threading.Tasks.Task<IResult> AsyncIResult(params object[] args) => await AsyncCall(args);

        public readonly string Key;

        public MetaData Meta { get; private set; }
    }
}

namespace Business.Meta
{
    using Business.Utils;
    using System.Reflection;

    #region Meta

    public struct ArgGroup
    {
        public ArgGroup(ReadOnlyCollection<Attributes.Ignore> ignore, ConcurrentLinkedList<Attributes.ArgumentAttribute> attrs, string nick, string path, string owner)
        {
            Ignore = ignore;
            Attrs = attrs;
            Path = path;
            Nick = nick;
            Owner = owner;
            Logger = default;
            IArgInLogger = default;
        }

        public ReadOnlyCollection<Attributes.Ignore> Ignore { get; private set; }

        public ConcurrentLinkedList<Attributes.ArgumentAttribute> Attrs { get; private set; }

        public string Nick { get; private set; }

        public string Path { get; private set; }

        public string Owner { get; private set; }

        public MetaLogger Logger { get; internal set; }

        public MetaLogger IArgInLogger { get; internal set; }
    }

    /// <summary>
    /// Argument
    /// </summary>
    public class Args
    {
        //public override string ToString() => string.Format("{0} {1}", Group2, Name);

        //argChild
        public Args(string name, System.Type type, int position, object defaultValue, bool hasDefaultValue, Accessor accessor, ConcurrentReadOnlyDictionary<string, ArgGroup> group, ReadOnlyCollection<Args> argAttrChild, bool hasDefinition, bool hasIArg, System.Type iArgOutType, System.Type iArgInType, Attributes.UseAttribute use, bool useType, string methodTypeFullName, string argTypeFullName, ArgTypeCode argType)
        {
            Name = name;
            Type = type;
            Position = position;
            //HasString = hasString;
            Accessor = accessor;
            Group = group;
            ArgAttrChild = argAttrChild;
            HasDefinition = hasDefinition;
            HasIArg = hasIArg;
            IArgOutType = iArgOutType;
            IArgInType = iArgInType;
            //this.trim = trim;
            //Path = path;
            //Source = source;

            DefaultValue = defaultValue;// type.GetTypeInfo().IsValueType ? System.Activator.CreateInstance(type) : null;
            HasDefaultValue = hasDefaultValue;
            //Logger = default;
            //IArgInLogger = default;
            //Group2 = group2;
            //Owner = owner;
            //Ignore = ignore;
            Use = use;
            UseType = useType;
            MethodTypeFullName = methodTypeFullName;
            ArgTypeFullName = argTypeFullName;
            ArgType = argType;
        }

        //===============name==================//
        public string Name { get; private set; }
        //===============type==================//
        public System.Type Type { get; private set; }
        //===============position==================//
        public int Position { get; private set; }
        //===============defaultValue==================//
        public object DefaultValue { get; private set; }
        //===============hasDefaultValue==================//
        public bool HasDefaultValue { get; private set; }
        //===============accessor==================//
        public Accessor Accessor { get; private set; }
        //===============group==================//
        public ConcurrentReadOnlyDictionary<string, ArgGroup> Group { get; private set; }
        ////===============argAttr==================//
        //public SafeList<Attributes.ArgumentAttribute> ArgAttr { get; private set; }
        //===============argAttrChild==================//
        public ReadOnlyCollection<Args> ArgAttrChild { get; private set; }
        //===============hasDefinition==================//
        public bool HasDefinition { get; private set; }
        //===============iArgOutType==================//
        public System.Type IArgOutType { get; private set; }
        //===============iArgInType==================//
        public System.Type IArgInType { get; private set; }
        ////==============path===================//
        //public string Path { get; private set; }
        ////==============source===================//
        //public string Source { get; private set; }
        //===============hasIArg==================//
        public bool HasIArg { get; private set; }
        //public MetaLogger Logger { get; private set; }
        //public MetaLogger IArgInLogger { get; private set; }
        //==============group===================//
        //public string Group2 { get; private set; }
        //==============owner===================//
        //public string Owner { get; private set; }
        ////==============ignore===================//
        //public ReadOnlyCollection<Attributes.Ignore> Ignore { get; private set; }
        //==============use===================//
        public Attributes.UseAttribute Use { get; internal set; }
        //==============useType===================//
        public bool UseType { get; internal set; }
        //==============methodTypeFullName===================//
        public string MethodTypeFullName { get; private set; }
        //==============argTypeFullName===================//
        public string ArgTypeFullName { get; private set; }
        //==============argType===================//
        /// <summary>
        /// xml using
        /// </summary>
        public ArgTypeCode ArgType { get; private set; }

        public enum ArgTypeCode
        {
            No,
            Definition,
            Field,
            Property,
        }
    }

    public struct MetaLogger
    {
        public Attributes.LoggerAttribute Record { get; set; }
        public Attributes.LoggerAttribute Exception { get; set; }
        public Attributes.LoggerAttribute Error { get; set; }
    }

    public struct MetaData
    {
        public override string ToString() => Name;

        /// <summary>
        /// MetaData
        /// </summary>
        /// <param name="commandGroup"></param>
        /// <param name="args"></param>
        /// <param name="iArgs"></param>
        /// <param name="metaLogger"></param>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="fullName"></param>
        /// <param name="returnType"></param>
        /// <param name="resultType"></param>
        /// <param name="defaultValue"></param>
        /// <param name="attributes"></param>
        /// <param name="position"></param>
        /// <param name="groupDefault"></param>
        /// <param name="useTypePosition"></param>
        /// <param name="methodTypeFullName"></param>
        public MetaData(ConcurrentReadOnlyDictionary<string, Attributes.CommandAttribute> commandGroup, ReadOnlyCollection<Args> args, ReadOnlyCollection<Args> iArgs, ConcurrentReadOnlyDictionary<string, MetaLogger> metaLogger, string path, string name, string fullName, TypeInfo returnType, System.Type resultType, object[] defaultValue, System.Collections.Generic.List<Attributes.AttributeBase> attributes, int position, string groupDefault, ConcurrentReadOnlyDictionary<int, System.Type> useTypePosition, string methodTypeFullName)
        {
            CommandGroup = commandGroup;
            Args = args;
            IArgs = iArgs;
            MetaLogger = metaLogger;
            Path = path;
            Name = name;
            FullName = fullName;

            //this.returnType = returnType;
            //this.hasAsync = Utils.Help.IsAssignableFrom(typeof(System.Threading.Tasks.Task<>).GetTypeInfo(), returnType, out System.Type[] arguments) || typeof(System.Threading.Tasks.Task).IsAssignableFrom(returnType);
            //typeof(void) != method.ReturnType
            HasAsync = Help.IsAssignableFrom(typeof(System.Threading.Tasks.Task<>).GetTypeInfo(), returnType, out System.Type[] arguments) || returnType == typeof(System.Threading.Tasks.Task);
            HasReturn = !(typeof(void) == returnType || (HasAsync && null == arguments));
            //typeof(IResult).IsAssignableFrom(method.ReturnType),
            //typeof(System.Object).Equals(method.ReturnType)
            var hasGeneric = HasAsync && null != arguments;
            HasIResult = typeof(Result.IResult).IsAssignableFrom(hasGeneric ? arguments[0] : returnType);
            HasObject = typeof(object).Equals(hasGeneric ? arguments[0] : returnType);
            ReturnType = hasGeneric ? arguments[0] : returnType;
            ResultType = resultType;
            DefaultValue = defaultValue;
            //this.logAttrs = logAttrs;
            Attributes = attributes;
            Position = position;
            GroupDefault = groupDefault;
            //ArgsFirst = argsFirst;
            UseTypePosition = useTypePosition;
            MethodTypeFullName = methodTypeFullName;
            //Ignore = ignore;
        }

        //==============commandAttr===================//
        public ConcurrentReadOnlyDictionary<string, Attributes.CommandAttribute> CommandGroup { get; private set; }
        //==============argAttrs===================//
        public ReadOnlyCollection<Args> Args { get; private set; }
        //==============iArgs===================//
        public ReadOnlyCollection<Args> IArgs { get; private set; }
        //==============MetaLogger===================//
        public ConcurrentReadOnlyDictionary<string, MetaLogger> MetaLogger { get; private set; }
        //==============path===================//
        public string Path { get; private set; }
        //==============name===================//
        public string Name { get; private set; }
        //==============fullName===================//
        public string FullName { get; private set; }
        //==============hasReturn===================//
        public bool HasReturn { get; private set; }
        //==============hasIResult===================//
        public bool HasIResult { get; private set; }
        //==============hasObject===================//
        public bool HasObject { get; private set; }
        //==============returnType===================//
        public System.Type ReturnType { get; private set; }
        //==============resultType===================//
        public System.Type ResultType { get; private set; }
        //==============hasAsync===================//
        public bool HasAsync { get; private set; }
        //==============defaultValue===================//
        public object[] DefaultValue { get; private set; }
        //==============attributes===================//
        public System.Collections.Generic.List<Attributes.AttributeBase> Attributes { get; private set; }
        //===============position==================//
        public int Position { get; private set; }
        //==============groupDefault===================//
        public string GroupDefault { get; private set; }
        ////==============argsFirst===================//
        //public ReadOnlyCollection<Args> ArgsFirst { get; private set; }
        //==============useTypesPosition===================//
        public ConcurrentReadOnlyDictionary<int, System.Type> UseTypePosition { get; private set; }
        //==============methodTypeFullName===================//
        public string MethodTypeFullName { get; private set; }
        ////==============ignore===================//
        //public Attributes.Ignore Ignore { get; private set; }
    }

    /*
    public interface ILocalArgs<T> { System.Collections.Generic.IList<T> ArgAttrChild { get; set; } }

    public struct LocalArgs : ILocalArgs<LocalArgs>
    {
        public LocalArgs(System.String name, System.String type, System.Int32 position, System.Object defaultValue, System.Boolean hasDefaultValue, System.Collections.Generic.IReadOnlyList<LocalAttribute> argAttr, System.Collections.Generic.IList<LocalArgs> argAttrChild, System.Boolean hasDeserialize, System.Boolean hasIArg, LocalLogger metaLogger, System.String fullName, string group, string owner)
        {
            this.name = name;
            this.type = type;
            this.position = position;
            this.defaultValue = defaultValue;
            this.hasDefaultValue = hasDefaultValue;
            this.argAttr = argAttr;
            this.argAttrChild = argAttrChild;
            this.hasDeserialize = hasDeserialize;
            this.hasIArg = hasIArg;
            this.metaLogger = metaLogger;
            this.fullName = fullName;
            this.group = group;
            this.owner = owner;
        }

        //===============name==================//
        readonly System.String name;
        public System.String Name { get => name; }
        //===============type==================//
        readonly System.String type;
        public System.String Type { get => type; }
        //===============position==================//
        readonly System.Int32 position;
        public System.Int32 Position { get => position; }
        //===============defaultValue==================//
        readonly System.Object defaultValue;
        public System.Object DefaultValue { get => defaultValue; }
        //===============hasDefaultValue==================//
        readonly System.Boolean hasDefaultValue;
        public System.Boolean HasDefaultValue { get => hasDefaultValue; }
        //===============argAttr==================//
        readonly System.Collections.Generic.IReadOnlyList<LocalAttribute> argAttr;
        public System.Collections.Generic.IReadOnlyList<LocalAttribute> ArgAttr { get => argAttr; }
        //===============argAttrChild==================//
        System.Collections.Generic.IList<LocalArgs> argAttrChild;
        public System.Collections.Generic.IList<LocalArgs> ArgAttrChild { get => argAttrChild; set => argAttrChild = value; }
        //===============hasDeserialize==================//
        readonly System.Boolean hasDeserialize;
        public System.Boolean HasDeserialize { get => hasDeserialize; }
        //===============hasIArg==================//
        readonly System.Boolean hasIArg;
        public System.Boolean HasIArg { get => hasIArg; }
        //==============fullName===================//
        readonly System.String fullName;
        public System.String FullName { get => fullName; }
        //==============logAttr===================//
        readonly LocalLogger metaLogger;
        public LocalLogger MetaLogger { get => metaLogger; }
        //==============group===================//
        readonly string group;
        public string Group { get => group; }
        //==============owner===================//
        readonly string owner;
        public string Owner { get => owner; }
    }

    public struct LocalLogger
    {
        public LocalLogger(LoggerAttribute record, LoggerAttribute exception, LoggerAttribute error)
        {
            this.record = record;
            this.exception = exception;
            this.error = error;
        }

        readonly LoggerAttribute record;
        public LoggerAttribute Record { get => record; }

        readonly LoggerAttribute exception;
        public LoggerAttribute Exception { get => exception; }

        readonly LoggerAttribute error;
        public LoggerAttribute Error { get => error; }

        public class LoggerAttribute
        {
            public LoggerAttribute(LoggerType logType, bool canWrite, Attributes.LoggerValueMode canValue, bool canResult)
            {
                this.logType = logType;
                this.canWrite = canWrite;
                this.canValue = canValue;
                this.canResult = canResult;
            }

            readonly LoggerType logType;
            /// <summary>
            /// Record type
            /// </summary>
            public LoggerType LogType
            {
                get { return logType; }
            }

            readonly bool canWrite;
            /// <summary>
            /// Allow record
            /// </summary>
            public bool CanWrite { get => canWrite; }

            readonly Attributes.LoggerValueMode canValue;
            /// <summary>
            /// Allowed to return to parameters
            /// </summary>
            public Attributes.LoggerValueMode CanValue { get => canValue; }

            readonly bool canResult;
            /// <summary>
            /// Allowed to return to results
            /// </summary>
            public bool CanResult { get => canResult; }
        }
    }

    public struct LocalAttribute
    {
        public LocalAttribute(string type, bool allowMultiple = false, bool inherited = false)
        {
            this.type = type;
            this.allowMultiple = allowMultiple;
            this.inherited = inherited;
        }

        readonly string type;
        public System.String Type { get => type; }

        readonly bool allowMultiple;
        /// <summary>
        /// Is it possible to specify attributes for multiple instances for a program element
        /// </summary>
        public bool AllowMultiple { get => allowMultiple; }

        readonly bool inherited;
        /// <summary>
        /// Determines whether the attributes indicated by the derived class and the overridden member are inherited
        /// </summary>
        public bool Inherited { get => inherited; }
    }

    public struct Local<T>
    {
        /// <summary>
        /// Json
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Utils.Help.JsonSerialize(this);

        public Local(string name, string returnType, LocalLogger metaLogger, System.Collections.Generic.IList<LocalAttribute> attributes, System.Collections.Generic.List<T> args, int position, string path, string group, string onlyName)
        {
            this.name = name;
            this.returnType = returnType;
            this.metaLogger = metaLogger;
            this.attributes = attributes as System.Collections.Generic.IReadOnlyList<LocalAttribute>;
            this.args = args;
            this.position = position;
            this.path = path;
            this.group = group;
            this.onlyName = onlyName;
        }

        readonly string name;
        public string Name { get => name; }

        readonly string returnType;
        public string ReturnType { get => returnType; }

        readonly LocalLogger metaLogger;
        public LocalLogger MetaLogger { get => metaLogger; }

        readonly System.Collections.Generic.IReadOnlyList<LocalAttribute> attributes;
        public System.Collections.Generic.IReadOnlyList<LocalAttribute> Attributes { get => attributes; }

        readonly System.Collections.Generic.List<T> args;
        public System.Collections.Generic.List<T> Args { get => args; }

        //===============position==================//
        readonly System.Int32 position;
        public System.Int32 Position { get => position; }

        //==============path===================//
        readonly string path;
        public string Path { get => path; }

        //==============group===================//
        readonly string group;
        public string Group { get => group; }

        //==============onlyName===================//
        readonly string onlyName;
        public string OnlyName { get => onlyName; }
    }
    */

    #endregion
}
