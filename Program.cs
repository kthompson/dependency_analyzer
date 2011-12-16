using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Mono.Cecil;
using QuickGraph;
using QuickGraph.Graphviz;

namespace DependencyAnalyzer
{
    static class Program
    {
        private static IAssemblyResolver _resolver;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var list = new List<SEdge<AssemblyDefinition>>();

            var asm = AssemblyDefinition.ReadAssembly(Assembly.GetExecutingAssembly().Location);
            _resolver = asm.MainModule.AssemblyResolver;

            AddEdges(asm, list);

            //var graph = asm.DependencyEdges().ToAdjacencyGraph<AssemblyDefinition, SEdge<AssemblyDefinition>>();
            var graph = list.ToAdjacencyGraph<AssemblyDefinition, SEdge<AssemblyDefinition>>();

            var graphviz = new GraphvizAlgorithm<AssemblyDefinition, SEdge<AssemblyDefinition>>(graph);
            graphviz.FormatVertex += (g, args) =>
                                         {   
                                             args.VertexFormatter.Label = args.Vertex.Name.Name;
                                         };

            // render
            Console.Write(graphviz.Generate());
        }


        private static void AddEdges(AssemblyDefinition asm, ICollection<SEdge<AssemblyDefinition>> list)
        {
            foreach (var dep in asm.Dependencies())
            {
                var sEdge = new SEdge<AssemblyDefinition>(asm, dep);
                if (list.Contains(sEdge))
                    continue;
                System.Diagnostics.Trace.WriteLine(sEdge.Source.Name.Name + " => " + sEdge.Target.Name.Name);
                list.Add(sEdge);
                AddEdges(dep, list);
            }
        }

        public static IEnumerable<AssemblyDefinition> Dependencies(this AssemblyDefinition asm)
        {
            return asm.MainModule.AssemblyReferences.Select(Resolve);
        }

        private static readonly Dictionary<string, AssemblyDefinition> _references = new Dictionary<string, AssemblyDefinition>();

        private static AssemblyDefinition Resolve(AssemblyNameReference reference)
        {
            if (!_references.ContainsKey(reference.Name))
            {
                var asm = _resolver.Resolve(reference);
                _references.Add(reference.Name, asm);
            }

            return _references[reference.Name]; ;
        }
    }

}
