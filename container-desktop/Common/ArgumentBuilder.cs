using System;
using System.Collections.Generic;
using System.Text;

namespace ContainerDesktop.Common
{
    public class ArgumentBuilder
    {
        private readonly StringBuilder _builder;

        public ArgumentBuilder(string value = null)
        {
            _builder = new StringBuilder(value);
        }

        public ArgumentBuilder Add(string value, bool quoted = false)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                if (quoted)
                {
                    value = $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
                }
                _builder.Append($" {value}");
            }
            return this;
        }

        public ArgumentBuilder Add(string name, string value, bool quoted = false)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
            {
                return this;
            }
            if (quoted)
            {
                value = $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
            }
            _builder.Append($" {name} {value}");
            return this;
        }

        public ArgumentBuilder AddIf(string value, bool add)
        {
            if (add)
            {
                Add(value);
            }
            return this;
        }

        public ArgumentBuilder AddIf(string name, string value, bool add)
        {
            if (add)
            {
                Add(name, value);
            }
            return this;
        }

        public ArgumentBuilder AddKeyValues(string name, IDictionary<string, string> keyValues)
        {
            if (keyValues != null)
            {
                foreach (var entry in keyValues)
                {
                    Add(name, $"{entry.Key}={entry.Value}", true);
                }
            }
            return this;
        }

        public string Build() => _builder.ToString();
    }
}
