using PdfSharpDslCore.Drawing;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace PdfSharpDslCore.Parser
{
    internal class VariablesDictionary : IDictionary<string, object>
    {

        IPdfDocumentDrawer _pdfDocumentDrawer;
        ConcurrentDictionary<string, object> _inner = new ConcurrentDictionary<string, object>();

        public VariablesDictionary(IPdfDocumentDrawer pdfDocumentDrawer)
        {
            _pdfDocumentDrawer = pdfDocumentDrawer;
        }

        public ICollection<string> Keys => throw new NotImplementedException();

        public ICollection<object> Values => throw new NotImplementedException();

        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => false;

        public object this[string key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Add(string key, object value)
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

        public bool TryGetValue(string key, out object value)
        {
            //system variables are intercepted
            switch(key)
            {
                case "PAGEWIDTH":
                    value = _pdfDocumentDrawer.PageWidth;
                    return true;
                case "PAGEHEIGHT":
                    value = _pdfDocumentDrawer.PageHeight;
                    return true;
                default:
                    return _inner.TryGetValue(key, out value);
            }
        }

        public void Add(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            _inner.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
