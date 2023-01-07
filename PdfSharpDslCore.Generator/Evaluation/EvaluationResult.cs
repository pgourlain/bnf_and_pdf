using System;

namespace PdfSharpDslCore.Generator.Evaluation
{
    internal struct EvaluationResult
    {
        private static EvaluationResult _null = new EvaluationResult() { StringValue = null, ValueType = typeof(string) };
        public static EvaluationResult Null => _null;
        public Type ValueType { get; set; }
        public string StringValue { get; set; }

        public static bool operator ==(EvaluationResult a, EvaluationResult b)
        {
            return a.ValueType == b.ValueType && a.StringValue == b.StringValue;
        }
        public static bool operator !=(EvaluationResult a, EvaluationResult b)
        {
            return a.ValueType != b.ValueType || a.StringValue != b.StringValue;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            int vt = 0;
            int sv = 0;
            if (ValueType != null)
            {
                vt = ValueType.GetHashCode();
            }
            if (StringValue != null)
            {
                sv = StringValue.GetHashCode();
            }
            return vt ^ sv;
        }
    }
}
