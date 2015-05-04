using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Mono.Cecil;
using QuickGraph;
using QuickGraph.Graphviz;
using QuickGraph.Graphviz.Dot;

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
            var commandLineArgs = Environment.GetCommandLineArgs();

            if(commandLineArgs.Length < 2)
            {
                PrintHelp();
                return;
            }

            if (commandLineArgs.Contains("-d"))
                Debugger.Launch();

            var asms = from asmLocation in commandLineArgs.Skip(1) 
                       where asmLocation != "-d"
                       select AssemblyDefinition.ReadAssembly(asmLocation);

            foreach (var asm in asms)
            {
                AddEdges(asm, 1);
            }

            //var graph = asm.DependencyEdges().ToAdjacencyGraph<AssemblyDefinition, SEdge<AssemblyDefinition>>();
            var graph = _edges.ToAdjacencyGraph<AssemblyDefinition, SEdge<AssemblyDefinition>>();

            var graphviz = new GraphvizAlgorithm<AssemblyDefinition, SEdge<AssemblyDefinition>>(graph);
            graphviz.FormatVertex += (g, args) =>
                                         {
                                             string name = args.Vertex.Name.Name;
                                             args.VertexFormatter.Label = name;
                                             var x = (float)_edgeList[name].Count / _edges.Count * 100;
                                             //args.VertexFormatter
                                             args.VertexFormatter.BottomLabel = _edgeList[name].Count + " edges";
                                         };

            // render

            foreach (var kp in _edgeList.OrderByDescending(f => f.Value.Count))
                Console.Error.WriteLine(kp.Key + ": " + kp.Value.Count);


            Console.Error.WriteLine("Total Dependencies: " + References.Count);
            Console.Error.WriteLine("Total Edges: " + _edges.Count);


            
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

        private static void AddEdges(AssemblyDefinition asm, int depth)
        {
            //if(depth > MaxDepth)
            //    return;

            foreach (var dep in asm.Dependencies())
            {
                var sEdge = new SEdge<AssemblyDefinition>(asm, dep);
                if (!AddSingleEdge(sEdge)) 
                    continue;
                AddEdges(dep, depth + 1);
            }
        }

        private static bool AddSingleEdge(SEdge<AssemblyDefinition> sEdge)
        {
            if (_edges.Contains(sEdge))
                return false;

            string source = sEdge.Source.Name.Name;
            string target = sEdge.Target.Name.Name;
            
            if(_exclusions.Contains(source) || _exclusions.Contains(target))
                return false;

            Trace.WriteLine(source + " => " + target);
            _edges.Add(sEdge);

            EnsureEdgeList(source);
            EnsureEdgeList(target);

            if (!_edgeList[source].Contains(target))
                _edgeList[source].Add(target);

            return true;
        }

        private static void EnsureEdgeList(string source)
        {
            if (!_edgeList.ContainsKey(source))
                _edgeList.Add(source, new List<string>());
        }

        public static IEnumerable<AssemblyDefinition> Dependencies(this AssemblyDefinition asm)
        {
            if (_resolver == null)
                _resolver = asm.MainModule.AssemblyResolver;

            return asm.MainModule.AssemblyReferences.Select(Resolve);
        }

        private static readonly Dictionary<string, AssemblyDefinition> References = new Dictionary<string, AssemblyDefinition>();
        private static readonly List<SEdge<AssemblyDefinition>> _edges = new List<SEdge<AssemblyDefinition>>();
        private static Dictionary<string, List<string>> _edgeList = new Dictionary<string, List<string>>();
        private static readonly string[] _exclusions = new string[] {"mscorlib", "System", "System.Core", "System.Xml", "System.Drawing", "System.Windows.Forms", "System.Data", 
                                                                     "WindowsBase", "PresentationCore", "PresentationFramework", "System.ServiceModel", "System.Web",

        //3.0
        "ComSvcConfig", "Microsoft.Transactions.Bridge", "Microsoft.Transactions.Bridge.Dtc", "PresentationBuildTasks", "PresentationCFFRasterizer", "PresentationCore", "PresentationFramework", "PresentationFramework.Aero", "PresentationFramework.Classic", "PresentationFramework.Luna", "PresentationFramework.Royale", "PresentationUI", "ReachFramework", "SMSvcHost", "ServiceModelReg", "System.IO.Log", "System.IdentityModel", "System.IdentityModel.Selectors", "System.Printing", "System.Runtime.Serialization", "System.ServiceModel", "System.ServiceModel.Install", "System.ServiceModel.WasHosting", "System.Speech", "System.Workflow.Activities", "System.Workflow.ComponentModel", "System.Workflow.Runtime", "UIAutomationClient", "UIAutomationClientsideProviders", "UIAutomationProvider", "UIAutomationTypes", "WindowsBase", "WindowsFormsIntegration", "WsatConfig", "infocard", 

        //3.5 client
        "Accessibility", "CustomMarshalers", "ISymWrapper", "Microsoft.JScript", "Microsoft.VisualBasic", "Microsoft.VisualC", "Microsoft.Vsa", "mscorlib", "PresentationCore", "PresentationFramework.Aero", "PresentationFramework.Classic",
"PresentationFramework", "PresentationFramework.Luna", "PresentationFramework.Royale", "ReachFramework", "System.AddIn.Contract", "System.AddIn", "System.Configuration", "System.Configuration.Install", "System.Core", "System.Data.DataSetExtensions", 
"System.Data", "System.Data.Linq", "System.Data.Services.Client", "System.Data.SqlXml", "System.Deployment", "System.DirectoryServices", "System", "System.Drawing", "System.EnterpriseServices", "System.IdentityModel", "System.Management",
"System.Messaging", "System.Net", "System.Printing", "System.Runtime.Remoting", "System.Runtime.Serialization", "System.Runtime.Serialization.Formatters.Soap", "System.Security", "System.ServiceModel", "System.ServiceModel.Web",
"System.ServiceProcess", "System.Transactions", "System.Web.Services", "System.Windows.Forms", "System.Windows.Presentation", "System.Xml", "System.Xml.Linq", "UIAutomationClient", "UIAutomationClientsideProviders", "UIAutomationProvider",
"UIAutomationTypes", "WindowsBase", "WindowsFormsIntegration",

        //3.5 full
        "AddInProcess", "AddInProcess32", "AddInUtil", "DataSvcUtil", "EdmGen", "MSBuild", "Microsoft.Build.Conversion.v3.5", "Microsoft.Build.Engine", "Microsoft.Build.Framework", "Microsoft.Build.Tasks.v3.5", "Microsoft.Build.Utilities.v3.5", "Microsoft.Data.Entity.Build.Tasks", "Microsoft.VisualC.STLCLR", "Sentinel.v3.5Client", "System.AddIn", "System.AddIn.Contract", "System.ComponentModel.DataAnnotations", "System.Core", "System.Data.DataSetExtensions", "System.Data.Entity", "System.Data.Entity.Design", "System.Data.Linq", "System.Data.Services", "System.Data.Services.Client", "System.Data.Services.Design", "System.DirectoryServices.AccountManagement", "System.Management.Instrumentation", "System.Net", "System.ServiceModel.Web", "System.Web.Abstractions", "System.Web.DynamicData", "System.Web.DynamicData.Design", "System.Web.Entity", "System.Web.Entity.Design", "System.Web.Extensions", "System.Web.Extensions.Design", "System.Web.Routing", "System.Windows.Presentation", "System.WorkflowServices", "System.Xml.Linq"};
        
        private static int MaxDepth = 2;

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
