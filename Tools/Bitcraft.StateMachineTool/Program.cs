﻿using Bitcraft.StateMachineTool.CodeGenerators;
using Bitcraft.StateMachineTool.CodeGenerators.CSharp;
using Bitcraft.StateMachineTool.Core;
using Bitcraft.ToolKit.CodeGeneration;
using System;
using System.IO;
using System.Linq;
using System.Text;

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
            {
                try
                {
                    graph = parser.Parse(stream);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Graph file parse error:");
                    Console.WriteLine(ex);
                    return -1;
                }
            }

            string namespaceName = args.NamespaceName;
            string stateMachineName = args.StateMachineName ?? graph.Semantic; // command line argument has priority
            bool isInternal = args.IsInternal;

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

            WriteFile(new StateMachineCodeGenerator(generatorsFactory, namespaceName, stateMachineName, isInternal, initialNode, graph), outputPath, fs.StateMachineFilename);
            WriteFile(new StateTokensCodeGenerator(generatorsFactory, namespaceName, stateMachineName, isInternal, graph), outputPath, fs.StateTokensFilename);
            WriteFile(new ActionTokensCodeGenerator(generatorsFactory, namespaceName, stateMachineName, isInternal, graph), outputPath, fs.ActionTokensFilename);

            foreach (var state in fs.States)
            {
                WriteFile(
                    new StateCodeGenerator(
                        generatorsFactory,
                        namespaceName != null ? namespaceName + "." + Constants.StatesFolder : null,
                        stateMachineName,
                        state.Semantic,
                        args.UseOriginalStateBase,
                        isInternal,
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
