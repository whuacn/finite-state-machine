﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bitcraft.ToolKit.CodeGeneration;
using Bitcraft.StateMachineTool.Core;

namespace Bitcraft.StateMachineTool.CodeGenerators.Cpp
{
    public class ActionTokensCodeGenerator : CodeGeneratorBase
    {
        private IGraph graph;

        public ActionTokensCodeGenerator(ILanguageAbstraction generatorsFactory, string namespaceName, string stateMachineName, IGraph graph)
            : base(generatorsFactory, namespaceName, stateMachineName)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph));

            this.graph = graph;
        }

        public override void Write(CodeWriter writer)
        {
            WriteFileHeader(writer);

            Language.CreateRawStatementCodeGenerator(
                "#include <stdio.h>"
            ).Write(writer);

            writer.AppendLine();

            Language.CreateUsingCodeGenerator(
                CSharpConstants.StateMachineNamespace
            ).Write(writer);

            writer.AppendLine();

            base.Write(writer);
        }

        protected override void WriteContent(CodeWriter writer)
        {
            if (graph.Transitions.Length == 0)
                return;

            var distinctTransitions = graph.Transitions
                .Select(t => t.Semantic)
                .Distinct()
                .ToArray();

            foreach (var transition in distinctTransitions)
            {
                Language.CreateVariableDeclarationCodeGenerator(
                    AccessModifier.None,
                    null,
                    Constants.ActionTokenType + "*",
                    transition,
                    string.Format("new " + Constants.ActionTokenType + "(\"{0}\")", transition)).Write(writer);
            }
        }
    }
}
