using System;
using System.Collections.Generic;
using System.Linq;

namespace ADDA.Common.Helper
{
    public static class Tree
    {
        public static void PrintTree<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> getChildren, Func<T, string> text, string indent = "")
        {
            foreach (var item in source)
            {
                Console.WriteLine(indent + text(item));
                getChildren(item).PrintTree(getChildren, text, indent + "  ");
            }
        }

        // List all nodes of type <T> using a function to get the children of the node
        public static IEnumerable<T> ListAllNodes<T>(T root, Func<T, IEnumerable<T>> getChildren)
        {
            var stack = new Stack<T>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                yield return current;

                var children = getChildren(current);
                if (children != null && children.Any())
                {
                    foreach (var child in children)
                    {
                        stack.Push(child);
                    }
                }
            }
        }

        // Compute all paths to any node from a node of type <T> using a function to get the children of the node
        public static IEnumerable<IEnumerable<T>> ComputeAllPathsFromNode<T>(T root, Func<T, IEnumerable<T>> getChildren)
        {
            var children = getChildren(root);
            if (children != null && children.Any())
            {
                foreach (var Child in children)
                    foreach (var ChildPath in ComputeAllPathsFromNode(Child, getChildren))
                        yield return new[] { root }.Concat(ChildPath);
            }
            yield return new[] { root };
        }

        // Compute all paths to leafs from a node of type <T> using a function to get the children of the node
        public static IEnumerable<IEnumerable<T>> ComputeAllPathsToLeafsFromNode<T>(T root, Func<T, IEnumerable<T>> getChildren)
        {
            var children = getChildren(root);
            if (children != null && children.Any())
            {
                foreach (var Child in children)
                    foreach (var ChildPath in ComputeAllPathsToLeafsFromNode(Child, getChildren))
                        yield return new[] { root }.Concat(ChildPath);
            }
            else
            {
                yield return new[] { root };
            }
        }
    }
}