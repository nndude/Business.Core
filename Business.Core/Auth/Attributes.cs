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
    using System.Linq;
    using Result;

    #region abstract

    #region

    [System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class IgnoreAttribute : System.Attribute { }

    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class ConfigAttribute : System.Attribute
    {
        public ConfigAttribute(string name, int port = 5000)
        {
            this.name = name;
            this.port = port;
        }

        public ConfigAttribute(int port, string name = null)
        {
            this.port = port;
            this.name = name;
        }

        string name;
        public string Name { get { return this.name; } internal set { this.name = value; } }

        int port;
        public int Port { get { return this.port; } internal set { this.port = value; } }
    }

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method | System.AttributeTargets.Struct | System.AttributeTargets.Parameter, AllowMultiple = true, Inherited = true)]
    public class LoggerAttribute : System.Attribute, Extensions.ICloneable<LoggerAttribute>
    {
        public LoggerAttribute Clone()
        {
            return new LoggerAttribute(logType, canWrite) { CanValue = CanValue, CanResult = CanResult };
        }

        object System.ICloneable.Clone()
        {
            return this.Clone();
        }

        public LoggerAttribute(LoggerType logType, bool canWrite = true)
        {
            this.logType = logType;
            this.canWrite = canWrite;
        }

        readonly LoggerType logType;
        public LoggerType LogType
        {
            get { return logType; }
        }

        bool canWrite;
        public bool CanWrite { get { return canWrite; } set { canWrite = value; } }

        public LoggerValueMode CanValue { get; set; }

        public bool CanResult { get; set; }
    }

    public enum LoggerValueMode
    {
        Select = 0,
        All = 1,
        No = 2,
    }

    #endregion

    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct | System.AttributeTargets.Property | System.AttributeTargets.Field | System.AttributeTargets.Parameter, AllowMultiple = true, Inherited = true)]
    public abstract class ArgumentAttribute : System.Attribute
    {
        #region MetaData

        public static readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Tuple<System.Type, System.Action<object, object>>>> MetaData;

        static System.Collections.Generic.Dictionary<string, System.Tuple<System.Type, System.Action<object, object>>> GetMetaData(System.Type type)
        {
            var member = new System.Collections.Generic.Dictionary<string, System.Tuple<System.Type, System.Action<object, object>>>();

            var fields = type.GetFields();
            foreach (var field in fields)
            {
                member.Add(field.Name, System.Tuple.Create(field.FieldType, Extensions.Emit.FieldAccessorGenerator.CreateSetter(field)));
            }

            var propertys = type.GetProperties();
            foreach (var property in propertys)
            {
                var setter = Extensions.Emit.PropertyAccessorGenerator.CreateSetter(property);
                if (null == setter) { continue; }
                member.Add(property.Name, System.Tuple.Create(property.PropertyType, setter));
            }

            return member;
        }

        static ArgumentAttribute()
        {
            MetaData = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, System.Tuple<System.Type, System.Action<object, object>>>>();

            var ass = System.AppDomain.CurrentDomain.GetAssemblies().Where(c => !c.IsDynamic);

            foreach (var item in ass)
            {
                try
                {
                    var types = item.GetExportedTypes();

                    foreach (var type in types)
                    {
                        if (type.IsSubclassOf(typeof(ArgumentAttribute)) && !type.IsAbstract)
                        {
                            MetaData.Add(type.FullName, GetMetaData(type));
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    System.IO.File.AppendAllText(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Business.Lib.log.txt"), string.Format("{1}{0}{2}{0}", System.Environment.NewLine, item.FullName, ex), System.Text.Encoding.UTF8);
                }
            }
        }

        #endregion

        public virtual bool MemberSet(System.Type type, string value, out object outValue)
        {
            outValue = null;

            if (type.IsEnum)
            {
                if (System.String.IsNullOrWhiteSpace(value)) { return false; }

                var enums = System.Enum.GetValues(type).Cast<System.Enum>();
                var enumValue = enums.FirstOrDefault(c => value.Equals(c.ToString(), System.StringComparison.InvariantCultureIgnoreCase));
                if (null != enumValue)
                {
                    outValue = enumValue;
                    return true;
                }
                return false;
            }
            else
            {
                switch (type.FullName)
                {
                    case "System.String":
                        outValue = value;
                        return true;
                    case "System.Int16":
                        short value2;
                        if (!System.Int16.TryParse(value, out value2))
                        {
                            outValue = value2;
                            return true;
                        }
                        return false;
                    case "System.Int32":
                        int value3;
                        if (System.Int32.TryParse(value, out value3))
                        {
                            outValue = value3;
                            return true;
                        }
                        return false;
                    case "System.Int64":
                        long value4;
                        if (System.Int64.TryParse(value, out value4))
                        {
                            outValue = value4;
                            return true;
                        }
                        return false;
                    case "System.Decimal":
                        decimal value5;
                        if (System.Decimal.TryParse(value, out value5))
                        {
                            outValue = value5;
                            return true;
                        }
                        return false;
                    case "System.Double":
                        double value6;
                        if (System.Double.TryParse(value, out value6))
                        {
                            outValue = value6;
                            return true;
                        }
                        return false;
                    default: return false;
                }
            }
        }

        internal bool MemberSet(string member, string value)
        {
            System.Collections.Generic.Dictionary<string, System.Tuple<System.Type, System.Action<object, object>>> meta;
            if (MetaData.TryGetValue(this.GetType().FullName, out meta))
            {
                System.Tuple<System.Type, System.Action<object, object>> accessor;
                if (meta.TryGetValue(member, out accessor))
                {
                    if (null == accessor.Item2) { return false; }

                    try
                    {
                        object outValue;
                        if (MemberSet(accessor.Item1, value, out outValue))
                        {
                            accessor.Item2(this, outValue);
                            return true;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        public ArgumentAttribute(int code, string message = null, bool canNull = true)
        {
            this.fullName = this.GetType().FullName;
            this.code = code;
            this.message = message;
            this.canNull = canNull;
        }

        internal readonly string fullName;

        int code;
        /// <summary>
        /// Used to return code
        /// </summary>
        public int Code { get { return code; } set { this.code = value; } }

        string message;
        /// <summary>
        /// Used to return error messages
        /// </summary>
        public string Message { get { return message; } set { this.message = value; } }

        bool canNull;
        /// <summary>
        /// By checking the Allow null value
        /// </summary>
        public bool CanNull { get { return canNull; } set { canNull = value; } }

        /// <summary>
        /// Remove leading or trailing white space characters
        /// </summary>
        public bool TrimChar { get; set; }

        /// <summary>
        /// Used for the command group
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Start processing the Parameter object, By this.ResultCreate() method returns
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="method"></param>
        /// <param name="member"></param>
        /// <param name="business"></param>
        /// <returns></returns>
        public abstract IResult Proces(dynamic value, System.Type type, string method, string member, dynamic business);

        #region Result

        /// <summary>
        /// Used to create the Proces() method returns object
        /// </summary>
        /// <returns></returns>
        public IResult ResultCreate()
        {
            return ResultFactory.Create();
        }

        /// <summary>
        /// Used to create the Proces() method returns object
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public IResult ResultCreate(int state)
        {
            return ResultFactory.Create(state);
        }

        /// <summary>
        /// Used to create the Proces() method returns object
        /// </summary>
        /// <param name="state"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public IResult ResultCreate(int state, string message)
        {
            return ResultFactory.Create(state, message);
        }

        /// <summary>
        /// Used to create the Proces() method returns object
        /// </summary>
        /// <typeparam name="Data"></typeparam>
        /// <param name="data"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public IResult<Data> ResultCreate<Data>(Data data, int state = 1)
        {
            return ResultFactory.Create(data, state);
        }

        #endregion
    }

    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class CommandAttribute : System.Attribute
    {
        public CommandAttribute(string onlyName = null) { this.OnlyName = onlyName; }

        public bool TrimChar { get; set; }

        public string Group { get; set; }

        public string OnlyName { get; set; }
    }

    #endregion

    #region

    public sealed class CheckNullAttribute : ArgumentAttribute
    {
        public CheckNullAttribute(int code = -800, string message = null)
            : base(code, message) { }

        public override IResult Proces(dynamic value, System.Type type, string method, string member, dynamic business)
        {
            if (typeof(System.String).Equals(type))
            {
                if (System.String.IsNullOrEmpty(value))
                {
                    return this.ResultCreate(Code, Message ?? string.Format("argument \"{0}\" can not null.", member));
                }
            }
            else if (System.Object.Equals(null, value))
            {
                return this.ResultCreate(Code, Message ?? string.Format("argument \"{0}\" can not null.", member));
            }

            return this.ResultCreate();
        }
    }

    public sealed class SizeAttribute : ArgumentAttribute
    {
        public SizeAttribute(int code = -801, string message = null, bool canNull = true) : base(code, message, canNull) { }

        public object Min { get; set; }
        public object Max { get; set; }

        public override IResult Proces(dynamic value, System.Type type, string method, string member, dynamic business)
        {
            if (this.CanNull && System.Object.Equals(null, value)) { return this.ResultCreate(); }

            var msg = System.String.Empty;

            switch (type.FullName)
            {
                case "System.String":
                    var ags1 = System.Convert.ToString(value).Trim();
                    if (null != Min && Extensions.Help.ChangeType<System.Int32>(Min) > ags1.Length)
                    {
                        msg = string.Format("argument \"{0}\" minimum length value {1}.", member, Min);
                    }
                    if (null != Max && Extensions.Help.ChangeType<System.Int32>(Max) < ags1.Length)
                    {
                        msg = string.Format("argument \"{0}\" maximum length value {1}.", member, Max);
                    }
                    break;
                case "System.DateTime":
                    var ags2 = System.Convert.ToDateTime(value);
                    if (null != Min && Extensions.Help.ChangeType<System.DateTime>(Min) > ags2)
                    {
                        msg = string.Format("argument \"{0}\" minimum value {1}.", member, Min);
                    }
                    if (null != Max && Extensions.Help.ChangeType<System.DateTime>(Max) < ags2)
                    {
                        msg = string.Format("argument \"{0}\" maximum value {1}.", member, Max);
                    }
                    break;
                case "System.Int32":
                    var ags3 = System.Convert.ToInt32(value);
                    if (null != Min && Extensions.Help.ChangeType<System.Int32>(Min) > ags3)
                    {
                        msg = string.Format("argument \"{0}\" minimum value {1}.", member, Min);
                    }
                    if (null != Max && Extensions.Help.ChangeType<System.Int32>(Max) < ags3)
                    {
                        msg = string.Format("argument \"{0}\" maximum value {1}.", member, Max);
                    }
                    break;
                case "System.Int64":
                    var ags4 = System.Convert.ToInt64(value);
                    if (null != Min && Extensions.Help.ChangeType<System.Int64>(Min) > ags4)
                    {
                        msg = string.Format("argument \"{0}\" minimum value {1}.", member, Min);
                    }
                    if (null != Max && Extensions.Help.ChangeType<System.Int64>(Max) < ags4)
                    {
                        msg = string.Format("argument \"{0}\" maximum value {1}.", member, Max);
                    }
                    break;
                case "System.Decimal":
                    var ags5 = System.Convert.ToDecimal(value);
                    if (null != Min && Extensions.Help.ChangeType<System.Decimal>(Min) > ags5)
                    {
                        msg = string.Format("argument \"{0}\" minimum value {1}.", member, Min);
                    }
                    if (null != Max && Extensions.Help.ChangeType<System.Decimal>(Max) < ags5)
                    {
                        msg = string.Format("argument \"{0}\" maximum value {1}.", member, Max);
                    }
                    break;
                case "System.Double":
                    var ags6 = System.Convert.ToDouble(value);
                    if (null != Min && Extensions.Help.ChangeType<System.Double>(Min) > ags6)
                    {
                        msg = string.Format("argument \"{0}\" minimum value {1}.", member, Min);
                    }
                    if (null != Max && Extensions.Help.ChangeType<System.Double>(Max) < ags6)
                    {
                        msg = string.Format("argument \"{0}\" maximum value {1}.", member, Max);
                    }
                    break;
                default:
                    //var iList = type.GetInterface("System.Collections.IList");
                    if (typeof(System.Collections.IList).IsAssignableFrom(value.GetType()))
                    {
                        var list = value as System.Collections.IList;
                        if (null != Min && Extensions.Help.ChangeType<System.Int32>(Min) > list.Count)
                        {
                            msg = string.Format("argument \"{0}\" minimum count value {1}.", member, Min);
                        }
                        if (null != Max && Extensions.Help.ChangeType<System.Int32>(Max) < list.Count)
                        {
                            msg = string.Format("argument \"{0}\" maximum count value {1}.", member, Max);
                        }
                    }
                    break;
            }

            if (!System.String.IsNullOrEmpty(msg))
            {
                return this.ResultCreate(Code, Message ?? msg);
            }

            return this.ResultCreate();
        }
    }

    public sealed class ScaleAttribute : ArgumentAttribute
    {
        public ScaleAttribute(int code = -802, string message = null, bool canNull = true) : base(code, message, canNull) { }

        int size = 2;
        public int Size { get { return size; } set { size = value; } }

        public override IResult Proces(dynamic value, System.Type type, string method, string member, dynamic business)
        {
            if (this.CanNull && System.Object.Equals(null, value)) { return this.ResultCreate(); }

            switch (type.FullName)
            {
                case "System.Decimal":
                    return this.ResultCreate(Extensions.Help.Scale((decimal)value, size));
                case "System.Double":
                    return this.ResultCreate(Extensions.Help.Scale((double)value, size));
                default: return this.ResultCreate(Code, Message ?? string.Format("argument \"{0}\" type error.", member));
            }
        }
    }

    public sealed class CheckEmailAttribute : ArgumentAttribute
    {
        public CheckEmailAttribute(int code = -803, string message = null, bool canNull = true) : base(code, message, canNull) { }

        public override IResult Proces(dynamic value, System.Type type, string method, string member, dynamic business)
        {
            if (this.CanNull && System.Object.Equals(null, value)) { return this.ResultCreate(); }

            if (!Extensions.Help.CheckEmail(value))
            {
                return this.ResultCreate(Code, Message ?? string.Format("argument \"{0}\" email error.", member));
            }
            return this.ResultCreate();
        }
    }

    public sealed class CheckCharAttribute : ArgumentAttribute
    {
        public CheckCharAttribute(int code = -804, string message = null, bool canNull = true) : base(code, message, canNull) { }

        Extensions.Help.CheckCharMode mode = Extensions.Help.CheckCharMode.All;
        public Extensions.Help.CheckCharMode Mode { get { return mode; } set { mode = value; } }

        public override IResult Proces(dynamic value, System.Type type, string method, string member, dynamic business)
        {
            if (this.CanNull && System.Object.Equals(null, value)) { return this.ResultCreate(); }

            if (!Extensions.Help.CheckChar(value, Mode))
            {
                return this.ResultCreate(Code, Message ?? string.Format("argument \"{0}\" char verification failed.", member));
            }
            return this.ResultCreate();
        }
    }

    public sealed class MD5Attribute : ArgumentAttribute
    {
        public MD5Attribute(int code = -805, string message = null, bool canNull = true)
            : base(code, message, canNull) { }

        string encodingNmae = "UTF-8";
        public string EncodingNmae { get { return encodingNmae; } set { encodingNmae = value; } }

        public bool HasUpper { get; set; }

        public override IResult Proces(dynamic value, System.Type type, string method, string member, dynamic business)
        {
            if (this.CanNull && System.Object.Equals(null, value)) { return this.ResultCreate(); }

            return this.ResultCreate(Extensions.Help.MD5Encoding(value, encodingNmae, HasUpper));
        }
    }

    #endregion

    #region Deserialize

    public sealed class JsonArgAttribute : ArgumentAttribute
    {
        public JsonArgAttribute(int code = -12, string message = null, bool canNull = false)
            : base(code, message, canNull) { }

        public override IResult Proces(dynamic value, System.Type type, string method, string member, dynamic business)
        {
            if (this.CanNull && System.Object.Equals(null, value)) { return this.ResultCreate(); }

            try
            {
                return this.ResultCreate(Newtonsoft.Json.JsonConvert.DeserializeObject(value, type));
            }
            catch { return this.ResultCreate(Code, Message ?? string.Format("Arguments {0} Json deserialize error", member)); }
        }
    }

    public sealed class ProtoBufArgAttribute : ArgumentAttribute
    {
        public ProtoBufArgAttribute(int code = -13, string message = null, bool canNull = false)
            : base(code, message, canNull) { }

        public override IResult Proces(dynamic value, System.Type type, string method, string member, dynamic business)
        {
            if (this.CanNull && System.Object.Equals(null, value)) { return this.ResultCreate(); }

            try
            {
                using (var stream = new System.IO.MemoryStream(value))
                {
                    return this.ResultCreate(ProtoBuf.Meta.RuntimeTypeModel.Default.Deserialize(stream, null, type));
                }
            }
            catch { return this.ResultCreate(Code, Message ?? string.Format("Arguments {0} ProtoBuf deserialize error", member)); }
        }
    }

    #endregion
}
