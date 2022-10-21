using Irony.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfSharpDslCore.Extensions
{
    internal static class ParseTreeNodeExtensions
    {
        public static IEnumerable<ParseTreeNode> ChildNodes(this ParseTreeNode node, string termName)
        {
            List<ParseTreeNode> result = new List<ParseTreeNode>();
            Queue<ParseTreeNode> queue = new Queue<ParseTreeNode>();

            queue.Enqueue(node);

            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
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

        public static ParseTreeNode ChildNode(this ParseTreeNode node, string termName)
        {
            return node.ChildNodes.Where(n => n.Term != null && n.Term.Name == termName).FirstOrDefault();
        }
    }
}
