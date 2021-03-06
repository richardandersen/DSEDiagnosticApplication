﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using IMMLogValue = Common.Patterns.Collections.MemoryMapped.IMMValue<DSEDiagnosticLibrary.ILogEvent>;

namespace DSEDiagnosticLibrary
{
    static class JsonConverters 
    {
        volatile static bool Initialized = false;
        public readonly static JsonConverter[] Instance = new JsonConverter[]
                                                                    {
                                                                        new IPAddressJsonConverter(),
                                                                        new IPEndPointJsonConverter(),
                                                                        new IPathJsonConverter(),
                                                                        new DateTimeRangeJsonConverter()
                                                                    };
        public readonly static JsonSerializerSettings Settings = new JsonSerializerSettings
                                                                    {
                                                                        Converters = Instance
                                                                    };

        static JsonConverters()
        {
            if(!Initialized)
            {
                Initialized = true;
                JsonConvert.DefaultSettings = () => Settings;
            }
        }
    }


    public class IPathJsonConverter : JsonConverter
    {        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Path");
            writer.WriteValue(((Common.IPath)value).Path);
            writer.WritePropertyName("IsFilePath");
            writer.WriteValue(((Common.IPath)value).IsFilePath);
            writer.WritePropertyName("IsAbsolutePath");
            writer.WriteValue(((Common.IPath)value).IsAbsolutePath);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string path = null;
            bool? isFilePath = null;
            bool? isAbsolutePath = null;
            object newObject = existingValue;

            if (reader.Value == null)
            {
                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    if(reader.TokenType == JsonToken.PropertyName)
                    {
                        if((string)reader.Value == "Path")
                        {
                            reader.Read();
                            path = reader.Value as string;
                        }
                        else if ((string)reader.Value == "IsFilePath")
                        {
                            reader.Read();
                            isFilePath = reader.Value as bool?;
                        }
                        else if ((string)reader.Value == "IsAbsolutePath")
                        {
                            reader.Read();
                            isAbsolutePath = reader.Value as bool?;
                        }
                    }
                }
            }
            else
            {
                path = reader.Value as string;
            }

            if(path == null)
            {
                reader.Skip();
            }
            else
            {
                if(!isFilePath.HasValue)
                {
                    isFilePath = typeof(Common.IFilePath).IsAssignableFrom(objectType);
                }
                if(!isAbsolutePath.HasValue)
                {
                    isAbsolutePath = typeof(Common.IAbsolutePath).IsAssignableFrom(objectType);
                }

                if (isFilePath.HasValue)
                {
                    if (isFilePath.Value)
                    {
                        if (isAbsolutePath.HasValue)
                        {
                            if (isAbsolutePath.Value)
                            {
                                newObject = new Common.File.FilePathAbsolute(path);
                            }
                            else
                            {
                                newObject = new Common.File.FilePathRelative(path);
                            }
                        }
                        else
                        {
                            newObject = Common.Path.PathUtils.BuildFilePath(path);
                        }
                    }
                    else
                    {
                        if (isAbsolutePath.HasValue)
                        {
                            if (isAbsolutePath.Value)
                            {
                                newObject = new Common.Directory.DirectoryPathAbsolute(path);
                            }
                            else
                            {
                                newObject = new Common.Directory.DirectoryPathRelative(path);
                            }
                        }
                        else
                        {
                            newObject = Common.Path.PathUtils.BuildDirectoryPath(path);
                        }
                    }
                }
                else
                {
                    newObject = Common.Path.PathUtils.BuildPath(path);
                }
            }

            return newObject;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(Common.IPath).IsAssignableFrom(objectType);
        }
    }

    public class DateTimeRangeJsonConverter : JsonConverter
    {        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Min");
            writer.WriteValue(((Common.DateTimeRange)value).Min);
            writer.WritePropertyName("Max");
            writer.WriteValue(((Common.DateTimeRange)value).Max);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            DateTime? maxDate = null;
            DateTime? minDate = null;
            object newObject = existingValue;

            if (reader.Value == null)
            {
                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        if ((string)reader.Value == "Min")
                        {
                            reader.Read();
                            maxDate = reader.Value as DateTime?;
                        }
                        else if ((string)reader.Value == "Max")
                        {
                            reader.Read();
                            maxDate = reader.Value as DateTime?;
                        }
                    }
                }
            }
            else
            {
                maxDate = reader.Value as DateTime?;
            }

            if (!maxDate.HasValue && !minDate.HasValue)
            {
                reader.Skip();
            }
            else if(maxDate.HasValue && !minDate.HasValue)
            {
                var newDTR = new Common.DateTimeRange();
                newDTR.SetMaximum(maxDate.Value);
                newObject = newDTR;
            }
            else if (!maxDate.HasValue && minDate.HasValue)
            {
                var newDTR = new Common.DateTimeRange();
                newDTR.SetMinimal(minDate.Value);
                newObject = newDTR;
            }
            else
            {
                newObject = new Common.DateTimeRange(minDate.Value, maxDate.Value);
            }

            return newObject;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Common.DateTimeRange);
        }
    }

    public class IPAddressJsonConverter : JsonConverter
    {       
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((System.Net.IPAddress)value)?.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var ipAddressStr = reader.Value as string;

            if(string.IsNullOrEmpty(ipAddressStr))
            {
                return null;
            }

            return System.Net.IPAddress.Parse(ipAddressStr);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(System.Net.IPAddress);
        }
    }

    public class IPEndPointJsonConverter : JsonConverter
    {        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var ep = (System.Net.IPEndPoint)value;
            var jo = new Newtonsoft.Json.Linq.JObject();

            jo.Add("Address", Newtonsoft.Json.Linq.JToken.FromObject(ep.Address, serializer));
            jo.Add("Port", ep?.Port ?? 0);
            jo.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = Newtonsoft.Json.Linq.JObject.Load(reader);
            var address = jo["Address"].ToObject<System.Net.IPAddress>(serializer);

            if (address != null)
            {
                int port = (int)jo["Port"];

                return new System.Net.IPEndPoint(address, port);
            }

            return null;
        }
        
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(System.Net.IPEndPoint);
        }
    }

    /*public class IMMLogValueJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var mmLogValue = (IMMLogValue)value;
            var logValue = mmLogValue == null
                                ? null
                                : mmLogValue.Value;
            var jo = new Newtonsoft.Json.Linq.JObject();

            jo.Add("MMLogValue", Newtonsoft.Json.Linq.JToken.FromObject(logValue, serializer));            
            jo.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = Newtonsoft.Json.Linq.JObject.Load(reader);
            var logValue = jo["MMLogValue"].ToObject<ILogEvent>(serializer);

            if (logValue != null)
            {
               return 
            }

            return null;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IMMLogValue);
        }
    }*/
}
