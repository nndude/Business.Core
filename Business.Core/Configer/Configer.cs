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
    using Business.Utils;
    using Business.Meta;
    using System.Linq;
    /*
    #region ConfigJson

    public struct ConfigJson
    {
        public static implicit operator ConfigJson(string value) => Utils.Help.TryJsonDeserialize<ConfigJson>(value);

        public override string ToString() => Utils.Help.JsonSerialize(this);

        public ConfigJson(System.Collections.Generic.List<ConfigAttribute> attributes)
        {
            this.attributes = attributes;
            this.group = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<ConfigAttribute>>>();

            if (0 < this.attributes.Count)
            {
                var business = this.attributes.GroupBy(c => c.Target.Business, System.StringComparer.CurrentCultureIgnoreCase);

                foreach (var item in business)
                {
                    var method = item.GroupBy(c => c.Target.Method, System.StringComparer.CurrentCultureIgnoreCase);
                    this.group.Add(item.Key, method.ToDictionary(c => c.Key, c => c.ToList()));
                }
            }
        }

        readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<ConfigAttribute>>> group;
        [Newtonsoft.Json.JsonIgnore]
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<ConfigAttribute>>> Group { get => group; }

        readonly System.Collections.Generic.List<ConfigAttribute> attributes;
        public System.Collections.Generic.List<ConfigAttribute> Attributes { get => attributes; }

        public struct ConfigAttribute
        {
            public override string ToString() => string.Format("{0}->{1}", Target.ToString(), Attribute);

            public ConfigAttribute(ConfigTarget target, string group, string attribute, System.Collections.Generic.Dictionary<string, object> member)
            {
                this.target = target;
                this.group = !System.String.IsNullOrWhiteSpace(group) ? group.Trim() : Bind.CommandGroupDefault;
                this.attribute = !System.String.IsNullOrWhiteSpace(attribute) ? attribute.Trim() : System.String.Empty;
                this.member = member;
            }

            readonly ConfigTarget target;
            public ConfigTarget Target { get => target; }

            readonly string group;
            public System.String Group { get => group; }

            readonly string attribute;
            public System.String Attribute { get => attribute; }

            readonly System.Collections.Generic.Dictionary<string, object> member;
            public System.Collections.Generic.Dictionary<string, object> Member { get => member; }
        }

        public struct ConfigTarget
        {
            public static implicit operator ConfigTarget(string value) => new ConfigTarget(value);

            public override string ToString() => this.target;

            const char separator = '.';
            public ConfigTarget(string target)
            {
                this.target = business = method = parameter = property = System.String.Empty;
                if (System.String.IsNullOrWhiteSpace(target)) { return; }

                var sp = target.Split(separator);
                if (!System.String.IsNullOrWhiteSpace(sp[0])) { business = sp[0].Trim(); this.target += business; }

                if (1 < sp.Length)
                {
                    if (!System.String.IsNullOrWhiteSpace(sp[1])) { method = sp[1].Trim(); this.target += string.Format("{0}{1}", separator, method); }

                    if (2 < sp.Length)
                    {
                        if (!System.String.IsNullOrWhiteSpace(sp[2])) { parameter = sp[2].Trim(); this.target += string.Format("{0}{1}", separator, parameter); }

                        if (3 < sp.Length)
                        {
                            if (!System.String.IsNullOrWhiteSpace(sp[3])) { property = sp[3].Trim(); this.target += string.Format("{0}{1}", separator, property); }
                        }
                    }
                }
            }

            readonly string target;

            readonly string business;
            public string Business { get => business; }

            readonly string method;
            public string Method { get => method; }

            readonly string parameter;
            public string Parameter { get => parameter; }

            readonly string property;
            public string Property { get => property; }
        }
    }

    #endregion

#if !Mobile
    public partial class Configuration
    {
        public Configuration(bool enableWatcher)
        {
#if !Mobile
            if (enableWatcher && !CfgWatcher.EnableRaisingEvents)
            {
                CfgWatcher.EnableRaisingEvents = true;
            }
#endif
            this.enableWatcher = enableWatcher;
        }

#if !Mobile
    #region set business attribute

        static void SetBusinessAttribute(System.Collections.Generic.IReadOnlyList<Attributes.AttributeBase> attributes, ConcurrentReadOnlyDictionary<string, MetaData> metaData, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<ConfigJson.ConfigAttribute>> configAttributes)
        {
            foreach (var item in configAttributes)
            {
                if (0 == item.Value.Count) { continue; }

                switch (item.Key)
                {
                    //business
                    case "":
                        foreach (var item2 in item.Value)
                        {
                            var attribute = attributes.FirstOrDefault(c => c.TypeFullName.Equals(item2.Attribute, System.StringComparison.CurrentCultureIgnoreCase));

                            if (null == attribute) { continue; }

                            foreach (var item3 in item2.Member)
                            {
                                attribute[item3.Key] = item3.Value;
                            }
                        }
                        break;
                    //all method
                    case Any:
                        foreach (var item2 in metaData)
                        {
                            SetMethodAttribute(item2.Value, item.Value);
                        }
                        break;
                    //method
                    default:
                        if (!metaData.TryGetValue(item.Key, out MetaData method)) { break; }
                        SetMethodAttribute(method, item.Value);
                        break;
                }
            }
        }

        static void SetMethodAttribute(MetaData method, System.Collections.Generic.List<ConfigJson.ConfigAttribute> item)
        {
            var methodAttrsGroup = item.GroupBy(c => c.Target.Parameter);

            foreach (var item2 in methodAttrsGroup)
            {
                switch (item2.Key)
                {
                    //method
                    case "":
                        foreach (var configAttribute in item2)
                        {
                            var attribute = method.Attributes.FirstOrDefault(c => c.TypeFullName.Equals(configAttribute.Attribute, System.StringComparison.CurrentCultureIgnoreCase));

                            if (null == attribute)
                            {
                                continue;
                            }

                            foreach (var member in configAttribute.Member)
                            {
                                attribute[member.Key] = member.Value;
                            }
                        }
                        break;
                    case Any: //any parameter
                        foreach (var configAttribute in item2)
                        {
                            if (!method.ArgAttrs.TryGetValue(Bind.GetCommandGroup(configAttribute.Group, method.Name), out ArgAttrs argAttrs))
                            {
                                continue;
                            }

                            foreach (var item3 in argAttrs.Args)
                            {
                                SetParameterAttribute(item3, configAttribute);
                            }
                        }
                        break;
                    //parameter
                    default:
                        foreach (var configAttribute in item2)
                        {
                            if (!method.ArgAttrs.TryGetValue(Bind.GetCommandGroup(configAttribute.Group, method.Name), out ArgAttrs argAttrs))
                            {
                                continue;
                            }

                            var parameter = argAttrs.Args.FirstOrDefault(c => c.Name.Equals(item2.Key, System.StringComparison.CurrentCultureIgnoreCase));

                            if (default(Args).Equals(parameter))
                            {
                                continue;
                            }

                            SetParameterAttribute(parameter, configAttribute);
                        }
                        break;
                }
            }
        }

        static void SetParameterAttribute(Args args, ConfigJson.ConfigAttribute configAttribute)
        {
            switch (configAttribute.Target.Property)
            {
                case "":
                    {
                        //parameter
                        var attribute = args.ArgAttr.FirstOrDefault(c => c.TypeFullName.Equals(configAttribute.Attribute, System.StringComparison.CurrentCultureIgnoreCase));

                        if (null == attribute)
                        {
                            break;
                        }

                        foreach (var member in configAttribute.Member)
                        {
                            attribute[member.Key] = member.Value;
                        }
                    }
                    break;
                // any property
                case Any:
                    {
                        foreach (var item in args.ArgAttrChild)
                        {
                            var attribute = item.ArgAttr.FirstOrDefault(c => c.TypeFullName.Equals(configAttribute.Attribute, System.StringComparison.CurrentCultureIgnoreCase));

                            if (null == attribute)
                            {
                                continue;
                            }

                            foreach (var member in configAttribute.Member)
                            {
                                attribute[member.Key] = member.Value;
                            }
                        }
                    }
                    break;
                //property
                default:
                    {
                        var property = args.ArgAttrChild.FirstOrDefault(c => c.Name.Equals(configAttribute.Target.Property, System.StringComparison.CurrentCultureIgnoreCase));

                        if (default(Args).Equals(property))
                        {
                            break;
                        }

                        var attribute = property.ArgAttr.FirstOrDefault(c => c.TypeFullName.Equals(configAttribute.Attribute, System.StringComparison.CurrentCultureIgnoreCase));

                        if (null == attribute)
                        {
                            break;
                        }

                        foreach (var member in configAttribute.Member)
                        {
                            attribute[member.Key] = member.Value;
                        }
                    }
                    break;
            }
        }

    #endregion
#endif

    #region Static

        /// <summary>
        /// *
        /// </summary>
        const string Any = "*";

        static readonly string CfgPath = System.IO.Path.Combine(Help.BaseDirectory, "business.json");
        static System.DateTime cfgLastTime = System.DateTime.MinValue;

        static readonly System.Threading.ReaderWriterLockSlim locker = new System.Threading.ReaderWriterLockSlim();
        internal static readonly System.IO.FileSystemWatcher CfgWatcher = new System.IO.FileSystemWatcher(System.IO.Path.GetDirectoryName(CfgPath)) { InternalBufferSize = 4096, Filter = System.IO.Path.GetFileName(CfgPath), NotifyFilter = System.IO.NotifyFilters.LastWrite | System.IO.NotifyFilters.FileName };

        static Configuration()
        {
            CfgWatcher.Renamed += Changed;
            CfgWatcher.Changed += Changed;
        }

        static ConfigJson GetConfig()
        {
            if (!System.IO.File.Exists(CfgPath)) { return default(ConfigJson); }

            if (!locker.TryEnterReadLock(300)) { return default(ConfigJson); }

            try
            {
                return Help.FileReadString(CfgPath);
            }
            catch
            {
                return default(ConfigJson);
            }
            finally { locker.ExitReadLock(); }
        }

        static void Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            if (!e.FullPath.Equals(CfgPath, System.StringComparison.CurrentCultureIgnoreCase)) { return; }

            var lastTime = System.IO.File.GetLastWriteTime(e.FullPath);

            if (0 <= cfgLastTime.CompareTo(lastTime)) { return; }

            cfgLastTime = lastTime;

            //------------------//

            var dels = Bind.BusinessList.Values.Where(c => c.Configuration.EnableWatcher).Select(c => c.Configuration).ToList();
            if (0 == dels.Count) { return; }

            var config = GetConfig();
            if (default(ConfigJson).Equals(config) || 0 == config.Attributes.Count) { return; }

            foreach (var item in config.Group)
            {
                var dels2 = dels.Where(c => c.info.BusinessName.Equals(item.Key));

                foreach (var del in dels2)
                {
                    SetBusinessAttribute(del.attributes, del.MetaData, item.Value);
                }
            }
        }

    #endregion
    }
#endif
    */
    public partial class Configer
    {
        public static ConcurrentReadOnlyDictionary<string, IBusiness> BusinessList = new ConcurrentReadOnlyDictionary<string, IBusiness>(System.StringComparer.InvariantCultureIgnoreCase);

        public static ConcurrentReadOnlyDictionary<string, Document.Xml> Xmls = new ConcurrentReadOnlyDictionary<string, Document.Xml>();

        //internal readonly string ID = System.Guid.NewGuid().ToString("N");

        public Configer(Attributes.Info info, System.Type resultType, System.Collections.Generic.List<Attributes.AttributeBase> attributes)
        /*
#if !Mobile
        , bool enableWatcher = false)
        : this(enableWatcher)
#else 
        )
#endif
                    */
        {
            this.Info = info;
            this.ResultType = resultType;
            //this.token = token;
            //this.requestType = requestType;
            //this.requestDefault = RequestCreate<object>();
            //this.requestDefault.Configuration = this;
            this.Attributes = attributes.ToReadOnly();
            //this.routes = routes;
            this.UseTypes = new ReadOnlyCollection<string>();

            //GetCommandGroupDefault = name => GetCommandGroup(CommandGroupDefault, name);
        }

        ///// <summary>
        ///// Default
        ///// </summary>
        //public string CommandGroupDefault { get; set; } = "Default";

        //internal System.Func<string, string> GetCommandGroupDefault;

        //internal System.Func<string, string, string> GetCommandGroup = (group, name) => string.Format("{0}.{1}", group, name);

        public virtual string GetCommandGroup(string group, string name) => $"{group}.{name}";

        public ConcurrentReadOnlyDictionary<string, MetaData> MetaData { get; set; }
        public ReadOnlyCollection<Attributes.AttributeBase> Attributes { get; private set; }
        public Attributes.Info Info { get; private set; }
        //public bool EnableWatcher { get; }
        public System.Type ResultType { get; private set; }
        public ReadOnlyCollection<string> UseTypes { get; private set; }
        public Document.Doc Doc { get; internal set; }

        //public Configuration UseType(params System.Type[] type)
        //{
        //    if (null == type) { return this; }

        //    foreach (var item in type)
        //    {
        //        if (null == item || UseTypes.Contains(item.FullName)) { continue; }

        //        UseTypes.collection.Add(item.FullName);
        //    }

        //    return this;
        //}

        //public static void LoadAttributes(System.Collections.Generic.IEnumerable<string> assemblyFiles = null)
        //{
        //    Help.LoadAssemblys((null == assemblyFiles || !assemblyFiles.Any()) ? System.IO.Directory.GetFiles(System.AppContext.BaseDirectory, "*.dll") : assemblyFiles, true, type => { Business.Attributes.AttributeBase.LoadAttributes(type); return false; });
        //}

        //public static void LoadBusiness<Business>()
        //    where Business : class => LoadBusiness(typeof(Business));
        //public static void LoadBusiness(System.Type type) => Bind.Create(type);

        //public static void LoadBusiness(System.Collections.Generic.IEnumerable<string> assemblyFiles = null)
        public static void LoadBusiness(params string[] assemblyFiles)
        {
            var business = Help.LoadAssemblys((null == assemblyFiles || !assemblyFiles.Any()) ? System.IO.Directory.GetFiles(System.AppContext.BaseDirectory, "*.dll") : assemblyFiles, true, type =>
            {
                if (typeof(IBusiness).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    Bind.Create(type);

                    return true;
                }

                return false;
            });
        }

        public static void UseType(params System.Type[] type)
        {
            foreach (var item in BusinessList.Values)
            {
                item.UseType(type);
            }
        }

        public static void UseDoc(string outDir = null)
        {
            var exist = !string.IsNullOrEmpty(outDir) && System.IO.Directory.Exists(outDir);

            foreach (var item in BusinessList.Values)
            {
                item.UseDoc(exist ? System.IO.Path.Combine(outDir, $"{item.Configer.Info.BusinessName}.doc") : null);
            }
        }

        public static void UseDoc<Doc>(System.Func<System.Collections.Generic.Dictionary<string, Document.Xml.member>, Doc> operation, string outDir = null) where Doc : Document.Doc
        {
            var exist = !string.IsNullOrEmpty(outDir) && System.IO.Directory.Exists(outDir);

            foreach (var item in BusinessList.Values)
            {
                item.UseDoc(operation, exist ? System.IO.Path.Combine(outDir, $"{item.Configer.Info.BusinessName}.doc") : null);
            }
        }

        //public static System.Collections.Generic.Dictionary<string, Doc> LoadDoc<Business>()
        //{
        //    var dict = Bind.BusinessList.ToDictionary(c => c.Value.GetType().BaseType.Assembly.Location, c => c.Key, System.StringComparer.InvariantCultureIgnoreCase);

        //    return Bind.BusinessList.AsParallel().ToDictionary(c => c.Key, c =>
        //    {
        //        var ass = c.GetType().BaseType.Assembly;

        //        var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ass.Location), string.Format("{0}.xml", System.IO.Path.GetFileNameWithoutExtension(ass.Location)));

        //        if (!System.IO.File.Exists(path)) { return null; }

        //        var doc = Doc.DeserializeDoc(Help.FileReadString(path));

        //        return doc;
        //    });
        //}
        /*
        public static void LoadAttributeType(System.Collections.Generic.IEnumerable<Help.AssemblyType> assemblyFiles = null)
        {
            if (null == assemblyFiles || !assemblyFiles.Any())
            {
                LoadAttribute();
            }
            else
            {
                Help.LoadAssemblys(assemblyFiles, true, (assembly, types) =>
                {
                    try
                    {
                        LoadAttribute(types.Select(c => assembly.GetType(c)));
                    }
                    catch (System.Exception ex)
                    {
                        System.Console.WriteLine(assembly?.Location);
                        ex.ExceptionWrite(console: true);
                    }
                });
            }
        }
        */

        //readonly System.Collections.Generic.List<Attributes.RouteAttribute> route;
        //public System.Collections.Generic.IReadOnlyList<Attributes.RouteAttribute> Route { get => route; }

        //readonly SocketAttribute socket;
        //public SocketAttribute Socket { get => socket; }

        /*
        public bool EnableWatcher
        {
            get
            {
                return null == CfgChanged ? false : GetCfgChanged().Any(c => ((Config)c).Identity.Equals(this.Identity));
            }
            internal set
            {
                lock (CfgWatcher)
                {
                    if (value)
                    {
                        if (null == CfgChanged || !GetCfgChanged().Any(c => ((Config)c).Identity.Equals(this.Identity)))
                        {
                            CfgChanged += Changed;
                        }

                        if (!CfgWatcher.EnableRaisingEvents)
                        {
                            CfgWatcher.EnableRaisingEvents = true;
                        }
                    }
                    else
                    {
                        if (null == CfgChanged) { return; }

                        CfgChanged -= Changed;

                        if (0 == CfgChanged.GetInvocationList().Length)
                        {
                            if (CfgWatcher.EnableRaisingEvents)
                            {
                                CfgWatcher.EnableRaisingEvents = false;
                            }
                        }
                    }
                }
            }
        }
        */

        //public void Dispose()
        //{
        //    this.attributes.Clear();

        //    if (this.enableWatcher)
        //    {
        //        CfgChanged -= Changed;

        //        var locked = System.Threading.Interlocked.Decrement(ref interlocked);

        //        if (0 == locked)
        //        {
        //            CfgWatcher.EnableRaisingEvents = false;
        //        }
        //    }
        //}
    }
}
