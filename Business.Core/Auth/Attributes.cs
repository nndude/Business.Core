/*==================================
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

namespace Business.Attributes
{
    using Result;
    using Utils;
    using System.Reflection;
    using System.Linq;
    using System.Threading.Tasks;

    #region abstract

    public abstract class AttributeBase : System.Attribute
    {
        #region MetaData

        public static readonly ConcurrentReadOnlyDictionary<string, ConcurrentReadOnlyDictionary<string, Accessor>> MetaData;

        static AttributeBase()
        {
            MetaData = new ConcurrentReadOnlyDictionary<string, ConcurrentReadOnlyDictionary<string, Accessor>>();

#if !Mobile

            Help.LoadAssemblys(Help.BaseDirectory, (assembly, typeInfo) =>
            {
                if (typeInfo.IsSubclassOf(typeof(AttributeBase)) && !typeInfo.IsAbstract)
                {
                    var member = new ConcurrentReadOnlyDictionary<string, Accessor>();

                    foreach (var field in typeInfo.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
                    {
                        var accessor = field.GetAccessor();
                        if (null == accessor.Getter || null == accessor.Setter) { continue; }
                        member.dictionary.TryAdd(field.Name, accessor);
                    }

                    foreach (var property in typeInfo.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
                    {
                        var accessor = property.GetAccessor();
                        if (null == accessor.Getter || null == accessor.Setter) { continue; }
                        member.dictionary.TryAdd(property.Name, accessor);
                    }

                    MetaData.dictionary.GetOrAdd(typeInfo.FullName, member);
                }
            });
#endif
        }

        #endregion

        /// <summary>
        /// Accessor
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public object this[string member]
        {
            get
            {
                if (!MetaData.TryGetValue(this.type.FullName, out ConcurrentReadOnlyDictionary<string, Accessor> meta) || !meta.TryGetValue(member, out Accessor accessor)) { return null; }

                return accessor.Getter(this);
            }
            set
            {
                if (!MetaData.TryGetValue(this.type.FullName, out ConcurrentReadOnlyDictionary<string, Accessor> meta) || !meta.TryGetValue(member, out Accessor accessor)) { return; }

                try
                {
                    var value2 = Help.ChangeType(value, accessor.Type);
                    if (System.Object.Equals(value2, accessor.Getter(this))) { return; }

                    accessor.Setter(this, value2);
                }
                catch { }
            }
        }

        //public virtual bool MemberSet(System.Type type, string value, out object outValue)
        //{
        //    outValue = null;

        //    if (type.IsEnum)
        //    {
        //        if (System.String.IsNullOrWhiteSpace(value)) { return false; }

        //        var enums = System.Enum.GetValues(type).Cast<System.Enum>();
        //        var enumValue = enums.FirstOrDefault(c => value.Equals(c.ToString(), System.StringComparison.CurrentCultureIgnoreCase));
        //        if (null != enumValue)
        //        {
        //            outValue = enumValue;
        //            return true;
        //        }
        //        return false;
        //    }
        //    else
        //    {
        //        switch (type.FullName)
        //        {
        //            case "System.String":
        //                outValue = value;
        //                return true;
        //            case "System.Int16":
        //                if (System.Int16.TryParse(value, out short value2))
        //                {
        //                    outValue = value2;
        //                    return true;
        //                }
        //                return false;
        //            case "System.Int32":
        //                if (System.Int32.TryParse(value, out int value3))
        //                {
        //                    outValue = value3;
        //                    return true;
        //                }
        //                return false;
        //            case "System.Int64":
        //                if (System.Int64.TryParse(value, out long value4))
        //                {
        //                    outValue = value4;
        //                    return true;
        //                }
        //                return false;
        //            case "System.Decimal":
        //                if (System.Decimal.TryParse(value, out decimal value5))
        //                {
        //                    outValue = value5;
        //                    return true;
        //                }
        //                return false;
        //            case "System.Double":
        //                if (System.Double.TryParse(value, out double value6))
        //                {
        //                    outValue = value6;
        //                    return true;
        //                }
        //                return false;
        //            default:
        //                value.ChangeType(type);
        //                System.Convert.ChangeType(value, type);
        //                return false;
        //        }
        //    }
        //}
        /*
        public bool MemberSet(string member, dynamic value)
        {
            if (!MetaData.TryGetValue(this.type.FullName, out System.Collections.Generic.IReadOnlyDictionary<string, Accessor> meta) || !meta.TryGetValue(member, out Accessor accessor)) { return false; }

            try
            {
                var value2 = Help.ChangeType(value, accessor.Type);
                if (System.Object.Equals(value2, accessor.Getter(this)))
                {
                    return false;
                }
                accessor.Setter(this, value2);
                return true;
            }
            catch
            {
                return false;
            }
        }
        */

        public AttributeBase()//bool config = false
        {
            this.type = this.GetType();
            this.typeFullName = this.type.FullName;
            this.basePath = this.type.FullName.Replace('+', '.');

            var usage = this.type.GetTypeInfo().GetAttribute<System.AttributeUsageAttribute>();
            if (null != usage)
            {
                this.allowMultiple = usage.AllowMultiple;
                this.inherited = usage.Inherited;
            }

            //this.config = config;
        }

        #region Property

        readonly System.Type type;
        /// <summary>
        /// Gets the fully qualified type name, including the namespace but not the assembly
        /// </summary>
        public System.Type Type { get => type; }

        readonly System.String typeFullName;
        public System.String TypeFullName { get => typeFullName; }

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

        //bool enable = true;
        //public bool Enable { get => enable; set => enable = value; }

        //readonly bool config;
        //public bool Config { get => config; }

        readonly string basePath;
        public virtual string BasePath { get => basePath; }

        //readonly bool restart;
        //public bool Restart { get => restart; }

        ///// <summary>
        ///// Used for the command group
        ///// </summary>
        //public virtual string Group { get; set; }

        //public virtual string GetKey(string space = "->") => string.Format("{1}{0}{2}", space, this.Group, this.BasePath);

        #endregion

        /// <summary>
        /// Depth Clone
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual T Clone<T>() where T : AttributeBase => this.JsonSerialize().TryJsonDeserialize<T>();
    }

    #region

    /// <summary>
    /// The Method and Property needs to be ignored and will not be a proxy
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Property | System.AttributeTargets.Parameter | System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class IgnoreAttribute : System.Attribute
    {
        /// <summary>
        /// Do you just ignore children? 
        /// </summary>
        public bool HasChild { get; set; }
    }

    /// <summary>
    /// Token
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Parameter | System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public sealed class TokenAttribute : System.Attribute { }
    //[System.AttributeUsage(System.AttributeTargets.Parameter | System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    //public sealed class HttpFileAttribute : System.Attribute { }
    //[System.AttributeUsage(System.AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    //public class HttpRequestAttribute : System.Attribute { }


    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class EnableWatcherAttribute : System.Attribute { }

    /// <summary>
    /// Info
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class InfoAttribute : AttributeBase
    {
        public InfoAttribute(string businessName)
        {
            this.BusinessName = businessName;
            //this.ConfigFileName = System.IO.Path.Combine(Help.BaseDirectory, configFileName);
        }

        public string BusinessName { get; internal set; }

        //public string ConfigFileName { get; internal set; }
    }

    /// <summary>
    /// Socket
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = false, Inherited = true)]
    public class SocketAttribute : AttributeBase
    {
        /// <summary>
        /// Socket server running mode
        /// </summary>
        public enum SocketMode
        {
            /// <summary>
            /// Tcp mode
            /// </summary>
            Tcp = 0,
            /// <summary>
            /// Udp mode
            /// </summary>
            Udp = 1,
        }

        #region Default

        public const string DefaultDescription = "Socket Service";

        /// <summary>
        /// The default ip
        /// </summary>
        public const string DefaultIp = "127.0.0.1";

        /// <summary>
        /// The default port
        /// </summary>
        public const int DefaultPort = 8200;

        /// <summary>
        /// The default socket mode
        /// </summary>
        public const SocketMode DefaultMode = SocketMode.Tcp;

        /// <summary>
        /// The default socket mode
        /// </summary>
        public const string DefaultSecurity = "None";

        /// <summary>
        /// The default security
        /// </summary>
        public const bool DefaultClearIdleSession = true;

        /// <summary>
        /// Default clear idle session interval
        /// </summary>
        public const int DefaultClearIdleSessionInterval = 120;
        /// <summary>
        /// Default idle session timeout
        /// </summary>
        public const int DefaultIdleSessionTimeOut = 300;
        /// <summary>
        /// The default keep alive interval
        /// </summary>
        public const int DefaultKeepAliveInterval = 60;
        /// <summary>
        /// The default keep alive time
        /// </summary>
        public const int DefaultKeepAliveTime = 600;
        ///// <summary>
        ///// The default listen backlog
        ///// </summary>
        //public const int DefaultListenBacklog = 100;
        /// <summary>
        /// Default MaxConnectionNumber
        /// </summary>
        public const int DefaultMaxConnectionNumber = 100;
        /// <summary>
        /// Default MaxRequestLength
        /// </summary>
        public const int DefaultMaxRequestLength = 1024;
        /// <summary>
        /// Default ReceiveBufferSize
        /// </summary>
        public const int DefaultReceiveBufferSize = 4096;
        /// <summary>
        /// The default send buffer size
        /// </summary>
        public const int DefaultSendBufferSize = 2048;
        /// <summary>
        /// Default sending queue size
        /// </summary>
        public const int DefaultSendingQueueSize = 5;
        /// <summary>
        /// Default send timeout value, in milliseconds
        /// </summary>
        public const int DefaultSendTimeout = 5000;
        /// <summary>
        /// The default session snapshot interval
        /// </summary>
        public const int DefaultSessionSnapshotInterval = 5;

        #endregion

        /// <summary>
        /// Initializes a new instance of the socket configuration class
        /// </summary>
        /// <param name="ip">Gets the ip of listener</param>
        /// <param name="port">Gets the port of listener</param>
        /// <param name="mode">Gets/sets the mode.</param>
        /// <param name="security">Gets/sets the security option, None/Default/Tls/Ssl/...</param>
        /// <param name="clearIdleSession"></param>
        /// <param name="clearIdleSessionInterval">clear idle session interval</param>
        /// <param name="idleSessionTimeOut">idle session timeout</param>
        /// <param name="keepAliveInterval">The keep alive interval</param>
        /// <param name="keepAliveTime">The keep alive time</param>
        /// <param name="maxConnectionNumber">max connection number</param>
        /// <param name="maxRequestLength"></param>
        /// <param name="receiveBufferSize"></param>
        /// <param name="sendBufferSize">The send buffer size</param>
        /// <param name="sendingQueueSize">sending queue size</param>
        /// <param name="sendTimeOut">send timeout value, in milliseconds</param>
        /// <param name="sessionSnapshotInterval">The default session snapshot interval</param>
        /// <param name="description"></param>
        [Newtonsoft.Json.JsonConstructor]
        public SocketAttribute(string ip = DefaultIp, int port = DefaultPort, SocketMode mode = DefaultMode, string security = DefaultSecurity, bool clearIdleSession = true, int clearIdleSessionInterval = DefaultClearIdleSessionInterval, int idleSessionTimeOut = DefaultIdleSessionTimeOut, int keepAliveInterval = DefaultKeepAliveInterval, int keepAliveTime = DefaultKeepAliveTime, int maxConnectionNumber = DefaultMaxConnectionNumber, int maxRequestLength = DefaultMaxRequestLength, int receiveBufferSize = DefaultReceiveBufferSize, int sendBufferSize = DefaultSendBufferSize, int sendingQueueSize = DefaultSendingQueueSize, int sendTimeOut = DefaultSendTimeout, int sessionSnapshotInterval = DefaultSessionSnapshotInterval, string description = DefaultDescription)
        {
            this.Ip = ip;
            this.Port = port;
            this.Mode = mode;
            this.Security = security;
            this.ClearIdleSession = clearIdleSession;
            this.ClearIdleSessionInterval = clearIdleSessionInterval;
            this.IdleSessionTimeOut = idleSessionTimeOut;
            this.KeepAliveInterval = keepAliveInterval;
            this.KeepAliveTime = keepAliveTime;
            this.MaxConnectionNumber = maxConnectionNumber;
            this.MaxRequestLength = maxRequestLength;
            this.ReceiveBufferSize = receiveBufferSize;
            this.SendBufferSize = sendBufferSize;
            this.SendingQueueSize = sendingQueueSize;
            this.SendTimeOut = sendTimeOut;
            this.SessionSnapshotInterval = sessionSnapshotInterval;
            this.Description = description;
        }

        /// <summary>
        /// Gets the ip of listener
        /// </summary>
        public string Ip { get; internal set; }

        /// <summary>
        /// Gets the port of listener
        /// </summary>
        public int Port { get; internal set; }

        /// <summary>
        /// Gets/sets the mode.
        /// </summary>
        public SocketMode Mode { get; internal set; }

        /// <summary>
        /// Gets/sets the security option, None/Default/Tls/Ssl/...
        /// </summary>
        public string Security { get; internal set; }

        /// <summary>
        /// Gets/sets a value indicating whether clear idle session.
        /// </summary>
        public bool ClearIdleSession { get; internal set; }

        /// <summary>
        /// Gets/sets the clear idle session interval, in seconds.
        /// </summary>
        public int ClearIdleSessionInterval { get; internal set; }

        /// <summary>
        /// Gets/sets the idle session timeout time length, in seconds.
        /// </summary>
        public int IdleSessionTimeOut { get; internal set; }

        /// <summary>
        /// Gets the max connection number.
        /// </summary>
        public int MaxConnectionNumber { get; internal set; }

        /// <summary>
        /// Gets/sets the length of the max request.
        /// </summary>
        public int MaxRequestLength { get; internal set; }

        /// <summary>
        /// Gets the size of the receive buffer.
        /// </summary>
        public int ReceiveBufferSize { get; internal set; }

        /// <summary>
        /// Gets/sets the keep alive interval, in seconds.
        /// </summary>
        public int KeepAliveInterval { get; internal set; }

        /// <summary>
        /// Gets/sets the start keep alive time, in seconds
        /// </summary>
        public int KeepAliveTime { get; internal set; }

        /// <summary>
        /// Gets the size of the send buffer.
        /// </summary>
        public int SendBufferSize { get; internal set; }

        /// <summary>
        /// Gets/sets the size of the sending queue.
        /// </summary>
        public int SendingQueueSize { get; internal set; }

        /// <summary>
        /// Gets/sets the send time out.
        /// </summary>
        public int SendTimeOut { get; internal set; }

        /// <summary>
        /// Gets/sets the interval to taking snapshot for all live sessions.
        /// </summary>
        public int SessionSnapshotInterval { get; internal set; }

        public string Description { get; internal set; }
    }

    [System.AttributeUsage(System.AttributeTargets.Assembly | System.AttributeTargets.Class | System.AttributeTargets.Method | System.AttributeTargets.Struct | System.AttributeTargets.Parameter, AllowMultiple = true, Inherited = true)]
    public class LoggerAttribute : AttributeBase
    {
        [Newtonsoft.Json.JsonConstructor]
        public LoggerAttribute(LoggerType logType = LoggerType.All, bool canWrite = true)
        {
            this.logType = logType;
            this.canWrite = canWrite;
        }

        LoggerType logType;
        /// <summary>
        /// Record type
        /// </summary>
        public LoggerType LogType { get => logType; }

        bool canWrite;
        /// <summary>
        /// Allow record
        /// </summary>
        public bool CanWrite { get => canWrite; set => canWrite = value; }

        /// <summary>
        /// Allowed to return to parameters
        /// </summary>
        public LoggerValueMode CanValue { get; set; }

        bool canResult = true;
        /// <summary>
        /// Allowed to return to results
        /// </summary>
        public bool CanResult { get => canResult; set => canResult = value; }

        public LoggerAttribute SetLogType(LoggerType logType)
        {
            this.logType = logType;
            return this;
        }

        public override T Clone<T>() => new LoggerAttribute(this.LogType) { CanResult = this.CanResult, CanValue = this.CanValue, CanWrite = this.CanWrite } as T;
    }

    /// <summary>
    /// Record parameter model
    /// </summary>
    public enum LoggerValueMode
    {
        /// <summary>
        /// Allow selective recording of some parameters
        /// </summary>
        All = 0,
        /// <summary>
        /// All parameter Records
        /// </summary>
        Select = 1,
        /// <summary>
        /// No records
        /// </summary>
        No = 2,
    }

    #endregion

    //public struct ArgumentResult
    //{
    //    public ArgumentResult(ArgumentAttribute argAttr)
    //    {
    //        this.argAttr = argAttr;
    //    }

    //    internal readonly ArgumentAttribute argAttr;

    //    public IResult OK { get => argAttr.resultType.ResultCreate(); }

    //    public IResult Error { get => argAttr.resultType.ResultCreate(argAttr.State, argAttr.Message); }
    //}

    /// <summary>
    /// Base class for all attributes that apply to parameters
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct | System.AttributeTargets.Property | System.AttributeTargets.Field | System.AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public abstract class ArgumentAttribute : AttributeBase
    {
        public ArgumentAttribute(int state, string message = null, bool canNull = true)
        {
            //if (-1 < state) { throw new System.ArgumentOutOfRangeException("state"); }

            this.state = state;
            this.message = message;
            this.canNull = canNull;
            //this.Result = new ArgumentResult(this);

            this.BindAfter += () =>
            {
                if (!System.String.IsNullOrWhiteSpace(this.Message))
                {
                    this.Message = this.Replace(this.Message);
                }

                if (!System.String.IsNullOrWhiteSpace(this.Description))
                {
                    this.Description = this.Replace(this.Description);
                }
            };
        }

        public System.Action BindAfter { get; set; }

        internal System.Type resultType;
        public dynamic Business { get; set; }

        public string Method { get; set; }

        public string Member { get; set; }

        public System.Type MemberType { get; set; }

        //public ArgumentResult Result { get; }

        bool canNull;
        /// <summary>
        /// By checking the Allow null value
        /// </summary>
        public bool CanNull { get => canNull; set => canNull = value; }

        int state;
        /// <summary>
        /// Used to return state
        /// </summary>
        public int State { get => state; set => state = value; }

        string message;
        /// <summary>
        /// Used to return error messages
        /// </summary>
        public string Message { get => message; set => message = value; }

        ///// <summary>
        ///// Remove leading or trailing white space characters
        ///// </summary>
        //public bool TrimChar { get; set; }

        /// <summary>
        /// Used for the command group
        /// </summary>
        public string Group { get; set; }

        string description;
        public string Description { get => description; set => description = value; }

        /// <summary>
        /// Amicable name
        /// </summary>
        public string Nick { get; set; }

        ///// <summary>
        ///// 
        ///// </summary>
        //public bool CanRef { get; set; }

        public bool Ignore { get; set; }

        //public string Group { get; set; }

        /// <summary>
        /// Start processing the Parameter object, By this.ResultCreate() method returns
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract Task<IResult> Proces(dynamic value);

        #region Result

        /// <summary>
        /// Used to create the Proces() method returns object
        /// </summary>
        /// <returns></returns>
        //public IResult ResultCreate() => resultType.ResultCreate();

        /// <summary>
        /// Used to create the Proces() method returns object
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public IResult ResultCreate(int state) => resultType.ResultCreate(state);

        /// <summary>
        /// Used to create the Proces() method returns object
        /// </summary>
        /// <param name="state"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public IResult ResultCreate(int state = 1, string message = null) => resultType.ResultCreate(state, message);

        /// <summary>
        /// Used to create the Proces() method returns object
        /// </summary>
        /// <typeparam name="Data"></typeparam>
        /// <param name="data"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public IResult ResultCreate<Data>(Data data, string message = null, int state = 1) => resultType.ResultCreate(data, message, state);

        //public IResult ResultCreate(dynamic data, string message = null, int state = 1) => ResultCreate<dynamic>(data, message, state);

        #endregion

        public string Replace(string value)
        {
            if (System.String.IsNullOrWhiteSpace(value) || !MetaData.TryGetValue(this.TypeFullName, out ConcurrentReadOnlyDictionary<string, Accessor> meta))
            {
                return value;
            }

            var sb = new System.Text.StringBuilder(value);

            foreach (var item in meta)
            {
                var member = string.Format("{{{0}}}", item.Key);

                if (-1 < value.IndexOf(member))
                {
                    sb = sb.Replace(member, System.Convert.ToString(item.Value.Getter(this)));
                }
            }

            return sb.ToString();
        }

        public static IResult CheckNull(ArgumentAttribute attribute, dynamic value)
        {
            if (System.Object.Equals(null, value))
            {
                if (attribute.CanNull)
                {
                    return attribute.ResultCreate();
                }
                else
                {
                    return attribute.ResultCreate(attribute.State, attribute.Message ?? string.Format("argument \"{0}\" can not null.", attribute.Member));
                }
            }

            return attribute.ResultCreate(data: value);
        }
    }

    /// <summary>
    /// Command attribute on a method, for multiple sources to invoke the method
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class CommandAttribute : AttributeBase
    {
        public CommandAttribute(string onlyName = null) { this.OnlyName = onlyName; }

        //public bool TrimChar { get; set; }

        public string Group { get; set; }

        public string OnlyName { get; set; }

        public string MethodName { get; set; }

        public virtual string GetKey(string space = "->") => string.Format("{1}{0}{2}", space, this.Group, this.OnlyName);

        public override T Clone<T>() => new CommandAttribute { Group = this.Group, OnlyName = this.OnlyName } as T;
    }

    #endregion

    #region

    public class CheckNullAttribute : ArgumentAttribute
    {
        public CheckNullAttribute(int state = -800, string message = null)
            : base(state, message, false) { }

        public override async Task<IResult> Proces(dynamic value)
        {
            if (typeof(System.String).Equals(this.MemberType))
            {
                if (System.String.IsNullOrEmpty(value))
                {
                    return this.ResultCreate(State, Message ?? string.Format("argument \"{0}\" can not null.", this.Member));
                }
            }
            else if (System.Object.Equals(null, value))
            {
                return this.ResultCreate(State, Message ?? string.Format("argument \"{0}\" can not null.", this.Member));
            }

            return this.ResultCreate();
        }
    }

    public class SizeAttribute : ArgumentAttribute
    {
        public SizeAttribute(int state = -801, string message = null, bool canNull = true) : base(state, message, canNull)
        {
            this.BindAfter += () =>
            {
                if (!System.String.IsNullOrWhiteSpace(this.MinMsg))
                {
                    this.MinMsg = this.Replace(this.MinMsg);
                }

                if (!System.String.IsNullOrWhiteSpace(this.MaxMsg))
                {
                    this.MaxMsg = this.Replace(this.MaxMsg);
                }
            };
        }

        public object Min { get; set; }
        public object Max { get; set; }

        public string MinMsg { get; set; }
        public string MaxMsg { get; set; }

        public override async Task<IResult> Proces(dynamic value)
        {
            var result = CheckNull(this, value);
            if (!result.HasData) { return result; }

            var type = System.Nullable.GetUnderlyingType(this.MemberType) ?? this.MemberType;

            var msg = System.String.Empty;
            var minMsg = string.Format("argument \"{0}\" minimum range ", this.Member) + "{0}.";
            var maxMsg = string.Format("argument \"{0}\" maximum range ", this.Member) + "{0}.";

            switch (type.FullName)
            {
                case "System.String":
                    {
                        var ags = System.Convert.ToString(value).Trim();
                        if (null != Min && Help.ChangeType<System.Int32>(Min) > ags.Length)
                        {
                            msg = MinMsg ?? string.Format(minMsg, Min); break;
                        }
                        if (null != Max && Help.ChangeType<System.Int32>(Max) < ags.Length)
                        {
                            msg = MaxMsg ?? string.Format(maxMsg, Max); break;
                        }
                    }
                    break;
                case "System.DateTime":
                    {
                        var ags = System.Convert.ToDateTime(value);
                        if (null != Min && Help.ChangeType<System.DateTime>(Min) > ags)
                        {
                            msg = MinMsg ?? string.Format(minMsg, Min); break;
                        }
                        if (null != Max && Help.ChangeType<System.DateTime>(Max) < ags)
                        {
                            msg = MaxMsg ?? string.Format(maxMsg, Max); break;
                        }
                    }
                    break;
                case "System.Int32":
                    {
                        var ags = System.Convert.ToInt32(value);
                        if (null != Min && Help.ChangeType<System.Int32>(Min) > ags)
                        {
                            msg = MinMsg ?? string.Format(minMsg, Min); break;
                        }
                        if (null != Max && Help.ChangeType<System.Int32>(Max) < ags)
                        {
                            msg = MaxMsg ?? string.Format(maxMsg, Max); break;
                        }
                    }
                    break;
                case "System.Int64":
                    {
                        var ags = System.Convert.ToInt64(value);
                        if (null != Min && Help.ChangeType<System.Int64>(Min) > ags)
                        {
                            msg = MinMsg ?? string.Format(minMsg, Min); break;
                        }
                        if (null != Max && Help.ChangeType<System.Int64>(Max) < ags)
                        {
                            msg = MaxMsg ?? string.Format(maxMsg, Max); break;
                        }
                    }
                    break;
                case "System.Decimal":
                    {
                        var ags = System.Convert.ToDecimal(value);
                        if (null != Min && Help.ChangeType<System.Decimal>(Min) > ags)
                        {
                            msg = MinMsg ?? string.Format(minMsg, Min); break;
                        }
                        if (null != Max && Help.ChangeType<System.Decimal>(Max) < ags)
                        {
                            msg = MaxMsg ?? string.Format(maxMsg, Max); break;
                        }
                    }
                    break;
                case "System.Double":
                    {
                        var ags = System.Convert.ToDouble(value);
                        if (null != Min && Help.ChangeType<System.Double>(Min) > ags)
                        {
                            msg = MinMsg ?? string.Format(minMsg, Min); break;
                        }
                        if (null != Max && Help.ChangeType<System.Double>(Max) < ags)
                        {
                            msg = MaxMsg ?? string.Format(maxMsg, Max); break;
                        }
                    }
                    break;
                default:
                    if (type.IsCollection())
                    {
                        var list = value as System.Collections.ICollection;
                        if (null != Min && Help.ChangeType<System.Int32>(Min) > list.Count)
                        {
                            msg = MinMsg ?? string.Format(minMsg, Min); break;
                        }
                        if (null != Max && Help.ChangeType<System.Int32>(Max) < list.Count)
                        {
                            msg = MaxMsg ?? string.Format(maxMsg, Max); break;
                        }
                    }
                    else
                    {
                        return this.ResultCreate(State, string.Format("argument \"{0}\" type error", this.Member, type.FullName));
                    }
                    break;
            }

            if (!System.String.IsNullOrEmpty(msg))
            {
                return this.ResultCreate(State, Message ?? msg);
            }

            return this.ResultCreate();
        }
    }

    public class ScaleAttribute : ArgumentAttribute
    {
        public ScaleAttribute(int state = -802, string message = null, bool canNull = true) : base(state, message, canNull) { }

        int size = 2;
        public int Size { get => size; set => size = value; }

        public override async Task<IResult> Proces(dynamic value)
        {
            var result = CheckNull(this, value);
            if (!result.HasData) { return result; }

            switch (this.MemberType.FullName)
            {
                case "System.Decimal":
                    return this.ResultCreate(Utils.Help.Scale((decimal)value, size));
                case "System.Double":
                    return this.ResultCreate(Utils.Help.Scale((double)value, size));
                default: return this.ResultCreate(State, Message ?? string.Format("argument \"{0}\" type error.", this.Member));
            }
        }
    }

    public class CheckEmailAttribute : ArgumentAttribute
    {
        public CheckEmailAttribute(int state = -803, string message = null, bool canNull = true) : base(state, message, canNull) { }

        public override async Task<IResult> Proces(dynamic value)
        {
            var result = CheckNull(this, value);
            if (!result.HasData) { return result; }

            if (!Utils.Help.CheckEmail(value))
            {
                return this.ResultCreate(State, Message ?? string.Format("argument \"{0}\" email error.", this.Member));
            }
            return this.ResultCreate();
        }
    }

    public class CheckCharAttribute : ArgumentAttribute
    {
        public CheckCharAttribute(int state = -804, string message = null, bool canNull = true) : base(state, message, canNull) { }

        Utils.Help.CheckCharMode mode = Utils.Help.CheckCharMode.All;
        public Utils.Help.CheckCharMode Mode { get => mode; set => mode = value; }

        public override async Task<IResult> Proces(dynamic value)
        {
            var result = CheckNull(this, value);
            if (!result.HasData) { return result; }

            if (!Utils.Help.CheckChar(value, Mode))
            {
                return this.ResultCreate(State, Message ?? string.Format("argument \"{0}\" char verification failed.", this.Member));
            }
            return this.ResultCreate();
        }
    }

    public class MD5Attribute : ArgumentAttribute
    {
        public MD5Attribute(int state = -820, string message = null, bool canNull = true)
            : base(state, message, canNull) { }

        string encodingNmae = "UTF-8";
        public string EncodingNmae { get => encodingNmae; set => encodingNmae = value; }

        public bool HasUpper { get; set; }

        public override async Task<IResult> Proces(dynamic value)
        {
            var result = CheckNull(this, value);
            if (!result.HasData) { return result; }

            return this.ResultCreate(Utils.Help.MD5(value, encodingNmae, HasUpper));
        }
    }

    /// <summary>
    /// AES
    /// </summary>
    public class AES : ArgumentAttribute
    {
        /// <summary>
        /// d5547b72d2aa42ceae402fd96b3d7b60
        /// </summary>
        public static string KEY = "d5547b72d2aa42ceae402fd96b3d7b60";

        public AES(int code = -821, string message = null, bool canNull = true)
            : base(code, message, canNull) { }

        public override async Task<IResult> Proces(dynamic value)
        {
            var result = CheckNull(this, value);
            if (!result.HasData) { return result; }

            return this.ResultCreate(Help.AES.Encrypt(value, KEY));
        }

        public static bool Equals(string password, string encryptData, string salt)
        {
            try
            {
                return System.Object.Equals(password, Help.AES.Decrypt(encryptData, KEY, salt));
            }
            catch
            {
                return false;
            }
        }
    }

    #endregion

    #region Deserialize

    public sealed class ArgumentDefaultAttribute : ArgumentAttribute
    {
        public ArgumentDefaultAttribute(System.Type resultType, int state = -11, string message = null)
            : base(state, message) { this.resultType = resultType; }

        public override async Task<IResult> Proces(dynamic value) => this.ResultCreate<dynamic>(value);
    }

    public class JsonArgAttribute : ArgumentAttribute
    {
        public JsonArgAttribute(int state = -12, string message = null, bool canNull = false)
            : base(state, message, canNull) { }

        public Newtonsoft.Json.JsonSerializerSettings Settings { get; set; }

        public override async Task<IResult> Proces(dynamic value)
        {
            var result = CheckNull(this, value);
            if (!result.HasData) { return result; }

            try
            {
                return this.ResultCreate(Newtonsoft.Json.JsonConvert.DeserializeObject(value, this.MemberType, Settings));
            }
            catch { return this.ResultCreate(State, Message ?? string.Format("Arguments {0} Json deserialize error", this.Member)); }
        }
    }

    public class ProtoBufArgAttribute : ArgumentAttribute
    {
        public ProtoBufArgAttribute(int state = -13, string message = null, bool canNull = false)
            : base(state, message, canNull) { }

        public override async Task<IResult> Proces(dynamic value)
        {
            var result = CheckNull(this, value);
            if (!result.HasData) { return result; }

            try
            {
                using (var stream = new System.IO.MemoryStream(value))
                {
                    return this.ResultCreate(ProtoBuf.Meta.RuntimeTypeModel.Default.Deserialize(stream, null, this.MemberType));
                }
            }
            catch { return this.ResultCreate(State, Message ?? string.Format("Arguments {0} ProtoBuf deserialize error", this.Member)); }
        }
    }

    #endregion
}
