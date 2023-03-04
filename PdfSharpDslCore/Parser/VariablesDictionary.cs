using PdfSharpDslCore.Drawing;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace PdfSharpDslCore.Parser
{
    internal interface IVariablesDictionary
    {
        void SaveVariables();
        void RestoreVariables();
    }

    internal class VariablesDictionary : IDictionary<string, object?>, IVariablesDictionary
    {
        private readonly Func<string, object> _systemVariablesGet ;
        private ConcurrentDictionary<string, object?> _inner = new ConcurrentDictionary<string, object?>();
        private readonly Stack<ConcurrentDictionary<string, object?>> _savedVariables = new Stack<ConcurrentDictionary<string, object?>>();

        public VariablesDictionary(Func<string, object> systemVariablesGet)
        {
            _systemVariablesGet = systemVariablesGet;
        }

        public ICollection<string> Keys => _inner.Keys;

        public ICollection<object?> Values => _inner.Values;

        public int Count => _inner.Count;

        public bool IsReadOnly => false;

        public object? this[string key]
        {
            get => this.TryGetValue(key, out var value) ? value! : null;
            set => throw new NotImplementedException();
        }

        public void Add(string key, object? value)
        {
            _inner.AddOrUpdate(key, value, (_, __) => value);
        }

        public bool ContainsKey(string key)
        {
            return _inner.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            return _inner.TryRemove(key, out var _);
        }

        public bool TryGetValue(string key, out object? value)
        {
            //system variables are intercepted
            switch (key)
            {
                case "PAGEHEIGHT":
                case "PAGEWIDTH":
                    value = _systemVariablesGet(key);
                    return true;
                default:
                    return _inner.TryGetValue(key, out value);
            }
        }

        public void Add(KeyValuePair<string, object?> item)
        {
            _inner.AddOrUpdate(item.Key, item.Value, (_, __) => item.Value);
        }

        public void Clear()
        {
            _inner.Clear();
        }

        public bool Contains(KeyValuePair<string, object?> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, object?> item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void SaveVariables()
        {
            _savedVariables.Push(_inner);
            _inner = new ConcurrentDictionary<string, object?>(_inner);
        }

        public void RestoreVariables()
        {
            _inner = _savedVariables.Pop();
        }
    }
}
