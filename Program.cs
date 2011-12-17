using System;
using System.Collections.Generic;
using System.IO;
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
        static void Main()
        {
            var list = new List<SEdge<AssemblyDefinition>>();

            var commandLineArgs = Environment.GetCommandLineArgs();

            if(commandLineArgs.Length == 0)
            {
                PrintHelp();
                return;
            }

            foreach (var asm in commandLineArgs.Select(AssemblyDefinition.ReadAssembly))
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

        private static void PrintHelp()
        {
            Console.Error.WriteLine("Writes a GraphViz document to standard out that represents the dependency relationships of the supplied assemblies.");
            Console.Error.WriteLine();
            Console.Error.WriteLine(string.Format("usage: {0} asm1 [asm2 asm3 asmN]", GetCommandName()));
        }

        private static string GetCommandName()
        {
            return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location);
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
            if (_resolver == null)
                _resolver = asm.MainModule.AssemblyResolver;

            return asm.MainModule.AssemblyReferences.Select(Resolve);
        }

        private static readonly Dictionary<string, AssemblyDefinition> References = new Dictionary<string, AssemblyDefinition>();

        private static AssemblyDefinition Resolve(AssemblyNameReference reference)
        {
            if (!References.ContainsKey(reference.Name))
            {
                var asm = _resolver.Resolve(reference);
                References.Add(reference.Name, asm);
            }

            return References[reference.Name]; ;
        }
    }

}
