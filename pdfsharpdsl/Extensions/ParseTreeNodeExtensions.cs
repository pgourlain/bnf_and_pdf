using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pdfsharpdsl.Extensions
{
    internal static class ParseTreeNodeExtensions
    {
        public static IEnumerable<ParseTreeNode> ChildNodes(this ParseTreeNode node, string termName)
        {
            List<ParseTreeNode> result = new();
            Queue<ParseTreeNode> queue = new();

            queue.Enqueue(node);

            while (queue.TryDequeue(out var n))
            {
                if (n.Term != null && n.Term.Name == termName)
                {
                    result.Add(n);
                }
                foreach (var item in n.ChildNodes)
                {
                    queue.Enqueue(item);
                }
            }
            return result.AsReadOnly();
        }
    }
}
