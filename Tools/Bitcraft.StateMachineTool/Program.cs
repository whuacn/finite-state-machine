﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Bitcraft.StateMachineTool.Core;
using Bitcraft.ToolKit.CodeGeneration;
using Bitcraft.StateMachineTool.CodeGenerators.CSharp;

namespace Bitcraft.StateMachineTool
{
    class Program
    {
        static int Main(string[] args)
        {
            return new Program().Run(new CommandArguments(args));
        }

        private int Run(CommandArguments args)
        {
            if (args.Errors.Count > 0)
            {
                foreach (var err in args.Errors)
                    Console.WriteLine(err);
                return -1;
            }

            if (args.NothingToDo)
                return 0;

            string graphmlAbsoluteFilename = args.GraphmlFilename;
            if (Path.IsPathRooted(graphmlAbsoluteFilename) == false)
                graphmlAbsoluteFilename = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, graphmlAbsoluteFilename));

            string outputPath = args.OutputFolder;
            if (string.IsNullOrWhiteSpace(outputPath) == false)
            {
                if (Path.IsPathRooted(outputPath) == false)
                {
                    var basePath = args.IsOutputFolderRelativeToWorkingDir
                        ? Environment.CurrentDirectory
                        : Path.GetDirectoryName(graphmlAbsoluteFilename);
                    outputPath = Path.GetFullPath(Path.Combine(basePath, outputPath));
                }
            }
            else
            {
                if (args.IsOutputFolderRelativeToWorkingDir)
                    outputPath = Environment.CurrentDirectory;
                else
                    outputPath = Path.GetDirectoryName(graphmlAbsoluteFilename);
            }

            Directory.CreateDirectory(outputPath);

            IParser parser = new Bitcraft.StateMachineTool.yWorks.Parser();

            IGraph graph = null;
            using (var stream = new FileStream(graphmlAbsoluteFilename, FileMode.Open, FileAccess.Read))
                graph = parser.Parse(stream);

            string namespaceName = args.NamespaceName;
            string stateMachineName = args.StateMachineName ?? graph.Semantic; // command line argument has priority

            var graphInitialNodeName = graph.InitialNode != null ? graph.InitialNode.Semantic : null;

            INode initialNode = null;
            if (string.IsNullOrWhiteSpace(args.InitialStateName) == false)
            {
                var state = graph.Nodes.SingleOrDefault(x => x.Semantic == args.InitialStateName);
                if (state == null)
                {
                    Console.WriteLine("State '{0}' not found.", args.InitialStateName);
                    return -1;
                }

                initialNode = state;
            }
            else
            {
                initialNode = graph.InitialNode;
                if (initialNode == null)
                    initialNode = graph.Nodes.First();
            }

            if (string.IsNullOrWhiteSpace(stateMachineName))
            {
                var format = "State machine name is missing but is mandatory. Please set it either in the graphml file or through the {0} argument";
                Console.WriteLine(string.Format(format, CommandArguments.StateMachineNameArgumentKey));
                return -1;
            }

            var fs = new FileDefinitions(stateMachineName, graph);

            foreach (var folder in fs.Folders)
                Directory.CreateDirectory(Path.Combine(outputPath, folder));

            var generatorsFactory = new Bitcraft.ToolKit.CodeGeneration.CSharp.CSharpLanguageAbstraction();

            WriteFile(new StateMachineCodeGenerator(generatorsFactory, namespaceName, stateMachineName, initialNode, graph), outputPath, fs.StateMachineFilename);
            WriteFile(new StateTokensCodeGenerator(generatorsFactory, namespaceName, stateMachineName, graph), outputPath, fs.StateTokensFilename);
            WriteFile(new ActionTokensCodeGenerator(generatorsFactory, namespaceName, stateMachineName, graph), outputPath, fs.ActionTokensFilename);

            foreach (var state in fs.States)
            {
                WriteFile(
                    new StateCodeGenerator(
                        generatorsFactory,
                        namespaceName,
                        stateMachineName,
                        state.Semantic,
                        args.UseOriginalStateBase,
                        graph
                    ),
                    outputPath,
                    state.RelativePath
                );
            }

            return 0;
        }

        private void WriteFile(ICodeGenerator codeGenerator, string basePath, string relativeFilename)
        {
            var sb = new StringBuilder();
            codeGenerator.Write(new CodeWriter(sb));
            File.WriteAllText(Path.Combine(basePath, relativeFilename), sb.ToString());
        }
    }
}
