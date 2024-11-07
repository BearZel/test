using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbakConfigurator
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.IO;
    using System.Windows.Media.Media3D;

    public class Property
    {
        private JToken _value;

        public Property(JToken value)
        {
            _value = value;
        }

        public T Get<T>()
        {
            return _value.ToObject<T>();
        }

        public void Set<T>(T value)
        {
            _value = JToken.FromObject(value);
        }

        public void Add<T>(string key, T value)
        {
            if (_value.Type == JTokenType.Object)
            {
                ((JObject)_value)[key] = JToken.FromObject(value);
            }
        }

        public Property Add(string key)
        {
            if (_value.Type == JTokenType.Object)
            {
                var newProperty = new Property(((JObject)_value)[key]);
                return newProperty;
            }
            throw new InvalidOperationException("Cannot add to non-object property.");
        }

        public Property Get(string key)
        {
            if (_value.Type == JTokenType.Object)
            {
                return new Property(((JObject)_value)[key]);
            }
            throw new InvalidOperationException("Cannot get from non-object property.");
        }

        public static implicit operator string(Property p) => p.Get<string>();
        public static implicit operator int(Property p) => p.Get<int>();
        public static implicit operator double(Property p) => p.Get<double>();
    }

    public class Config
    {
        private const string DefaultConfigDir = "/opt/abak/A:/assembly/configs/";
        private string _name;
        private string _dirPath;
        private JObject _rootJson;
        private Property _root;

        public Config(string name, string basePath = DefaultConfigDir)
        {
            _name = name;
            _dirPath = basePath;
            _rootJson = new JObject();
            _root = new Property(_rootJson);

            var filePath = _dirPath + _name;
            _rootJson = CAuxil.ParseJson(filePath);
            _root = new Property(_rootJson);
        }

        public Property Root()
        {
            return _root;
        }

        public void Save()
        {
            var filePath = _dirPath + _name;
            CAuxil.SaveJson(filePath, _rootJson);
        }

        public void SetBasePath(string path)
        {
            _dirPath = path;
        }
    }
}
